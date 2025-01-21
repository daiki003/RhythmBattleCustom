using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PlayFabController
{
    private static bool newAccount; //アカウントを新規作成したかどうか
    private static string playFabId; //自分のID
    public static string randomPlayFabId; //直近で取得した他の人のID
    private static readonly string ID_CHARACTERS = "0123456789"; //IDに使用する文字
    [SerializeField] static GetPlayerCombinedInfoRequestParams InfoRequestParams;

    [HideInInspector] static public string PlayerName { get; private set; }
    [HideInInspector] static public List<CharacterResult> Characters { get; private set; }
    private static bool _finishGetMaster;

    // ログイン ---------------------------------------------------------------------------------------------------------------------------------------[]
    public static void Login()
    {
        InfoRequestParams = new GetPlayerCombinedInfoRequestParams();
        InfoRequestParams.GetUserData = true;
        PlayFabAuthService.Instance.InfoRequestParams = InfoRequestParams;
        PlayFabAuthService.Instance.InitializeCallback();
        PlayFabAuthService.OnLoginSuccess += loginSuccess;
        PlayFabAuthService.Instance.Authenticate(Authtypes.Silent);
    }

    public static void loginSuccess(LoginResult result)
    {
        playFabId = result.PlayFabId;
        UpdateRandomPlayfabId();
        MasterManager.GetAllMasterData();
        Debug.Log("ログイン" + playFabId);
    }

    public static void SimpleLogin(string customId)
    {
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CustomId = customId,
            CreateAccount = true,
            InfoRequestParameters = InfoRequestParams
        }, (result) =>
        {
            loginSuccess(result);

        }, (error) =>
        {
            Debug.LogError(error.GenerateErrorReport());

        });
    }

    public static void InitializePrivateData(Action callBack)
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "Name", "" }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);

        void OnSuccess(UpdateUserDataResult result)
        {
            callBack();
            Debug.Log("InitializePlayerData");
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("InitializeUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    public static void InitializePublicData(Action callBack)
    {
        var clearStates = new List<ClearState>();
        for (int i = 0; i < MasterManager.StageMasterList.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var clearState = new ClearState()
                {
                    StageId = MasterManager.StageMasterList[i].StageId,
                    Level = j,
                };
                clearStates.Add(clearState);
            }
        }
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "ClearStates", PlayFabSimpleJson.SerializeObject(clearStates) }
            },
            Permission = UserDataPermission.Public
        };

        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);

        void OnSuccess(UpdateUserDataResult result)
        {
            InitializePrivateData(callBack);
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("InitializeUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

#region プレイヤーデータ取得
    // 自身の全てのデータを取得してSaveDataを更新
    public static void GetPlayerData()
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, OnSuccess, OnError);

        void OnSuccess(GetUserDataResult result)
        {
            if (result.Data.ContainsKey("ClearStates"))
            {
                SaveDataManager.ClearStateList = PlayFabSimpleJson.DeserializeObject<List<ClearState>>(result.Data["ClearStates"].Value);
                foreach (var item in result.Data)
                {
                    if (item.Key.Contains("Override"))
                    {
                        MasterManager.OverrideMasterList.Add(PlayFabSimpleJson.DeserializeObject<StageMaster>(item.Value.Value));
                    }
                    if (item.Key == "CustomStageList")
                    {
                        MasterManager.CustomStageList = PlayFabSimpleJson.DeserializeObject<List<SingleStageMaster>>(item.Value.Value);
                        MasterManager.SingleStageList.AddRange(MasterManager.CustomStageList);
                    }
                }
            }
            else
            {
                InitializePublicData(GetPlayerData);
            }
            Debug.Log("GetUserData: Success!");
            MasterManager.FinishGetMaster = true;
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("GetUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }
    public static StageMaster GetOverrideStageMaster(string stageId)
    {
        var request = new GetUserDataRequest();
        StageMaster stageMaster = null;
        PlayFabClientAPI.GetUserData(request, OnSuccess, OnError);
        return stageMaster;

        void OnSuccess(GetUserDataResult result)
        {
            string overrideKey = stageId + "Override";
            if (result.Data.ContainsKey(overrideKey))
            {
                stageMaster = PlayFabSimpleJson.DeserializeObject<StageMaster>(result.Data["ClearStates"].Value);
            }
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("GetUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }
#endregion

#region プレイヤーデータ操作
    public static void UpdatePlayerName(string playerName)
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "Name", playerName }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);

        void OnSuccess(UpdateUserDataResult result)
        {
            GetPlayerData();
            Debug.Log("UpdateUserData: Success!");
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("UpdateUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    public static void UpdateClearState(List<ClearState> clearStates)
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "ClearStates", PlayFabSimpleJson.SerializeObject(clearStates) }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);

        void OnSuccess(UpdateUserDataResult result)
        {
            GetPlayerData();
            Debug.Log("UpdateUserData: Success!");
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("UpdateUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    public static async UniTask UpdateOverrideScore(StageMaster stageMaster)
    {
        string keyName = stageMaster.StageId + "Override";
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { keyName, PlayFabSimpleJson.SerializeObject(stageMaster) }
            }
        };

        bool isSuccess = false;
        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);
        await UniTask.WaitUntil(() => isSuccess);

        void OnSuccess(UpdateUserDataResult result)
        {
            isSuccess = true;
            Debug.Log("UpdateStageOverride:" + keyName);
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("UpdateUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    public static async UniTask UpdateCustomStageList()
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                // 現在のCustomStageListの状態をサーバーに保存
                { "CustomStageList", PlayFabSimpleJson.SerializeObject(MasterManager.CustomStageList) }
            }
        };

        bool isSuccess = false;
        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);
        await UniTask.WaitUntil(() => isSuccess);

        void OnSuccess(UpdateUserDataResult result)
        {
            isSuccess = true;
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("UpdateUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }
#endregion

    // タイトルデータ取得 ------------------------------------------------------------------------------------------------------------------------------------
    public static void GetTitleData(Action<SettingMaster, List<StageMaster>> callBack)
    {
        var request = new GetTitleDataRequest();
        PlayFabClientAPI.GetTitleData(request, OnSuccess, OnError);

        void OnSuccess(GetTitleDataResult result)
        {
            Debug.Log("GetTitleData: Success!");

            var settingData = PlayFabSimpleJson.DeserializeObject<SettingMaster>(result.Data["Setting"]);
            var stageMasterList = new List<StageMaster>();
            foreach (var item in result.Data)
            {
                if (item.Key != "Setting")
                {
                    stageMasterList.Add(PlayFabSimpleJson.DeserializeObject<StageMaster>(item.Value));
                }
            }
            callBack(settingData, stageMasterList);
            _finishGetMaster = true;
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("GetTitleData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

#region ランキング関連
    // ランキング情報の登録
    public static void UpdatePlayerStatistics()
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>{
                new StatisticUpdate{
                    StatisticName = "Test",   
                    //ランダムに値を入れる事でランダムな取得に対応する
                    Value = UnityEngine.Random.Range(0, 1000000)
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdatePlayerStatisticsSuccess, OnUpdatePlayerStatisticsFailure);
    }

    //スコア(統計情報)の更新成功
    private static void OnUpdatePlayerStatisticsSuccess(UpdatePlayerStatisticsResult result)
    {
        Debug.Log($"スコア(統計情報)の更新が成功しました");
    }

    //スコア(統計情報)の更新失敗
    private static void OnUpdatePlayerStatisticsFailure(PlayFabError error)
    {
        Debug.LogError($"スコア(統計情報)更新に失敗しました\n{error.GenerateErrorReport()}");
    }

    //ランキング情報を更新する
    public static void UpdateRandomPlayfabId()
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "Test",
            //自分と周りの+-1のデータを取得する
            MaxResultsCount = 3
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, OnSuccess, OnError);
    }


    static void OnSuccess(GetLeaderboardAroundPlayerResult leaderboardResult)
    {
        if (leaderboardResult.Leaderboard.Count == 0)
        {
            return;
        }

        //取得したランキングからランダムにindexを取得
        int rnd = UnityEngine.Random.Range(0, leaderboardResult.Leaderboard.Count);
        int count = 0;
        while (leaderboardResult.Leaderboard[rnd].PlayFabId == playFabId && count < 10000)
        {
            rnd = UnityEngine.Random.Range(0, leaderboardResult.Leaderboard.Count);
            count++;
        }
        //取得したPlayFabIdを元にユーザデータを読み込む
        randomPlayFabId = leaderboardResult.Leaderboard[rnd].PlayFabId;
        Debug.Log("ランダムID更新" + randomPlayFabId);

        //自分のランキングValueを更新しておく
        UpdatePlayerStatistics();
    }

    static void OnError(PlayFabError error)
    {
        Debug.LogError($"スコア(統計情報)取得に失敗しました\n{error.GenerateErrorReport()}");
    }
#endregion
}