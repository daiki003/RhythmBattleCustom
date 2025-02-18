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

    public bool IsAlive => BallState < BallState.Holded;
    public bool IsActive => IsAlive && JudgeBall() >= HitType.Hit;

    public Subject<Unit> OnWhenLaunched = new Subject<Unit>();
    public Subject<Unit> OnWhenMiss = new Subject<Unit>();
    public Subject<SingleBall> OnWhenDestroyed = new Subject<SingleBall>();
    public Subject<SingleBall> OnWhenEnd = new Subject<SingleBall>();

    public Tweener MoveTween;

    public void Init(NoteMaster noteMaster, float criticalTime, BallType ballType, float ballTimeOffset)
    {
        string ballSpritePath = ballType == BallType.Single ? "Ball/Single" : "Ball/Long";
        _ballImage.sprite = Resources.Load<Sprite>(ballSpritePath);
        _ballImage.color = ballType == BallType.Single ? Color.red : Color.blue;
        CriticalTime = criticalTime;
        LaunchTime = criticalTime - ballTimeOffset;
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

    public void SetTween(Tweener tweener)
    {
        MoveTween = tweener;
    }

    public void Launch()
    {
        gameObject.SetActive(true);
        OnWhenLaunched.OnNext(default);
        BallState = BallState.Launched;
    }

    public void SetEnd()
    {
        BallState = BallState.End;
        gameObject.SetActive(false);
        OnWhenEnd.OnNext(this);
    }

    public void AfterBeat(bool isRelease)
    {
        if (isRelease && BallType == BallType.LongEnd ||
            !isRelease && BallType == BallType.Single)
        {
            SetEnd();
        }
        else if (!isRelease && BallType == BallType.LongStart)
        {
            BallState = BallState.Holded;
        }
    }

    public void StopMove()
    {
        if (MoveTween != null && MoveTween.IsActive())
        {
            MoveTween.Kill();
        }
    }

    void OnDestroy()
    {
        OnWhenDestroyed.OnNext(this);
        StopMove();
    }

    public void ViewUpdate(float bgmTime, bool isPause)
    {
        if (isPause)
        {
            return;
        }
        if (BallState == BallState.Holded || bgmTime < LaunchTime)
        {
            StopMove();
            return;
        }
        if (BallState != BallState.End && bgmTime > CriticalTime + MasterManager.SettingMaster.HitTimeBuffer)
        {
            OnWhenMiss.OnNext(default);
            SetEnd();
            return;
        }
        float timeRate = 1 - (CriticalTime - bgmTime) / MasterManager.SettingMaster.BallTimeOffset;
        int xDirection = IsLeft ? -1 : 1;
    }
}
