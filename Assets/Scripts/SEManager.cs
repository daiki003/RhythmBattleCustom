using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SeName
{
    BattleStart,
    Beat,
    Button1,
    Button2,
    Button3,
    Button4,
    Cancel,
    ChangePage,
    ChangeScene,
}

public class SEManager : MonoBehaviour
{
    [SerializeField] private AudioSource _seSource;

    private Dictionary<string, AudioClip> _chachClipDict = new();

    public float Volume => _seSource.volume;

    public static SEManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

    public void PreloadSe()
	{
		foreach (string name in Enum.GetNames(typeof(SeName)))
		{
			LoadClip(name);
		}
	}

	private AudioClip GetClip(string clipName)
	{
		if (_chachClipDict.TryGetValue(clipName, out var clip))
		{
			return clip;
		}
		// キャッシュになければロード
		return LoadClip(clipName);
	}

	private AudioClip LoadClip(string clipName)
	{
		var newClip = Resources.Load<AudioClip>(string.Format("SE/{0}", clipName));
		_chachClipDict.Add(clipName, newClip);
		return newClip;
	}

    public void AdjustVolume(float volume)
	{
		_seSource.volume = volume;
	}

    public void PlaySe(SeName seName)
    {
        var clip = GetClip(seName.ToString());
        _seSource.PlayOneShot(clip);
    }
}
