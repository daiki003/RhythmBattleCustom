using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public enum HomePanelType
{
    Custom = 0,
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
}

public static class HomePanelTypeExtension
{
    public static bool IsSample(this HomePanelType panelType)
    {
        return panelType switch
        {
            HomePanelType.Level1 or
            HomePanelType.Level2 or
            HomePanelType.Level3 => true,
            HomePanelType.Custom => false,
            _ => false
        };
    }
}

public class GetStageKey
{
    public string MusicId;
    public int StageId;
    public HomePanelType PanelType;
    public bool IsMyMusic;
    public GetStageKey(string musicId, int stageId, HomePanelType panelType, bool isMyMusic)
    {
        MusicId = musicId;
        StageId = stageId;
        PanelType = panelType;
        IsMyMusic = isMyMusic;
    }
}

public class HomeView : MonoBehaviour
{
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private Transform _customStripTransform;

    [SerializeField] private HomeViewInput _homeViewInput;

    private List<StageStrip> _stageStripList = new List<StageStrip>();

    private Subject<(GetStageKey stageKey, bool isPractice)> _clickPlayStageButton = new();
    public Observable<(GetStageKey stageKey, bool isPractice)> ClickPlayStageButton => _clickPlayStageButton;
    private Subject<(GetStageKey stageKey, bool isNewCreate)> _clickEditStageButton = new();
    public Observable<(GetStageKey stageKey, bool isNewCreate)> ClickEditStageButton => _clickEditStageButton;
    private Subject<string> _clickNewCreateStageButton = new();
    public Observable<string> ClickNewCreateStageButton => _clickNewCreateStageButton;

    private StageStrip _selectedStrip;
    private GetStageKey _currentStageKey;
    private HomePanelType _currentPanelType;

    public void Init(List<SingleStageMaster> stageList, HomePanelType firstPanelType)
    {
        CreateStripList(stageList);
        BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);

        _homeViewInput.OnClickButton.Subscribe(args =>
        {
            switch (args)
            {
                case MenuButtonArgs menuArgs:
                    ChangePanelType(menuArgs.PanelType);
                    break;
                case PlayStageButtonArgs playStageArgs:
                    _clickPlayStageButton.OnNext((_currentStageKey, playStageArgs.IsPracticeMode));
                    break;
                case EditStageButtonArgs editStageArgs:
                    _clickEditStageButton.OnNext((_currentStageKey, _currentPanelType.IsSample()));
                    break;
                case DeleteStageButtonArgs deleteStageArgs:
                    DeleteStage();
                    break;
                case NewCreateButtonArgs newCreateArgs:
                    CreateStage().Forget();
                    break;
                case SettingButtonArgs settingArgs:
                    DialogManager.instance.OpenSettingDialog();
                    break;
                case HelpButtonArgs helpArgs:
                    var commandList =  TutorialManager.Instance.GetTutorialCommandLists(TutorialType.Home);
                    var dialog = DialogManager.instance.OpenTutorialDialog(commandList);
                    dialog.OnCloseDialog.Subscribe(async result =>
                    {
                        if (result is not TutorialDialogResult tutorialResult)
                        {
                            return;
                        }
                        switch (result.ResultType)
                        {
                            case DialogResultType.Ok:
                                CancelSelectStrip();
                                await TutorialManager.Instance.StartTutorialAsync(tutorialResult.SelectedCommand);
                                break;
                        }
                    }).AddTo(dialog);
                    break;
            }
        }).AddTo(this);
        _homeViewInput.Init(firstPanelType);
    }

    // ステージの短冊を全て作成
    public void CreateStripList(List<SingleStageMaster> stageList)
    {
        DestroyAllStrip();
        bool isAddTutorialStrip = false;
        foreach (var stageInfo in stageList)
        {
            var strip = CreateStageStrip(stageInfo.StageHeader, stageInfo.StageId, _customStripTransform);
            // チュートリアル用に最初の短冊を登録しておく
            if (!isAddTutorialStrip && (HomePanelType)stageInfo.StageHeader.PanelType == HomePanelType.Level1)
            {
                isAddTutorialStrip = true;
                TutorialManager.Instance.AddTargetRect("FirstStageStrip", strip.transform as RectTransform);
            }
        }
    }

    // ステージの短冊1枚を作成
    private StageStrip CreateStageStrip(StageHeader stageHeader, int stageId, Transform transform)
    {
        var strip = Instantiate(_stageStripPrefab, transform);
        _stageStripList.Add(strip);
        strip.Init(stageHeader, stageId);
        strip.OnClickedStrip.Subscribe(async _ =>
        {
            if (_selectedStrip != strip)
            {
                _selectedStrip?.SetSelected(false);
                _selectedStrip = strip;
                _currentStageKey = new GetStageKey(strip.MusicIdId, strip.StageId, strip.PanelType, strip.IsMyMusic);
                strip.SetSelected(true);
                await BGMManager.instance.SetStageClip(strip.MusicIdId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
                _homeViewInput.SetButtonInteractable(true);
            }
            else
            {
                CancelSelectStrip();
            }
        }).AddTo(strip);
        strip.gameObject.SetActive(stageHeader.PanelType == (int)_currentPanelType);
        return strip;
    }

    private void CancelSelectStrip()
    {
        if (_selectedStrip != null)
        {
            _selectedStrip.SetSelected(false);
            _selectedStrip = null;
            BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
            _homeViewInput.SetButtonInteractable(false);
        }
    }

    private void DestroyAllStrip()
    {
        while (_stageStripList.Count > 0)
        {
            var strip = _stageStripList[0];
            DestroyStrip(strip);
        }
    }

    private void DestroyStrip(StageStrip strip)
    {
        _stageStripList.Remove(strip);
        Destroy(strip.gameObject);
    }

    private void DeleteStage()
    {
        // 確認ダイアログ
        var option = new MessageDialogOption
        {
            TitleText = "ステージ削除",
            MessageText = "本当に削除しますか？",
            OkButtonText = "削除",
            CancelButtonText = "キャンセル"
        };
        var dialog = DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
        dialog.OnCloseDialog.Subscribe(async result =>
        {
            if (result.ResultType == DialogResultType.Ok)
            {
                await MasterManager.DeleteCustomStage(_selectedStrip.MusicIdId, _selectedStrip.StageId);
                // 短冊の選択をキャンセルしてからを削除
                var selectedStrip = _selectedStrip;
                CancelSelectStrip();
                DestroyStrip(selectedStrip);
                // 削除通知ダイアログ
                var option = new MessageDialogOption
                {
                    TitleText = "ステージ削除",
                    MessageText = "削除しました",
                    OkButtonText = "OK",
                    HideCancelButton = true
                };
                var dialog = DialogManager.instance.CreateDialog<MessageDialog>(DialogManager.MessageDialogPrefabName, option);
            }
        }).AddTo(this);
    }

    private async UniTask CreateStage()
    {
        CancelSelectStrip();
        string musicId = await MediaController.instance.MusicExpote();
        if (string.IsNullOrEmpty(musicId))
        {
            // 曲選択がキャンセルされた場合は何もしない
            return;
        }
        _clickNewCreateStageButton.OnNext(musicId);
    }

    private void ChangePanelType(HomePanelType panelType)
    {
        if (_currentPanelType == panelType)
        {
            return;
        }
        CancelSelectStrip();
        _currentPanelType = panelType;
        bool isStage = panelType.IsSample();
        _homeViewInput.ChangeButtonByCustomMode(!isStage);
        // 短冊の表示切替
        foreach (var strip in _stageStripList)
        {
            strip.gameObject.SetActive(strip.PanelType == panelType);
        }
    }
}
