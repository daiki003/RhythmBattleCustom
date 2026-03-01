using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using R3;
using UnityEngine;

public class ScoreMakerBall : MonoBehaviour
{
    [SerializeField] private Image _ballImage;
    public ScoreMakerView.ScoreMakerBallType BallType;
    public int LineNumber;
    public bool IsLeft;

    // 以下LongBall限定
    public ScoreMakerBallLine AttachedLine { get; private set; }
    public ScoreMakerBall PairBall;
    public bool IsLongLast;

    public void Init(ScoreMakerView.ScoreMakerBallType ballType, int lineNumber, bool isLeft)
    {
        LineNumber = lineNumber;
        IsLeft = isLeft;
        ChangeBallType(ballType);
    }

    public void ChangeBallType(ScoreMakerView.ScoreMakerBallType ballType)
    {
        if (BallType == ballType) return;
        BallType = ballType;
        bool isSingle = ballType == ScoreMakerView.ScoreMakerBallType.Single;
        string ballSpritePath = isSingle ? "Ball/Single" : "Ball/Long";
        _ballImage.sprite = Resources.Load<Sprite>(ballSpritePath);
        _ballImage.color = isSingle ? Color.red : Color.blue;
    }

    public void AttachLine(ScoreMakerBallLine scoreMakerBallLine, ScoreMakerBall pairBall, bool isLast)
    {
        AttachedLine = scoreMakerBallLine;
        PairBall = pairBall;
        IsLongLast = isLast;
    }

    public void DestroyLine()
    {
        if (AttachedLine != null)
        {
            AttachedLine.DestroyLine();
        }
    }

    public void ResetPair(bool isForce = false)
    {
        if (isForce && AttachedLine != null)
        {
            AttachedLine.DestroyLine();
        }
        AttachedLine = null;
        PairBall = null;
        IsLongLast = false;
    }

    public NoteMaster CreateMaster(bool isCreatePair = false)
    {
        // 通常ロング終端のボールはスキップするが、ロングのペアとしては作る
        if (!IsLongLast || isCreatePair)
        {
            bool isLong = BallType ==  ScoreMakerView.ScoreMakerBallType.Long;
            var pairNotes = new List<NoteMaster>();
            if (isLong && !IsLongLast)
            {
                pairNotes.Add(PairBall.CreateMaster(isCreatePair: true));
            }
            var note = new NoteMaster()
            {
                num = LineNumber,
                block = IsLeft ? 1 : 5,
                type = isLong ? 2 : 1,
                notes = pairNotes,
            };
            return note;
        }
        return null;
    }

    void OnDestroy()
    {
        if (AttachedLine != null)
        {
            AttachedLine.DestroyLine();
        }
        if (PairBall != null)
        {
            PairBall.ResetPair();
        }
    }
}
