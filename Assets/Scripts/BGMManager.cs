using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
	[SerializeField] private AudioSource _bgmSource;
	private AudioClip _currentBgmClip;
	private AudioClip _currentIntroClip;

	public bool IsFinishBgm => _currentBgmClip != null && _currentBgmClip.length <= _bgmSource.time;
	public float CurrentTime => _bgmSource.time;
	public float CurrentTimeRate => _bgmSource.time / _currentBgmClip.length;
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

	public void SetClip(string clipPath, bool isLoop = false)
	{
		_currentBgmClip = Resources.Load<AudioClip>(string.Format("BGM/{0}", clipPath));
		_bgmSource.loop = isLoop;
	}

	public async UniTask PlayFromIntro()
	{
		if (_currentIntroClip != null)
		{
			_bgmSource.clip = _currentIntroClip;
			_bgmSource.Play();
		}
		await UniTask.WaitWhile(() => _bgmSource.isPlaying);
		_bgmSource.clip = _currentBgmClip;
		_bgmSource.Play();
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
