using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using R3;
using UnityEngine.Networking;
using SFB;

public class MediaController : MonoBehaviour 
{
    private string _currentSongId;
    private bool _isFinishExport;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        public static extern void exportItemFromId(string songId);

        [DllImport("__Internal")]
        public static extern void selectMusic();

        [DllImport("__Internal")]
        public static extern long getSongId();

        [DllImport("__Internal")]
        public static extern string getSongName();

        [DllImport("__Internal")]
        public static extern bool getDoExport();

        [DllImport("__Internal")]
        public static extern string getLog();
#else
        private static void exportItemFromId(string songId) { }
        private static void selectMusic()
        {
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select MP3", "", "mp3", false);
            string path = "";
            if (paths.Length > 0)
            {
                path = paths[0];
            }
            instance.FinishSelectMusic(path);
        }

        private static long getSongId() { return 0; }
        private static string getSongName() { return ""; }
        private static bool getDoExport() { return true; }
        public static string getLog() { return ""; }

#endif

    public static MediaController instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

    public async UniTask<string> MusicExpote()
    {
        _isFinishExport = false;
        // 曲選択開始
        selectMusic();

        await UniTask.WaitUntil(() => _isFinishExport);
        return _currentSongId;
    }

    public void FinishSelectMusic(string songId)
    {
        _currentSongId = songId;
        _isFinishExport = true;
    }

    public async UniTask<AudioClip> GetAudioClipAsync(string songId)
    {
        // 曲をエクスポート
        exportItemFromId(songId);
        await UniTask.WaitUntil(() => getDoExport());

        string path = GetMusicPath(songId);
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, GetAudioType()))
        {
            await www.SendWebRequest();

    #if UNITY_2020_1_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
    #else
            if (www.isNetworkError || www.isHttpError)
    #endif
            {
                Debug.LogError("❌ Failed to load audio: " + www.error);
                return null;
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
#if UNITY_IOS && !UNITY_EDITOR
                // wavファイルを削除
                System.IO.File.Delete(path);
#endif
                return clip;
            }
        }
    }

    private string GetMusicPath(string songId)
    {
#if UNITY_IOS && !UNITY_EDITOR
        // 曲のパスを取得
        return "file://" + Application.persistentDataPath + "/" + songId + ".wav";
#else
        // StandaloneFileBrowserを使用して曲のパスを取得
        return "file://" + songId;
#endif
    }

    private AudioType GetAudioType()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // 曲のパスを取得
        return AudioType.WAV;
#else
        // StandaloneFileBrowserを使用して曲のパスを取得
        return AudioType.UNKNOWN;
#endif
    }
}
