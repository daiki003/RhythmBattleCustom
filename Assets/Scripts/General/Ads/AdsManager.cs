using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using Cysharp.Threading.Tasks;

public static class AdsManager
{
    private static BannerView _bannerView;
    private const string _testBannerUnitId = "ca-app-pub-3940256099942544/2934735716";
    private const string _testInterstitialUnitId = "ca-app-pub-3940256099942544/4411468910";
    private const string _testRewardUnitId = "ca-app-pub-3940256099942544/1712485313";
    private const string _interstitialUnitId = "ca-app-pub-4957358157988887/8785541564";
    private const string _rewardUnitId = "ca-app-pub-4957358157988887/5349649426";
    private const int _interstitialInterval = 3; // インタースティシャル広告表示の間隔
    private const int _rewardInterval = 1; // リワード広告表示の間隔

    private static int _interstitialCount;

    public static void InitAds()
    {
        MobileAds.Initialize(initStatus => { });
    }

    public static void ShowBanner()
    {
        // バナー作成
        _bannerView = new BannerView(_testBannerUnitId, AdSize.Banner, AdPosition.Bottom);
        // バナーをロード
        AdRequest request = new();
        _bannerView.LoadAd(request);
    }

    public static void HideBanner()
    {
        _bannerView.Hide();
    }

    public static async UniTask ShowInterstitialAsync()
    {
        return;
        bool finishAds = false;
        bool isPlayingBgm = BGMManager.instance.IsPlaying;
        InterstitialAd.Load(_interstitialUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("Interstitial ad failed to load: " + error.GetMessage());
                finishAds = true;
                return;
            }
            if (ad == null)
            {
                finishAds = true;
                return;
            }

            ad.OnAdFullScreenContentClosed += () =>
            {
                // 広告が閉じられたときの処理
                ad.Destroy();
                finishAds = true;
            };
            // 広告がロードされたら表示
            BGMManager.instance.Pause();
            ad.Show();
            _interstitialCount = 0;
        });
        await UniTask.WaitUntil(() => finishAds);
        if (isPlayingBgm)
        {
            BGMManager.instance.Restart();
        }
    }

    public static async UniTask<bool> ShowRewardAsync()
    {
        bool finishAds = false;
        bool rewardEarned = false;
        RewardedAd.Load(_rewardUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("Interstitial ad failed to load: " + error.GetMessage());
                finishAds = true;
                return;
            }
            if (ad == null)
            {
                finishAds = true;
                return;
            }

            ad.OnAdFullScreenContentClosed += () =>
            {
                // 広告が閉じられたときの処理
                ad.Destroy();
                finishAds = true;
            };
            BGMManager.instance.Pause();
            // 広告がロードされたら表示
            ad.Show(reward =>
            {
                if (reward != null)
                {
                    rewardEarned = true;
                }
            });
        });
        await UniTask.WaitUntil(() => finishAds);
        BGMManager.instance.Restart();
        return rewardEarned;
    }
}
