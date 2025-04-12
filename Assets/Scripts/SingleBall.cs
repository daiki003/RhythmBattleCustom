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
    public bool IsActive(float currentTime) => IsAlive && JudgeBall(currentTime) >= HitType.Hit;

    public Subject<BallState> OnWhenSetBallState = new();
    public Subject<Unit> OnWhenMiss = new();
    public Subject<SingleBall> OnWhenDestroyed = new();

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

    public HitType JudgeBall(float currentTime)
    {
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

    public void SetBallState(BallState state)
    {
        if (BallState == state)
        {
            return;
        }
        BallState = state;
        OnWhenSetBallState.OnNext(state);
        switch (state)
        {
            case BallState.Wait:
                if (BallType != BallType.LongEnd)
                {
                    gameObject.SetActive(false);
                }
                break;
            case BallState.Launched:
                gameObject.SetActive(true);
                break;
            case BallState.End:
                if (MoveTween != null)
                {
                    MoveTween.Kill();
                    MoveTween = null;
                }
                gameObject.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// 現在の時間にふさわしいBallStateを取得
    /// </summary>
    public BallState GetShouldBeState(float currentTime)
    {
        if (CriticalTime < currentTime)
        {
            return BallType == BallType.LongStart ? BallState.Holded : BallState.End;
        }
        else if (LaunchTime < currentTime)
        {
            return BallState.Launched;
        }
        else
        {
            return BallState.Wait;
        }
    }

    public void AfterBeat(bool isRelease)
    {
        if (isRelease && BallType == BallType.LongEnd ||
            !isRelease && BallType == BallType.Single)
        {
            SetBallState(BallState.End);
        }
        else if (!isRelease && BallType == BallType.LongStart)
        {
            BallState = BallState.Holded;
            StopMove();
        }
    }

    public void StopMove()
    {
        if (MoveTween != null && MoveTween.IsActive())
        {
            MoveTween.Kill();
            MoveTween = null;
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
            SetBallState(BallState.End);
            return;
        }
    }
}
