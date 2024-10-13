using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Ball : MonoBehaviour
{
    [SerializeField] private Image _ballImage;

    public bool IsLeft;
    public float CriticalTime;
    public CancellationTokenSource Cts;

    private Vector3 _leftTargetPosition = new Vector3(300, 600, 0);
    private Vector3 _rightTargetPosition = new Vector3(-300, 600, 0);
    public Tweener moveTween;

    public void Init(bool isleft, float criticalTime, CancellationTokenSource cts)
    {
        _ballImage.color = Color.red;
        CriticalTime = criticalTime;
        IsLeft = isleft;
        Cts = cts;
    }

    public void SetTween(Tweener tweener)
    {
        moveTween = tweener;
    }

    void OnDestroy()
    {
        moveTween.Kill();
    }
}
