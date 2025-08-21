using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlPanelRequestBase
{
    
}

// ボール選択ページ
public class ControlPanelRequestSelectBall : ControlPanelRequestBase
{
    public SelectBallType BallType { get; private set; }

    public ControlPanelRequestSelectBall(SelectBallType ballType)
    {
        BallType = ballType;
    }
}
public class ControlPanelRequestBallRotation : ControlPanelRequestBase
{
    public ControlPanelRequestBallRotation() {}
}
public class ControlPanelRequestPractice : ControlPanelRequestBase
{
    public ControlPanelRequestPractice() {}
}

// 編集ページ
public class ControlPanelRequestCopy : ControlPanelRequestBase
{
    public ControlPanelRequestCopy() {}
}
public class ControlPanelRequestStartPaste : ControlPanelRequestBase
{
    public ControlPanelRequestStartPaste() {}
}
public class ControlPanelRequestSelectCancel : ControlPanelRequestBase
{
    public ControlPanelRequestSelectCancel() {}
}
public class ControlPanelRequestInversion : ControlPanelRequestBase
{
    public ControlPanelRequestInversion() {}
}
public class ControlPanelRequestDeleteRange : ControlPanelRequestBase
{
    public ControlPanelRequestDeleteRange() {}
}
public class ControlPanelRequestStageDuplicate : ControlPanelRequestBase
{
    public ControlPanelRequestStageDuplicate() {}
}

// 自動生成ページ
public class ControlPanelRequestEvenlySpaced : ControlPanelRequestBase
{
    public ControlPanelRequestEvenlySpaced() {}
}
public class ControlPanelRequestAutoCreate : ControlPanelRequestBase
{
    public ControlPanelRequestAutoCreate() {}
}
public class ControlPanelRequestPlayMake : ControlPanelRequestBase
{
    public ControlPanelRequestPlayMake() {}
}
public class ControlPanelRequestAllClear : ControlPanelRequestBase
{
    public ControlPanelRequestAllClear() {}
}

// BPMページ
public class ControlPanelRequestChangeBpm : ControlPanelRequestBase
{
    public float Bpm { get; private set; }
    public ControlPanelRequestChangeBpm(float bpm)
    {
        Bpm = bpm;
    }
}
public class ControlPanelRequestChangeStartTime : ControlPanelRequestBase
{
    public float StartTime { get; private set; }
    public ControlPanelRequestChangeStartTime(float startTime)
    {
        StartTime = startTime;
    }
}
public class ControlPanelRequestChangeEndTime : ControlPanelRequestBase
{
    public float EndTime { get; private set; }
    public ControlPanelRequestChangeEndTime(float endTime)
    {
        EndTime = endTime;
    }
}
public class ControlPanelRequestStartEstimate : ControlPanelRequestBase
{
    public ControlPanelRequestStartEstimate() {}
}
public class ControlPanelRequestFinishEstimate : ControlPanelRequestBase
{
    public ControlPanelRequestFinishEstimate() {}
}

// 拍子変更ページ
public class ControlPanelRequestBeatsNumber : ControlPanelRequestBase
{
    public int BeatsNumber { get; private set; }
    public ControlPanelRequestBeatsNumber(int beatsNumber)
    {
        BeatsNumber = beatsNumber;
    }
}
public class ControlPanelRequestModulation : ControlPanelRequestBase
{
    public int Measure { get; private set; }
    public int Beat { get; private set; }
    public ControlPanelRequestModulation(int measure, int beat)
    {
        Measure = measure;
        Beat = beat;
    }
}
public class ControlPanelRequestResetModulation : ControlPanelRequestBase
{
    public ControlPanelRequestResetModulation() {}
}
