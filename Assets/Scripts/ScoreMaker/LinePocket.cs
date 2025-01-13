using System.Collections;
using System.Collections.Generic;
using R3;
using Unity.VisualScripting;
using UnityEngine;

public class LinePocket : MonoBehaviour
{
    [SerializeField] private Transform _ballTransform;
    public Subject<ScoreMakerBall> OnWhenDropedBall = new Subject<ScoreMakerBall>();

    public ScoreMakerBall InstalledBall;

    public int Number { get; private set; }
    public bool IsLeft { get; private set; }

    public void SetParam(int number, bool isLeft)
    {
        Number = number;
        IsLeft = isLeft;
    }

    public void CreateBall(ScoreMaker.ScoreMakerBallType ballType)
    {
        var ballPrefab = ResourceManager.LoadPrefab("ScoreMakerBall");
        var ball = Instantiate(ballPrefab, _ballTransform);
        InstalledBall = ball.GetComponent<ScoreMakerBall>();
        InstalledBall.Init(ballType, Number, IsLeft);
    }
    public void SetBall(ScoreMakerBall ball)
    {
        ball.transform.SetParent(_ballTransform);
        ball.transform.localPosition = Vector3.zero;
    }

    public ScoreMakerBall Clicked(ScoreMaker.ScoreMakerBallType ballType)
    {
        // ボールがあるところならタイプにかかわらず消す
        if (InstalledBall != null)
        {
            Destroy(InstalledBall.gameObject);
            InstalledBall = null;
        }
        else if (ballType != ScoreMaker.ScoreMakerBallType.None)
        {
            CreateBall(ballType);
        }

        return InstalledBall;
    }
    public void DropedBall(ScoreMakerBall ball)
    {
        if (InstalledBall == null)
        {
            InstalledBall = ball;
            SetBall(ball);
        }
        OnWhenDropedBall.OnNext(ball);
    }
}
