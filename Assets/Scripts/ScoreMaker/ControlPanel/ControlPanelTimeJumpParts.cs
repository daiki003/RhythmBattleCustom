using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ControlPanelTimeJumpParts : ControlPanelPageBase
{
    [SerializeField] private List<TimeJumpButton> _jumpButtonList;

    public override void Init()
    {
        for (int i = 0; i < _jumpButtonList.Count; i++)
        {
            _jumpButtonList[i].Init(i);
            _jumpButtonList[i].OnClickMainButton.Subscribe(timeRate =>
            {
                _onRequest.OnNext(new ControlPanelRequestTimeJump(timeRate));
            }).AddTo(this);
            _jumpButtonList[i].OnClickRegisterButton.Subscribe(index =>
            {
                _onRequest.OnNext(new ControlPanelRequestRegisterTimeJump(index));
            }).AddTo(this);
            _jumpButtonList[i].OnClickDeleteButton.Subscribe(index =>
            {
                _onRequest.OnNext(new ControlPanelRequestDeleteTimeJump(index));
            }).AddTo(this);
        }
    }

    public void SetTimeRate(int index, float timeRate)
    {
        _jumpButtonList[index].SetTimeRate(timeRate);
    }
}
