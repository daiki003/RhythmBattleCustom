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
        foreach (var stage in MasterManager.SampleStageList)
        {
            var prefab = ResourceManager.LoadPrefab<NewCreateStrip>("NewCreateStrip");
            var strip = Instantiate(prefab, _stripTransform);
            strip.Init(stage.StageHeader);
            strip.OnClickedStrip.Subscribe(async _ =>
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                strip.SetSelected(true);
                await BGMManager.instance.SetStageClip(strip.MusicId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
            }).AddTo(this);
        }
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        _onCloseDialog.OnNext(new NewCreateDialogResult
        {
            ResultType = resultType,
            SelectedStageId = _selectedStrip?.MusicId ?? ""
        });
        if (resultType != DialogResultType.Ok)
        {
            SEManager.instance.PlaySe(SeName.Cancel);
            BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        }
        Destroy(gameObject);
    }
}
