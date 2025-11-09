using System.Collections;
using System.Collections.Generic;
using R3;
using Unity.VisualScripting;
using UnityEngine;

public class LinePocket : MonoBehaviour
{
    [SerializeField] private Transform _ballTransform;
    [SerializeField] private GameObject _selectedPanel;

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
    public void RecreateBall(ScoreMakerView.ScoreMakerBallType ballType)
    {
        if (InstalledBall == null)
        {
            CreateBall(ballType);
            return;
        }
        InstalledBall.ChangeBallType(ballType);
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

    public void SelectBall(bool isSelected)
    {
        _selectedPanel.SetActive(isSelected);
    }

    public ScoreMakerBall Rotation()
    {
        // ボールがないならシングル
        var ballType = ScoreMakerView.ScoreMakerBallType.Single;
        if (InstalledBall != null)
        {
            if (InstalledBall.BallType == ScoreMakerView.ScoreMakerBallType.Single)
            {
                // シングルならロングに
                ballType = ScoreMakerView.ScoreMakerBallType.Long;
            }
            else if (InstalledBall.BallType == ScoreMakerView.ScoreMakerBallType.Long)
            {
                // ロングならNoneに
                ballType = ScoreMakerView.ScoreMakerBallType.None;
            }
            // 今あるボールは消す
            Destroy(InstalledBall.gameObject);
            InstalledBall = null;
            if (ballType == ScoreMakerView.ScoreMakerBallType.None)
            {
                // Noneなら消して終わり
                return null;
            }
        }
        CreateBall(ballType);
        return InstalledBall;
    }
}
