using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class NewCreateDialogOption : DialogOptionBase
{

}

public class NewCreateDialogResult : DialogResultBase
{
    public string SelectedStageId;
}

public class NewCreateListDialog : DialogBase
{
    [SerializeField] private Transform _stripTransform;

    private NewCreateStrip _selectedStrip;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        foreach (var master in MasterManager.StageMasterList)
        {
            var prefab = ResourceManager.LoadPrefab<NewCreateStrip>("NewCreateStrip");
            var strip = Instantiate(prefab, _stripTransform);
            strip.Init(master.StageHeader);
            strip.OnClickedStrip.Subscribe(_ =>
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                strip.SetSelected(true);
                BGMManager.instance.SetClip(strip.StageId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
            }).AddTo(this);
        }
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        _onCloseDialog.OnNext(new NewCreateDialogResult
        {
            ResultType = resultType,
            SelectedStageId = _selectedStrip?.StageId ?? ""
        });
        if (resultType != DialogResultType.Ok)
        {
            BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        }
        Destroy(gameObject);
    }
}
