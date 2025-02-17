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
    public Subject<Unit> OnWhenMiss = new Subject<Unit>();
    public Subject<SingleBall> OnWhenDestroyed = new Subject<SingleBall>();

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

    public void PauseMove()
    {
        if (MoveTween != null && MoveTween.IsActive())
        {
            MoveTween.Pause();
        }
    }

    public void PlayMove()
    {
        if (MoveTween != null && MoveTween.IsActive())
        {
            MoveTween.Play();
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

    void Update()
    {
        float currentTime = BGMManager.instance.CurrentTime;
        if (BallState == BallState.Holded || currentTime < LaunchTime)
        {
            StopMove();
            return;
        }
        if (currentTime > CriticalTime + MasterManager.SettingMaster.HitTimeBuffer)
        {
            OnWhenMiss.OnNext(default);
            Destroy(gameObject);
            return;
        }
        float timeRate = 1 - (CriticalTime - currentTime) / MasterManager.SettingMaster.BallTimeOffset;
        int xDirection = IsLeft ? -1 : 1;
    }
}
