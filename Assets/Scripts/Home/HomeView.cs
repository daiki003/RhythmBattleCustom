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
    public bool IsMyMusic;
    public GetStageKey(string musicId, int stageId, bool isMyMusic)
    {
        MusicId = musicId;
        StageId = stageId;
        IsMyMusic = isMyMusic;
    }
}

public class HomeView : MonoBehaviour
{
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private Transform _customStripTransform;
    [SerializeField] private Text _lifeText;

    [SerializeField] private HomeViewInput _homeViewInput;

    private List<StageStrip> _stageStripList = new List<StageStrip>();

    private Subject<(GetStageKey stageKey, bool isPractice)> _clickPlayStageButton = new();
    public Observable<(GetStageKey stageKey, bool isPractice)> ClickPlayStageButton => _clickPlayStageButton;
    private Subject<(GetStageKey stageKey, bool isCopy)> _clickEditStageButton = new();
    public Observable<(GetStageKey stageKey, bool isCopy)> ClickEditStageButton => _clickEditStageButton;
    private Subject<string> _clickNewCreateStageButton = new();
    public Observable<string> ClickNewCreateStageButton => _clickNewCreateStageButton;

    private StageStrip _selectedStrip;
    private GetStageKey _currentStageKey;
    private HomePanelType _currentPanelType;

    public void Init(List<SingleStageMaster> stageList, HomePanelType firstPanelType)
    {
        CreateStripList(stageList);
        BGMManager.instance.SetClip(BgmName.WanderersCity, isLoop: true, isFade: true);
        UpdateLife();

        _homeViewInput.OnClickButton.Subscribe(async args =>
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
                    string title = _currentPanelType.IsSample() ? "作成" : "編集";
                    // ライフ消費確認
                    // var isConsumed = await DialogManager.instance.ConfirmConsumeLifeDialogAsync(
                    //     title: $"ステージ{title}",
                    //     message: $"{title}にはライフを1つ消費します。\n{title}しますか？",
                    //     buttonText: $"{title}する",
                    //     shortageText: $"ステージを{title}しますか？"
                    // );
                    // if (!isConsumed) return;

                    // 広告再生
                    await AdsManager.ShowInterstitialAsync();
                    _clickEditStageButton.OnNext((_currentStageKey, _currentPanelType.IsSample()));
                    break;
                case DeleteStageButtonArgs deleteStageArgs:
                    DeleteStageAsync().Forget();
                    break;
                case NewCreateButtonArgs newCreateArgs:
                    CreateStage().Forget();
                    break;
                case SettingButtonArgs settingArgs:
                    DialogManager.instance.OpenSettingDialog();
                    break;
                case HelpButtonArgs helpArgs:
                    var commandList = TutorialManager.Instance.GetTutorialCommandLists(TutorialType.Home);
                    var dialogResult = await DialogManager.instance.OpenTutorialDialogAsync(commandList);
                    switch (dialogResult.ResultType)
                    {
                        case DialogResultType.Ok:
                            CancelSelectStrip();
                            await TutorialManager.Instance.StartTutorialAsync(dialogResult.SelectedCommand);
                            break;
                    }
                    break;
                case PurchaseLifeButtonArgs purchaseLifeArgs:
                    await DialogManager.instance.OpenPurchaseLifeDialogAsync(
                        title: "ライフ獲得",
                        message: "広告を視聴してライフを1つ獲得しますか？",
                        isShortage: false
                    );
                    UpdateLife();
                    break;
            }
        }).AddTo(this);
        _homeViewInput.Init(firstPanelType);
    }

    private void UpdateLife()
    {
        _lifeText.text = SaveDataManager.LifeText;
        _homeViewInput.SetPurchaseLifeButtonActive(!SaveDataManager.IsInfiniteLife);
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
                _currentStageKey = new GetStageKey(strip.MusicId, strip.StageId, strip.IsMyMusic);
                strip.SetSelected(true);
                await BGMManager.instance.SetStageClip(strip.MusicId, isFade: true, startTime: strip.StartTime, endTime: strip.EndTime);
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

    private async UniTask DeleteStageAsync()
    {
        // 確認ダイアログ
        var dialogResult = await DialogManager.instance.ShowDialogAsync<MessageDialog, DialogResultBase>(
            new MessageDialogOption
            {
                TitleText = "ステージ削除",
                MessageText = "本当に削除しますか？",
                OkButtonText = "削除",
                CancelButtonText = "キャンセル",
                IsBgCancel = false,
            }
        );
        if (dialogResult.ResultType == DialogResultType.Ok)
        {
            await MasterManager.DeleteCustomStage(_selectedStrip.MusicId, _selectedStrip.StageId);
            // 短冊の選択をキャンセルしてからを削除
            var selectedStrip = _selectedStrip;
            CancelSelectStrip();
            DestroyStrip(selectedStrip);
            // 削除通知ダイアログ
            DialogManager.instance.ShowDialogAsync<MessageDialog, DialogResultBase>(
                new MessageDialogOption
                {
                    TitleText = "ステージ削除",
                    MessageText = "削除しました",
                    OkButtonText = "OK",
                    HideCancelButton = true
                }
            ).Forget();
        }
    }

    private async UniTask CreateStage()
    {
        CancelSelectStrip();
        // 曲選択
        string musicId = await MediaController.instance.MusicExpote();
        if (string.IsNullOrEmpty(musicId))
        {
            // 曲選択がキャンセルされた場合は何もしない
            return;
        }

        // ライフ消費確認
        // var isConsumed = await DialogManager.instance.ConfirmConsumeLifeDialogAsync(
        //      title: "ステージ作成",
        //      message: "ステージ作成にはライフを1つ消費します。\n作成しますか？",
        //      buttonText: "作成する",
        //     shortageText: "ステージを作成しますか？"
        // );
        // if (!isConsumed) return;

        // 広告再生
        await AdsManager.ShowInterstitialAsync();

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
