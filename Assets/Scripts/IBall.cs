using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;

public enum BallType
{
    Single,
    LongStart,
    LongEnd,
    Long,
}

public enum BallState
{
    Wait,
    Launched,
}

public class IBall : MonoBehaviour
{
    public bool IsLeft;
    public float CriticalTime;
    public float LaunchTime;
    public BallType BallType;
    public BallState BallState;

    public Subject<Unit> OnWhenClicked = new Subject<Unit>();
    public Subject<SingleBall> OnWhenDestroyed = new Subject<SingleBall>();
    public Tweener MoveTween;

    public void SetTween(Tweener tweener)
    {
        MoveTween = tweener;
    }
    public virtual void LaunchCheck(float time)
    {

    }
}
