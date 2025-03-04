using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class ScoreLine : MonoBehaviour
{
    [SerializeField] private LinePocket _leftPocket;
    [SerializeField] private LinePocket _rightPocket;
    [SerializeField] private Image _moveButton;
    [SerializeField] private Text _numberText;
    public LinePocket LeftPocket => _leftPocket;
    public LinePocket RightPocket => _rightPocket;

    public int LineNumber { get; private set; }
    public bool IsEnd;
    public bool HasBall => _leftPocket.InstalledBall != null || _rightPocket.InstalledBall != null;

    public void Init(int number)
    {
        LineNumber = number;
        _numberText.gameObject.SetActive(number % 4 == 0);
        _numberText.text = (number / 4).ToString();
        _leftPocket.SetParam(LineNumber, isLeft: true);
        _rightPocket.SetParam(LineNumber, isLeft: false);
        _leftPocket.OnWhenDropedBall.Subscribe(ball =>
        {

        });
        _rightPocket.OnWhenDropedBall.Subscribe(ball =>
        {

        });
    }

    public void Beat()
    {
        if (_leftPocket.InstalledBall != null || _rightPocket.InstalledBall != null)
        {
            SEManager.instance.PlayBeatSe();
        }
    }

    public ScoreMakerBall CreateBall(ScoreMakerView.ScoreMakerBallType ballType, bool isLeft)
    {
        var targetPocket = isLeft ? _leftPocket : _rightPocket;
        return targetPocket.Clicked(ballType);
    }

    public void ClearLine()
    {
        _leftPocket.Clicked(ScoreMakerView.ScoreMakerBallType.None);
        _rightPocket.Clicked(ScoreMakerView.ScoreMakerBallType.None);
    }

    public ScoreMakerBall GetBall(bool isLeft)
    {
        var targetPocket = isLeft ? _leftPocket : _rightPocket;
        return targetPocket.InstalledBall;
    }

    public float GetLineTime(float bpm, float offset)
    {
        return LineNumber * (60f / bpm) + offset;
    }

    public NoteMaster GetMaster(bool isLeft)
    {
        var ball = GetBall(isLeft);
        return ball != null ? ball.CreateMaster() : null;
    }

    public void SetMoveButton(bool isActive)
    {
        _moveButton.gameObject.SetActive(isActive);
    }

    void OnDestroy()
    {
        Destroy(_leftPocket.gameObject);
        Destroy(_rightPocket.gameObject);
    }
}
