using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

public enum BallType
{
    Single,
    LongStart,
    LongEnd
}

public class Ball : MonoBehaviour
{
    [SerializeField] private Image _ballImage;

    public bool IsLeft;
    public float CriticalTime;
    public BallType BallType;
    public CancellationTokenSource Cts;

    private Vector3 _leftTargetPosition = new Vector3(300, 600, 0);
    private Vector3 _rightTargetPosition = new Vector3(-300, 600, 0);
    public Tweener MoveTween;
    public Subject<Unit> OnWhenDestroy = new Subject<Unit>();
    public Subject<Unit> OnWhenClicked = new Subject<Unit>();

    public virtual void Init(bool isleft, float criticalTime, BallType ballType, CancellationTokenSource cts)
    {
        _ballImage.color = ballType == BallType.Single ? Color.red : Color.blue;
        CriticalTime = criticalTime;
        BallType = ballType;
        IsLeft = isleft;
        Cts = cts;
    }

    public void SetTween(Tweener tweener)
    {
        MoveTween = tweener;
    }

    void OnDestroy()
    {
        MoveTween.Kill();
        OnWhenDestroy.OnNext(default);
    }
}
