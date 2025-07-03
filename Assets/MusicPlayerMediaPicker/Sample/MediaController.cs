using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using R3;
using UnityEngine.Networking;

public class MediaController : MonoBehaviour 
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _loadButton;

    private string _currentSongId;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        public static extern void exportItemFromId(string songId);

        [DllImport("__Internal")]
        public static extern void exportSelectedItem();

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
        private static void exportSelectedItem() { }

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

	void Start()
    {
        _playButton?.OnClickAsObservable().Subscribe(async _ => await StartMusicAsync()).AddTo(this);
        _loadButton?.OnClickAsObservable().Subscribe(_ => MusicExpote()).AddTo(this);
	}

    private void MusicExpote()
    {
        // 曲エクスポートを開始
        exportSelectedItem();
    }

    public void StartMusic(string songId)
    {
        _currentSongId = songId;
    }

    public async UniTask StartMusicAsync()
    {
        // 曲エクスポート完了まで待つ
        await UniTask.WaitWhile(() => getDoExport());

        string path = Application.persistentDataPath + "/" + _currentSongId + ".wav";

        _audioSource.clip = await GetAudioClipAsync();

        _audioSource.Play();
        
    	// wavファイルを削除
        System.IO.File.Delete(path);
    }

    public async UniTask<AudioClip> GetAudioClipAsync()
    {
        string path = Application.persistentDataPath + "/" + _currentSongId + ".wav";
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
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
                return clip;
            }
        }
    }
}
