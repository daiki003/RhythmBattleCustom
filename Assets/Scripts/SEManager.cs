using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEManager : MonoBehaviour
{
    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioClip _beatSe;

    public static SEManager instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
        // フレームレート設定（FPS60にしたい場合）
        Application.targetFrameRate = 60;
	}

    public void PlayBeatSe()
    {
        _seSource.PlayOneShot(_beatSe);
    }
}
