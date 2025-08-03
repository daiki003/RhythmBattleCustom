using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

public class PlayFabController
{
    private static string playFabId; //自分のID
    public static string randomPlayFabId; //直近で取得した他の人のID
    [SerializeField] static GetPlayerCombinedInfoRequestParams InfoRequestParams;

    private static bool _isLoginSuccess = false;

    private const string _customIdKey = "PlayFab_CustomId";

    // ログイン ---------------------------------------------------------------------------------------------------------------------------------------[]
    public static async UniTask LoginAsync()
    {
        PlayFabSettings.staticSettings.TitleId = "1EF453";
        InfoRequestParams = new GetPlayerCombinedInfoRequestParams();
        InfoRequestParams.GetUserData = true;
        LoginResult loginResult = null;
        PlayFabAuthService.Instance.InfoRequestParams = InfoRequestParams;
        PlayFabAuthService.Instance.InitializeCallback();
        PlayFabAuthService.OnLoginSuccess += (result) => loginResult = result;
        // PlayFabAuthService.Instance.Authenticate(Authtypes.Silent);
        SimpleLogin();
        await UniTask.WaitUntil(() => _isLoginSuccess);
    }

    public static void LoginSuccess(LoginResult result)
    {
        playFabId = result.PlayFabId;
        UpdateRandomPlayfabId();
        Debug.Log("ログイン" + playFabId);
        _isLoginSuccess = true;
    }

    public static void SimpleLogin()
    {
        string customId = GetOrCreateCustomId();
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CustomId = customId,
            CreateAccount = true,
            InfoRequestParameters = InfoRequestParams
        }, (result) =>
        {
            LoginSuccess(result);

        }, (error) =>
        {
            Debug.LogError(error.GenerateErrorReport());

        });
    }

    private static string GetOrCreateCustomId()
    {
        // すでに保存されているIDがあれば使う
        if (PlayerPrefs.HasKey(_customIdKey))
        {
            return PlayerPrefs.GetString(_customIdKey);
        }

        // なければ新規で作成して保存
        string newId = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(_customIdKey, newId);
        PlayerPrefs.Save();
        return newId;
    }

    public static async UniTask InitializePrivateData()
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "Name", "" }
            }
        };

        UpdateUserDataResult result = null;
        PlayFabError error = null;
        PlayFabClientAPI.UpdateUserData(request, x => result = x, x => error = x);

        await UniTask.WaitUntil(() => result != null || error != null);

        if (result != null)
        {
            Debug.Log("InitializePlayerData");
        }
        else if (error != null)
        {
            Debug.Log("InitializeUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    public static async UniTask InitializePublicData()
    {
        var clearStates = new List<ClearState>();
        for (int i = 0; i < MasterManager.StageMasterList.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var clearState = new ClearState()
                {
                    MusicId = MasterManager.StageMasterList[i].MusicId,
                    StageId = j,
                };
                clearStates.Add(clearState);
            }
        }
        var settingData = new SettingData
        {
            BgmVolume = 0.5f,
            SeVolume = 0.5f,
        };
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "ClearStates", PlayFabSimpleJson.SerializeObject(clearStates) },
                { "SettingData", PlayFabSimpleJson.SerializeObject(settingData) }
            },
            Permission = UserDataPermission.Public
        };
        UpdateUserDataResult result = null;
        PlayFabError error = null;
        PlayFabClientAPI.UpdateUserData(request, x => result = x, x => error = x);

        await UniTask.WaitUntil(() => result != null || error != null);

        if (result != null)
        {
            await InitializePrivateData();
        }
        else if (error != null)
        {
            Debug.Log("InitializeUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

#region プレイヤーデータ取得
    // 自身の全てのデータを取得してSaveDataを更新
    public static async UniTask<PlayerDataResult> GetPlayerData()
    {
        var request = new GetUserDataRequest();
        GetUserDataResult result = null;
        PlayFabError error = null;
        PlayFabClientAPI.GetUserData(request, x => result = x, x => error = x);

        // データ取得まで待機
        await UniTask.WaitUntil(() => result != null || error != null);

        if (result != null)
        {
            Debug.Log("GetUserData: Success!");
            if (result.Data.ContainsKey("ClearStates") && result.Data.ContainsKey("SettingData"))
            {
                var clearStateList = PlayFabSimpleJson.DeserializeObject<List<ClearState>>(result.Data["ClearStates"].Value);
                var overrideMasterList = new List<StageMaster>();
                var customStageList = new List<SingleStageMaster>();
                foreach (var item in result.Data)
                {
                    if (item.Key.Contains("Override"))
                    {
                        overrideMasterList.Add(PlayFabSimpleJson.DeserializeObject<StageMaster>(item.Value.Value));
                    }
                    if (item.Key == "CustomStageList")
                    {
                        customStageList.AddRange(PlayFabSimpleJson.DeserializeObject<List<SingleStageMaster>>(item.Value.Value));
                    }
                }
                return new PlayerDataResult
                {
                    ClearStateList = clearStateList,
                    OverrideStageMasterList = overrideMasterList,
                    CustomStageList = customStageList
                };
            }
            else
            {
                await InitializePublicData();
                // 初期データを作ってから再取得
                return await GetPlayerData();
            }
        }
        else if (error != null)
        {
            Debug.Log("GetUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
        return null;
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
    public static async UniTask UpdateClearState(List<ClearState> clearStates)
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "ClearStates", PlayFabSimpleJson.SerializeObject(clearStates) }
            }
        };

        bool isSuccess = false;
        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);
        await UniTask.WaitUntil(() => isSuccess);

        void OnSuccess(UpdateUserDataResult result)
        {
            isSuccess = true;
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
        string keyName = stageMaster.MusicId + "Override";
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

    public static async UniTask UpdateCustomStageList(List<SingleStageMaster> customStageList)
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                // 現在のCustomStageListの状態をサーバーに保存
                { "CustomStageList", PlayFabSimpleJson.SerializeObject(customStageList) }
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
    public static async UniTask<TitleDataResult> GetTitleData()
    {
        var request = new GetTitleDataRequest();
        GetTitleDataResult result = null;
        PlayFabError error = null;
        PlayFabClientAPI.GetTitleData(request, x => result = x, x => error = x);

        // データ取得まで待機
        await UniTask.WaitUntil(() => result != null || error != null);

        if (result != null)
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
            return new TitleDataResult
            {
                SettingeMaster = settingData,
                StageMasterList = stageMasterList
            };
        }
        else if (error != null)
        {
            Debug.Log("GetTitleData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
        return null;
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