using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Linq;

public class StageDuplicateDialogOption : DialogOptionBase
{
    public StageInfo StageInfo;
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
    private StageInfo _stageInfo;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        var duplicateOption = dialogOption as StageDuplicateDialogOption;
        if (dialogOption is not StageDuplicateDialogOption option) return;
        _stageInfo = option.StageInfo;
        var stageHeader = _stageInfo.StageHeader;
        _customStageTitle.SetActive(_stageInfo.HasCustomStage());
        foreach (var levelMaster in _stageInfo.LevelList)
        {
            var prefab = ResourceManager.LoadPrefab<NewCreateStrip>("NewCreateStrip");
            bool isNormalStage = levelMaster.Level <= MasterManager.MaxDefaultLevelId;
            var targetTransform = isNormalStage ? _normalStripTransform : _customStripTransform;
            var strip = Instantiate(prefab, targetTransform);
            // レベル3までは名前にレベルを付ける
            strip.Init(stageHeader, levelMaster.Level, isNormalStage);
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
        int level = _selectedStrip?.Level ?? 0;
        _onCloseDialog.OnNext(new StageDuplicateDialogResult
        {
            ResultType = resultType,
            DuplicateNotes = _stageInfo.LevelList.FirstOrDefault(l => l.Level == level)?.Notes
        });
        Destroy(gameObject);
    }
}
