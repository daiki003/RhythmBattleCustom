using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public static class AdsManager
{
    private static BannerView _bannerView;
    private static InterstitialAd _interstitial;
    private static RewardedAd rewardedAd;
    private const string _testBannerUnitId = "ca-app-pub-3940256099942544/2934735716";
    private const string _testInterstitialUnitId = "ca-app-pub-3940256099942544/4411468910";
    private const string _testRewardUnitId = "ca-app-pub-3940256099942544/2934735716";

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
}
