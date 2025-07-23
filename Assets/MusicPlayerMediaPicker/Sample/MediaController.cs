using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using SFB;
using System;
using System.Runtime.InteropServices;
using System.IO;

public class MediaController : MonoBehaviour 
{
    private string _currentSongId;
    private bool _isFinishExport;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        public static extern void exportItemFromId(IntPtr songId);

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
        private static void exportItemFromId(IntPtr songId) { }
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
        private static bool getDoExport() { return false; }
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
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(songId + "\0");
        var unmanagedPtr = Marshal.AllocHGlobal(utf8Bytes.Length);
        Marshal.Copy(utf8Bytes, 0, unmanagedPtr, utf8Bytes.Length);
        exportItemFromId(unmanagedPtr);
        Marshal.FreeHGlobal(unmanagedPtr);
        await UniTask.WaitWhile(() => getDoExport());

        string path = GetMusicPath(songId);
#if UNITY_IOS && !UNITY_EDITOR
        if (!File.Exists(path))
        {
            Debug.LogError("ファイルが存在しません: " + path);
        }
#endif
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
                Debug.Log("return clip: " + clip);
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

    void OnNativeLog(string message)
    {
        Debug.Log("[iOS] " + message);
    }
}
