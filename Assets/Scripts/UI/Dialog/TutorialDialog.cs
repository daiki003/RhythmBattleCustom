using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;

public class TutorialDialogOption : DialogOptionBase
{
    public TutorialCommandList[] CommandList;
}

public class TutorialDialogResult : DialogResultBase
{
    public TutorialCommandList SelectedCommand;
}

public class TutorialDialog : DialogBase
{
    [SerializeField] private Transform _stripTransform;

    private TutorialCommandList _selectedCommand;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        if (dialogOption is not TutorialDialogOption helpDialogOption) return;
        foreach (var command in helpDialogOption.CommandList)
        {
            var prefab = ResourceManager.LoadPrefab<TutorialStrip>("UI/TutorialStrip");
            var strip = Instantiate(prefab, _stripTransform);
            strip.OnClickedStrip.Subscribe(_ =>
            {
                _selectedCommand = command;
                ClosePanel(DialogResultType.Ok);
            }).AddTo(strip);
            strip.Init(command.TutorialName);
        }
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        // Okの場合はボタン側で鳴らしたい
        if (resultType != DialogResultType.Ok)
        {
            SEManager.instance.PlaySe(SeName.Cancel);
        }
        _onCloseDialog.OnNext(new TutorialDialogResult
        {
            ResultType = resultType,
            SelectedCommand = _selectedCommand
        });
        Destroy(gameObject);
    }
}
