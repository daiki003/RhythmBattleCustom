using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEManager : MonoBehaviour
{
    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioClip _beatSe;
    [SerializeField] private AudioClip _buttonSe;
    [SerializeField] private AudioClip _battleStartSe;

    public float Volume => _seSource.volume;

    public static SEManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

    public void AdjustVolume(float volume)
	{
		_seSource.volume = volume;
	}

    public void PlayBeatSe()
    {
        _seSource.PlayOneShot(_beatSe);
    }

    public void PlayButtonSe()
    {
        _seSource.PlayOneShot(_buttonSe);
    }

    public void PlayBattleStartSe()
    {
        _seSource.PlayOneShot(_battleStartSe);
    }
}
