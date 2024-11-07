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

    public static BGMManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	public void SetClip(string clipPath)
	{
		_currentBgmClip = Resources.Load<AudioClip>(string.Format("BGM/{0}/Main", clipPath));
		_currentIntroClip = Resources.Load<AudioClip>(string.Format("BGM/{0}/Intro", clipPath));
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

	public void SetTime(float time)
	{
		_bgmSource.time = time;
	}
}
