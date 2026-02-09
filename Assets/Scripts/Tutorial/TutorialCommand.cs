using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TutorialCommandList")]
public class TutorialCommandList : ScriptableObject
{
    public string TutorialName;
    public TutorialType TutorialType;
    public ScoreMakerStartParam ScoreMakerStartParam;
    public List<TutorialCommandParam> Commands;
}

public enum TutorialType
{
    Home,
    Practice,
    ScoreMaker,
}

[System.Serializable]
public class ScoreMakerStartParam
{
    public float ScrollPositionY;
    public List<BallArrangementPartam> BallArrangementList = new();
    public List<TimeJumpParam> TimeJumpList = new();
}

[System.Serializable]
public class BallArrangementPartam
{
    public int Number;
    public bool IsLeft;
}

[System.Serializable]
public class TimeJumpParam
{
    public int Index;
    public float TimeRate;
}

public enum TutorialAdvanceType
{
    TapMessage,
    PushButton,
    AdvanceId,
}

[System.Serializable]
public class TutorialCommandParam
{
    public TutorialAdvanceType AdvanceType;
    [SerializeReference]
    public TutorialTarget MaskTarget;
    [SerializeReference]
    public TutorialTarget ArrowTarget;
    public TargetArrow.ArrowVector TargetArrowVector;
    [SerializeReference]
    public TutorialTarget TouchableTarget;
    public bool IsEmphasis;
    public string AdvanceId;
    [TextArea(3, 10)]
    public string Message;
    public float MessagePositionY;

}

public enum TargetType
{
    None,
    Original,
    Inherit,
}

[System.Serializable]
public class TutorialTarget
{
    public string TargetId;
    [SerializeReference]
    public OverrideTargetParam OverrideParam;
}

[System.Serializable]
public class ArrowParam
{
    public TargetArrow.ArrowVector TargetArrowVector;
    [SerializeReference]
    public TutorialTarget OverrideTarget;
    [SerializeReference]
    public TutorialTarget TouchableOverride;
}

[System.Serializable]
public class OverrideTargetParam
{
    public float Width;
    public float Height;
    public Vector2 PositionOffset;
}