using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

public enum BgmName
{
	WanderersCity,
	Result
}

public class BGMManager : MonoBehaviour
{
	[SerializeField] private AudioSource _bgmSource;
	private AudioClip _currentBgmClip;
	private Dictionary<string, AudioClip> _chachClipDict = new();

	public bool IsFinishBgm => _currentBgmClip != null && _currentBgmClip.length <= _bgmSource.time;
	public float CurrentTime => _bgmSource.time;
	public float CurrentClipLength => _currentBgmClip.length;
	public bool IsPlaying => _bgmSource.isPlaying;

    public static BGMManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	public void PreloadBgm()
	{
		foreach (string name in Enum.GetNames(typeof(BgmName)))
		{
			LoadClip(name);
		}
		foreach (string name in MasterManager.StageMasterList.Select(m => m.StageId))
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
		var newClip = Resources.Load<AudioClip>(string.Format("BGM/{0}", clipName));
		_chachClipDict.Add(clipName, newClip);
		return newClip;
	}

	public void SetClip(BgmName bgmName, bool isLoop = false, bool immediatePlay = true)
	{
		SetClip(bgmName.ToString(), isLoop, immediatePlay);
	}

	public void SetClip(string clipName, bool isLoop = false, bool immediatePlay = true)
	{
		_currentBgmClip = GetClip(clipName);
		_bgmSource.loop = isLoop;
		if (immediatePlay)
		{
			Play();
		}
	}

	public void Play()
	{
		_bgmSource.clip = _currentBgmClip;
		_bgmSource.Play();
	}

	public void Stop()
	{
		_bgmSource.Stop();
	}

	public void Pause(bool forcePause = false)
	{
		if (_bgmSource.isPlaying || forcePause)
		{
			_bgmSource.Pause();
		}
		else
		{
			_bgmSource.Play();
		}
	}

	public void SetTime(float time)
	{
		_bgmSource.time = time;
	}

	public void SetTimeByRate(float rate, float offset)
	{
		_bgmSource.time = MathF.Max(_currentBgmClip.length * Mathf.Clamp(rate, 0, 1) + offset, 0);
	}

	public float GetCurrentTimeLate(float offset)
	{
		return Mathf.Clamp((_bgmSource.time - offset) / _currentBgmClip.length, 0f, 1f);
	}
}
