#if UNITY_EDITOR
using System;
using UnityEngine;

/// <summary>
/// スクリーンショットをキャプチャするサンプル
/// </summary>
public static class ScreenShot
{
    private const string _screenShotPath = "ScreenShots/";
    // 画面全体のスクリーンショットを保存する
    public static void CaptureScreenShot()
    {
        ScreenCapture.CaptureScreenshot(_screenShotPath + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
    }
}
#endif