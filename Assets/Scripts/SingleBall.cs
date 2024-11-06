using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.Profiling;
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
    public Subject<Unit> OnWhenMiss = new Subject<Unit>();
    public Subject<SingleBall> OnWhenDestroyed = new Subject<SingleBall>();

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

    public HitType JudgeBall()
    {
        float currentTime = BGMManager.instance.CurrentTime;
        float criticalTimeBuffer = MasterManager.SettingMaster.CriticalTimeBuffer;
        float hitTimeBuffer = MasterManager.SettingMaster.HitTimeBuffer;
        if (CriticalTime > currentTime - criticalTimeBuffer && CriticalTime < currentTime + criticalTimeBuffer)
        {
            return HitType.Critical;
        }
        else if (CriticalTime > currentTime - hitTimeBuffer && CriticalTime < currentTime + hitTimeBuffer)
        {
            return HitType.Hit;
        }
        return HitType.None;
    }

    public bool OnClickButton()
    {
        if (BallType == BallType.LongEnd)
        {
            return false;
        }
        OnWhenClicked.OnNext(default);
        if (BallType == BallType.Single)
        {
            Destroy(gameObject);
        }
        else if (BallType == BallType.LongStart)
        {
            BallState = BallState.Holded;
        }
        return true;
    }

    public bool OnReleaseButton()
    {
        if (BallType == BallType.LongEnd)
        {
            return false;
        }
        OnWhenClicked.OnNext(default);
        if (BallType == BallType.Single)
        {
            Destroy(gameObject);
        }
        else if (BallType == BallType.LongStart)
        {
            BallState = BallState.Holded;
        }
        return true;
    }

    void OnDestroy()
    {
        OnWhenDestroyed.OnNext(this);
    }

    void Update()
    {
        Profiler.BeginSample("BallUpdate1");
        float currentTime = BGMManager.instance.CurrentTime;
        Profiler.EndSample();
        Profiler.BeginSample("BallUpdate2");
        if (BallState == BallState.Holded || currentTime < LaunchTime)
        {
            return;
        }
        Profiler.EndSample();
        Profiler.BeginSample("BallUpdate3");
        if (currentTime > CriticalTime + MasterManager.SettingMaster.HitTimeBuffer)
        {
            OnWhenMiss.OnNext(default);
            Destroy(gameObject);
            return;
        }
        Profiler.EndSample();
        Profiler.BeginSample("BallUpdate4");
        float timeRate = 1 - (CriticalTime - currentTime) / MasterManager.SettingMaster.BallTimeOffset;
        Profiler.EndSample();
        Profiler.BeginSample("BallUpdate5");
        int xDirection = IsLeft ? -1 : 1;
        Profiler.EndSample();
        Profiler.BeginSample("BallUpdate6");
        transform.localPosition = new Vector3(xDirection * (70 + (140 * timeRate)), 400 - (970 * timeRate), 0);
        Profiler.EndSample();
    }
}
