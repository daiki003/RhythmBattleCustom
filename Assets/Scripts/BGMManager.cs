using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    [SerializeField] private AudioSource _bgmSource;
	private AudioClip _currentBgmClip;

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
		_currentBgmClip = Resources.Load<AudioClip>("BGM/" + clipPath);
		_bgmSource.clip = _currentBgmClip;
	}

	public void Play()
	{
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
