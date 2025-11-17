using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Linq;

public class StageDuplicateDialogOption : DialogOptionBase
{
    public SingleStageMaster StageInfo;
}

public class StageDuplicateDialogResult : DialogResultBase
{
    public List<NoteMaster> DuplicateNotes = new();
}

public class StageDuplicateDialog : DialogBase
{
    [SerializeField] private Transform _normalStripTransform;
    [SerializeField] private Transform _customStripTransform;
    [SerializeField] private GameObject _customStageTitle;

    private NewCreateStrip _selectedStrip;
    private SingleStageMaster _stageMaster;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        var duplicateOption = dialogOption as StageDuplicateDialogOption;
        if (dialogOption is not StageDuplicateDialogOption option) return;
        _stageMaster = option.StageInfo;
        var stageHeader = _stageMaster.StageHeader;
        _customStageTitle.SetActive(false);
        var allMasterList = MasterManager.CustomStageList;
        foreach (var master in allMasterList)
        {
            var prefab = ResourceManager.LoadPrefab<NewCreateStrip>("NewCreateStrip");
            var targetTransform = _customStripTransform;
            var strip = Instantiate(prefab, targetTransform);
            // レベル3までは名前にレベルを付ける
            strip.Init(stageHeader, 0, false);
            strip.OnClickedStrip.Subscribe(_ =>
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                strip.SetSelected(true);
                SEManager.instance.PlaySe(SeName.Button1);
            }).AddTo(this);
        }
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        if (resultType != DialogResultType.Ok)
        {
            SEManager.instance.PlaySe(SeName.Cancel);
        }
        int level = _selectedStrip?.StageId ?? 0;
        _onCloseDialog.OnNext(new StageDuplicateDialogResult
        {
            ResultType = resultType,
            DuplicateNotes = _stageMaster.Notes
        });
        Destroy(gameObject);
    }
}
