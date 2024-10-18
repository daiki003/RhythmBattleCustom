using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class SingleBall : MonoBehaviour
{
    [SerializeField] private Image _ballImage;
    public bool IsLeft;
    public float CriticalTime;
    public float LaunchTime;
    public BallType BallType;
    public BallState BallState;

    public Subject<Unit> OnWhenLaunched = new Subject<Unit>();
    public Subject<Unit> OnWhenClicked = new Subject<Unit>();
    public Subject<SingleBall> OnWhenDestroyed = new Subject<SingleBall>();
    public Tweener MoveTween;

    public void Init(NoteMaster noteMaster, float criticalTime, BallType ballType)
    {
        string ballSpritePath = ballType == BallType.Single ? "Ball/Single" : "Ball/Long";
        _ballImage.sprite = Resources.Load<Sprite>(ballSpritePath);
        // _ballImage.color = ballType == BallType.Single ? Color.red : Color.blue;
        CriticalTime = criticalTime;
        LaunchTime = criticalTime - MasterManager.SettingMaster.BallTimeOffset;
        BallType = ballType;
        IsLeft = noteMaster.block <= 3;
    }

    public void SetTween(Tweener tweener)
    {
        MoveTween = tweener;
    }
    void OnDestroy()
    {
        OnWhenDestroyed.OnNext(this);
        if (MoveTween != null)
        {
            MoveTween.Kill();
        }
    }
}
