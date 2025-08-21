using System.Collections;
using System.Collections.Generic;
using R3;
using Unity.VisualScripting;
using UnityEngine;

public class LinePocket : MonoBehaviour
{
    [SerializeField] private Transform _ballTransform;

    public ScoreMakerBall InstalledBall;

    public int Number { get; private set; }
    public bool IsLeft { get; private set; }

    public void SetParam(int number, bool isLeft)
    {
        Number = number;
        IsLeft = isLeft;
    }

    public void CreateBall(ScoreMakerView.ScoreMakerBallType ballType)
    {
        var ballPrefab = ResourceManager.LoadPrefab<ScoreMakerBall>("ScoreMaker/ScoreMakerBall");
        var ball = Instantiate(ballPrefab, _ballTransform);
        InstalledBall = ball;
        InstalledBall.Init(ballType, Number, IsLeft);
    }
    public void SetBall(ScoreMakerBall ball)
    {
        ball.transform.SetParent(_ballTransform);
        ball.transform.localPosition = Vector3.zero;
    }

    public ScoreMakerBall Clicked(ScoreMakerView.ScoreMakerBallType ballType)
    {
        // ボールがあるところならタイプにかかわらず消す
        if (InstalledBall != null)
        {
            Destroy(InstalledBall.gameObject);
            InstalledBall = null;
        }
        else if (ballType != ScoreMakerView.ScoreMakerBallType.None)
        {
            CreateBall(ballType);
        }

        return InstalledBall;
    }

    public ScoreMakerBall Rotation()
    {
        var ballType = ScoreMakerView.ScoreMakerBallType.Single;
        if (InstalledBall != null)
        {
            if (InstalledBall.BallType == ScoreMakerView.ScoreMakerBallType.Single)
            {
                // シングルならロングに
                ballType = ScoreMakerView.ScoreMakerBallType.Long;
            }
            // 今あるボールは消す
            Destroy(InstalledBall.gameObject);
            InstalledBall = null;
            if (ballType == ScoreMakerView.ScoreMakerBallType.Long)
            {
                // ロングなら消して終わり
                return null;
            }
        }
        // ボールがないならシングル
        CreateBall(ballType);
        return InstalledBall;
    }
}
