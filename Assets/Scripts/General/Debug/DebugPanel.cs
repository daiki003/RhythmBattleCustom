using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour
{
    [SerializeField] private GameObject _bgPanel;
    [SerializeField] private Text _debugText;
    [SerializeField] private Button _showLogButton;

    public static DebugPanel instance;
	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
        _showLogButton.OnClickAsObservable().Subscribe(_ =>
        {
            _bgPanel.SetActive(!_bgPanel.activeSelf);
        }).AddTo(this);
        _debugText.text = "ログ\n";
        _bgPanel.SetActive(false);
	}

    public void AddLog(string logText)
    {
        string currentTime = DateTime.Now.ToString("HH:mm:ss") + ":";
        _debugText.text += currentTime + logText + "\n";
    }
}
