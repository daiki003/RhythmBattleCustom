using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using System;
using System.Collections.Generic;

public class PlayFabController
{
    private static bool newAccount; //アカウントを新規作成したかどうか
    private static string playFabId; //自分のID
    public static string randomPlayFabId; //直近で取得した他の人のID
    private static readonly string ID_CHARACTERS = "0123456789"; //IDに使用する文字
    [SerializeField] static GetPlayerCombinedInfoRequestParams InfoRequestParams;

    [HideInInspector] static public string PlayerName { get; private set; }
    [HideInInspector] static public List<CharacterResult> Characters { get; private set; }

    // ログイン ---------------------------------------------------------------------------------------------------------------------------------------[]
    public static void login()
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
        if (result.NewlyCreated)
        {
            initializePublicData(getPlayerData);
        }
        else
        {
            getPlayerData();
        }
        UpdateRandomPlayfabId();
        GameManager.instance.GetAllMasterData();
        Debug.Log("ログイン" + playFabId);
    }

    public static void simpleLogin(string customId)
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

    public static void initializePrivateData(Action callBack)
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "Name", "" },
                { "AllDecks", "" }
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

    public static void initializePublicData(Action callBack)
    {
        var request = new UpdateUserDataRequest()
        {
            Data = new Dictionary<string, string>
            {
                { "MainDeck", "" }
            },
            Permission = UserDataPermission.Public
        };

        PlayFabClientAPI.UpdateUserData(request, OnSuccess, OnError);

        void OnSuccess(UpdateUserDataResult result)
        {
            initializePrivateData(callBack);
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("InitializeUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    // プレイヤーデータ取得 ------------------------------------------------------------------------------------------------------------------------
    // 自身の全てのデータを取得してSaveDataを更新
    public static void getPlayerData()
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, OnSuccess, OnError);

        void OnSuccess(GetUserDataResult result)
        {
            Debug.Log("GetUserData: Success!");
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("GetUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    // プレイヤーデータ操作 ------------------------------------------------------------------------------------------------------------------------------
    public static void updatePlayerName(string playerName)
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
            getPlayerData();
            Debug.Log("UpdateUserData: Success!");
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("UpdateUserData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    // タイトルデータ取得
    public static void GetTitleData(Action<SettingMaster> callBack)
    {
        var request = new GetTitleDataRequest();
        PlayFabClientAPI.GetTitleData(request, OnSuccess, OnError);

        void OnSuccess(GetTitleDataResult result)
        {
            Debug.Log("GetTitleData: Success!");

            SettingMaster settingData = PlayFabSimpleJson.DeserializeObject<SettingMaster>(result.Data["Setting"]);
            callBack(settingData);
        }

        void OnError(PlayFabError error)
        {
            Debug.Log("GetTitleData: Fail...");
            Debug.Log(error.GenerateErrorReport());
        }
    }

    // ランキング関連 ---------------------------------------------------------------------------------------------------------------------------------------
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
}