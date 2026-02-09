using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlPanelRequestBase
{
    
}

// ボール選択ページ
public class ControlPanelRequestSelectBall : ControlPanelRequestBase
{
    public OperationType OperationType { get; private set; }

    public ControlPanelRequestSelectBall(OperationType operationType)
    {
        OperationType = operationType;
    }
}

// マスク操作ページ
public class ControlPanelRequestUp : ControlPanelRequestBase
{
    public ControlPanelRequestUp() { }
}
public class ControlPanelRequestDown : ControlPanelRequestBase
{
    public ControlPanelRequestDown() { }
}
public class ControlPanelRequestCloseMask : ControlPanelRequestBase
{
    public ControlPanelRequestCloseMask() { }
}
public class ControlPanelRequestCopy : ControlPanelRequestBase
{
    public ControlPanelRequestCopy() { }
}
public class ControlPanelRequestStartPaste : ControlPanelRequestBase
{
    public bool IsStart { get; private set; }
    public ControlPanelRequestStartPaste(bool isStart) { IsStart = isStart; }
}
public class ControlPanelRequestDeleteRange : ControlPanelRequestBase
{
    public ControlPanelRequestDeleteRange() {}
}


// 編集ページ
public class ControlPanelRequestInversion : ControlPanelRequestBase
{
    public ControlPanelRequestInversion() {}
}

// 自動生成ページ
public class ControlPanelRequestEvenlySpaced : ControlPanelRequestBase
{
    public ControlPanelRequestEvenlySpaced() {}
}
public class ControlPanelRequestPlayMake : ControlPanelRequestBase
{
    public ControlPanelRequestPlayMake() { }
}
public class ControlPanelRequestPractice : ControlPanelRequestBase
{
    public ControlPanelRequestPractice() { }
}
public class ControlPanelRequestSave : ControlPanelRequestBase
{
    public ControlPanelRequestSave() { }
}
public class ControlPanelRequestNewSave : ControlPanelRequestBase
{
    public ControlPanelRequestNewSave() {}
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

// 拍子変更ページ
public class ControlPanelRequestBeatsNumber : ControlPanelRequestBase
{
    public int BeatsNumber { get; private set; }
    public ControlPanelRequestBeatsNumber(int beatsNumber)
    {
        BeatsNumber = beatsNumber;
    }
}
public class ControlPanelRequestModulationChange : ControlPanelRequestBase
{
    public int Measure { get; private set; }
    public bool IsForward { get; private set; }
    public ControlPanelRequestModulationChange(int measure, bool isForward)
    {
        Measure = measure;
        IsForward = isForward;
    }
}
public class ControlPanelRequestResetModulation : ControlPanelRequestBase
{
    public ControlPanelRequestResetModulation() { }
}

public class ControlPanelRequestUndo : ControlPanelRequestBase
{
    public ControlPanelRequestUndo() { }
}

// 演奏作成モード関連
public class ControlPanelRequestFinishPlayMake : ControlPanelRequestBase
{
    public ControlPanelRequestFinishPlayMake() { }
}
public class ControlPanelRequestResetPlayMake : ControlPanelRequestBase
{
    public ControlPanelRequestResetPlayMake() { }
}

// タイムジャンプ関連
public class ControlPanelRequestTimeJump : ControlPanelRequestBase
{
    public float TimeRate { get; private set; }
    public ControlPanelRequestTimeJump(float lineNumber)
    {
        TimeRate = lineNumber;
    }
}
public class ControlPanelRequestRegisterTimeJump : ControlPanelRequestBase
{
    public int Index { get; private set; }
    public ControlPanelRequestRegisterTimeJump(int index)
    {
        Index = index;
    }
}
public class ControlPanelRequestDeleteTimeJump : ControlPanelRequestBase
{
    public int Index { get; private set; }
    public ControlPanelRequestDeleteTimeJump(int index)
    {
        Index = index;
    }
}
