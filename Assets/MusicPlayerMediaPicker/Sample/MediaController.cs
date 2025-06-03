using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using R3;

public class MediaController : MonoBehaviour 
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Text text;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        public static extern void exportRandomToItem();

        [DllImport("__Internal")]
        public static extern void exportSelectedItem();

        [DllImport("__Internal")]
        public static extern long getSongId();

        [DllImport("__Internal")]
        public static extern string getSongName();

        [DllImport("__Internal")]
        public static extern bool getDoExport();
#else
        private static void exportRandomToItem() { }
        private static void exportSelectedItem() { }

        private static long getSongId() { return 0; }
        private static string getSongName() { return ""; }
        private static bool getDoExport() { return false; }

#endif

	void Start()
    {
        _playButton.OnClickAsObservable().Subscribe(async _ => await MusicImport()).AddTo(this);
	}

    private async UniTask MusicImport()
    {
        text.text = "楽曲エクスポート中";

        // 曲エクスポートを開始
        exportSelectedItem();

        // 曲エクスポート完了まで待つ
        await UniTask.WaitWhile(() => getDoExport());

        text.text = "楽曲インポート中";

        // Documentsにある曲を取得
        string path = Application.persistentDataPath + "/" + getSongId() + ".wav";
        WWW www = new WWW("file://" + path);

        // インポートが完了するまで待つ
        await UniTask.WaitUntil(() => www.isDone);

        _audioSource.clip = www.GetAudioClip(false, false);
	    
	    text.text = "再生します！";

        _audioSource.Play();

        text.text = getSongName();
	    
    	// wavファイルを削除
        System.IO.File.Delete(path);
    }
}
