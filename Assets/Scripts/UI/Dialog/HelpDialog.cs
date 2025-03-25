using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEditor;
using UnityEngine.UI;

public class HelpDialogOption : DialogOptionBase
{
    public string HelpTitleText;
    public List<string> StripIdList = new();
    public HelpDialogPageType FirstPageType;
}

public class HelpDialogResult : DialogResultBase
{
    public string SelectedStageId;
}

public enum HelpDialogPageType
{
    Home,
    Home2,
    Battle,
    PracticeMode,
    ScoreMaker,
    ScoreMaker2,
    ScoreMaker3,
}

public static class HelpDialogPageTypeExtension
{
    public static string GetTitle(this HelpDialogPageType pageType)
    {
        return pageType switch
        {
            HelpDialogPageType.Home => "ホーム",
            HelpDialogPageType.Home2 => "ホーム2",
            HelpDialogPageType.Battle => "バトル",
            HelpDialogPageType.PracticeMode => "練習モード",
            HelpDialogPageType.ScoreMaker => "ステージ作成",
            HelpDialogPageType.ScoreMaker2 => "ステージ作成2",
            HelpDialogPageType.ScoreMaker3 => "ステージ作成3",
            _ => "",
        };
    }

    public static List<string> GetStripIdList(this HelpDialogPageType pageType)
    {
        return pageType switch
        {
            HelpDialogPageType.Home => new List<string>(){
                "StageSelect"
            },
            HelpDialogPageType.Home2 => new List<string>(){
                "CustomStage"
            },
            HelpDialogPageType.Battle => new List<string>(){
                "Battle"
            },
            HelpDialogPageType.PracticeMode => new List<string>(){
                "Practice"
            },
            HelpDialogPageType.ScoreMaker => new List<string>(){
                "ScoreMaker1"
            },
            HelpDialogPageType.ScoreMaker2 => new List<string>(){
                "ScoreMaker2"
            },
            HelpDialogPageType.ScoreMaker3 => new List<string>(){
                "ScoreMaker3"
            },
            _ => new List<string>(),
        };
    }
}

public class HelpDialog : DialogBase
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Transform _stripTransform;

    private HelpDialogPageType _currentPageType;

    public override void Init(DialogOptionBase dialogOption)
    {
        base.Init(dialogOption);
        if (dialogOption is not HelpDialogOption helpDialogOption) return;
        SetPage(helpDialogOption.FirstPageType);

    }

    // ページごとの短冊を生成する
    public void SetPage(HelpDialogPageType pageType)
    {
        _currentPageType = pageType;
        _stripTransform.DestroyAllChildren();
        _titleText.text = pageType.GetTitle();
        foreach (string stripId in pageType.GetStripIdList())
        {
            var prefab = ResourceManager.LoadPrefab<HelpStrip>("UI/HelpStrip");
            var strip = Instantiate(prefab, _stripTransform);
            strip.Init(stripId);
        }
    }

    public override void ClosePanel(DialogResultType resultType)
    {
        switch (resultType)
        {
            case DialogResultType.Ok:
                // 次ページに進む
                if (_currentPageType < HelpDialogPageType.ScoreMaker3)
                {
                    SetPage(_currentPageType + 1);
                }
                break;
            case DialogResultType.Cancel:
                // 前ページに戻る
                if (_currentPageType > HelpDialogPageType.Home)
                {
                    SetPage(_currentPageType - 1);
                }
                break;
            case DialogResultType.None:
                // Noneのときだけ普通にダイアログを閉じる
                base.ClosePanel(resultType);
                break;
        }
    }

    void Update()
    {
        // 端のページの場合ページ切り替えボタンを押せなくする
        DialogCommonParts.OkButton.interactable = _currentPageType != HelpDialogPageType.ScoreMaker3;
        DialogCommonParts.CancelButton.interactable = _currentPageType != HelpDialogPageType.Home;
    }
}
