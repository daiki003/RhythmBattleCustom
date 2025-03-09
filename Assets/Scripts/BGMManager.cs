using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using DG.Tweening;
using System.Threading;

public enum BgmName
{
	Title,
	WanderersCity,
	Result
}

public class BGMManager : MonoBehaviour
{
	[SerializeField] private AudioSource _bgmSource;
	private AudioClip _currentBgmClip;
	private Dictionary<string, AudioClip> _chachClipDict = new();
	private float _startTime;
	private float _endTime;
	private bool _isDuringLoopFade;
	private float _homeBgmTime;

	public bool IsFinishBgm => _currentBgmClip != null && _currentBgmClip.length <= _bgmSource.time;
	public float CurrentTime => _bgmSource.time;
	public float Length => _currentBgmClip.length;
	public float CurrentTimeLate => CurrentTime / Length;
	public float CurrentClipLength => _currentBgmClip.length;
	public float Volume => _bgmSource.volume;
	public bool IsPlaying => _bgmSource.isPlaying;

	private CancellationTokenSource _fadeCts;

	private const float _fadeDuration = 1f;
	private const float _maxTimeCofficient = 0.999f;
	private string _homeBgmName => BgmName.WanderersCity.ToString();

    public static BGMManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	async void Update()
    {
        if (_endTime.IsBetween(0, _bgmSource.time) && !_isDuringLoopFade)
		{
			_isDuringLoopFade = true;
			await FadeLoopAsync();
			_isDuringLoopFade = false;
		}
    }

	public void AdjustVolume(float volume)
	{
		_bgmSource.volume = volume;
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

	public void SetClip(BgmName bgmName, bool isLoop = false, bool immediatePlay = true, bool isFade = false)
	{
		SetClip(bgmName.ToString(), isLoop, immediatePlay, isFade);
	}

	public void SetClip(string clipName, bool isLoop = false, bool immediatePlay = true, bool isFade = false, float startTime = 0f, float endTime = 0f)
	{
		_bgmSource.loop = isLoop;
		_startTime = startTime;
		_endTime = endTime;
		var newClip = GetClip(clipName);
		if (_currentBgmClip == newClip)
		{
			return;
		}
		SaveHomeBgmTime();
		_currentBgmClip = newClip;
		SetHomeBgmTime();
		if (immediatePlay)
		{
			Play(isFade);
		}
	}

	public void Play(bool isFade = false)
	{
		_bgmSource.clip = _currentBgmClip;
		_bgmSource.time = _startTime;
		_bgmSource.Play();
		if (isFade)
		{
			CancelFade();
			_bgmSource.volume = 0f;
			DOTween.To(() => _bgmSource.volume, (value) => _bgmSource.volume = value, SaveDataManager.SettingData.BgmVolume, _fadeDuration).ToUniTask(cancellationToken: _fadeCts.Token);
		}
	}

	private async UniTask FadeLoopAsync()
	{
		CancelFade();
		await DOTween.To(() => _bgmSource.volume, (value) => _bgmSource.volume = value, 0f, _fadeDuration).ToUniTask(cancellationToken: _fadeCts.Token);
		_bgmSource.time = _startTime;
		await DOTween.To(() => _bgmSource.volume, (value) => _bgmSource.volume = value, SaveDataManager.SettingData.BgmVolume, _fadeDuration).ToUniTask(cancellationToken: _fadeCts.Token);
	}

	private void CancelFade()
	{
		if (_fadeCts != null)
		{
			_fadeCts.Cancel();
			_fadeCts.Dispose();
		}
		_fadeCts = new CancellationTokenSource();
	}

	public void Stop()
	{
		_bgmSource.Stop();
	}

	public void Restart()
	{
		_bgmSource.Play();
	}

	public void Pause()
	{
		_bgmSource.Pause();
	}

	public void ChangePause()
	{
		if (_bgmSource.isPlaying)
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
		_bgmSource.time = Mathf.Min(time, Length * _maxTimeCofficient);
	}

	// ホームのBgmから変えるとき、再生時間を保存しておく
	private void SaveHomeBgmTime()
	{
		if (_currentBgmClip != null && _currentBgmClip.name == _homeBgmName)
		{
			_homeBgmTime = _bgmSource.time;
		}
	}

	// ホームのBgmに変えるとき、保存した再生時間に戻す
	private void SetHomeBgmTime()
	{
		if (_currentBgmClip != null && _currentBgmClip.name == _homeBgmName)
		{
			_startTime = _homeBgmTime;
		}
	}

	public void ResetHomeBgmTime()
	{
		_homeBgmTime = 0f;
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
