using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMakerControlPanel : MonoBehaviour
{
    [SerializeField] private List<ControlPanelPageBase> _menuPageList;
    [SerializeField] private ControlPanelPageMask _maskParts;
    [SerializeField] private ControlPanelPageBall _ballParts;
    [SerializeField] private ControlPanelPageCopyPaste _copyPasteParts;
    [SerializeField] private ControlPanelTimeJumpParts _timeJumpParts;
    [SerializeField] private Button _undoButton;

    // 下部メニュー
    [SerializeField] private GameObject _menuObject;
    [SerializeField] private TabGroup _tabGroup;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _openButton;

    // 演奏作成モード中のUI
    [SerializeField] private GameObject _playMakeModeUI;
    [SerializeField] private Button _finishPlayMakeModeButton;
    [SerializeField] private Button _resetPlayMakeModeButton;

    public bool IsWaitingPaste { get; private set; }
    public bool IsPasteMode => _copyPasteParts.IsPasteMode;
    private bool _isUnderMenuOpen;

    private Subject<ControlPanelRequestBase> _onRequest = new();
    public Observable<ControlPanelRequestBase> OnRequest => _onRequest;

    public void Init(MusicParameter musicParameter)
    {
        // パーツ系
        InitializeParts(_ballParts);
        InitializeParts(_maskParts);
        InitializeParts(_copyPasteParts);
        InitializeParts(_timeJumpParts);

        // 下部メニュー
        _tabGroup.Init();
        _tabGroup.OnTabSelected.Subscribe(index =>
        {
            SetPage(index);
        }).AddTo(this);
        SetPage(0);
        for (int i = 0; i < _menuPageList.Count; i++)
        {
            var page = _menuPageList[i];
            _tabGroup.SetTabText(i, page.PageName);
            InitializeParts(page, musicParameter);
        }

        // ボタン系
        _undoButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestUndo());
        }).AddTo(this);

        _finishPlayMakeModeButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestFinishPlayMake());
        }).AddTo(this);
        _resetPlayMakeModeButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestResetPlayMake());
        }).AddTo(this);

        _openButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeUnderMenuOpen(isOpen: true);
        }).AddTo(this);
        _closeButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangeUnderMenuOpen(isOpen: false);
        }).AddTo(this);

        // その他初期化
        SetModePanel(isPlayMakeMode: false);
        ChangeUnderMenuOpen(isOpen: true);
    }

    private void InitializeParts(ControlPanelPageBase parts, MusicParameter musicParameter = null)
    {
        parts.Init();
        parts.SetMusicParameter(musicParameter);
        parts.OnRequest.Subscribe(request => _onRequest.OnNext(request)).AddTo(this);
    }

    private void SetPage(int pageIndex)
    {
        for (int i = 0; i < _menuPageList.Count; i++)
        {
            _menuPageList[i].gameObject.SetActive(i == pageIndex);
        }
    }

    public void FinishPaste()
    {
        _copyPasteParts.FinishPaste();
        IsWaitingPaste = false;
    }

    public void SetButtonState(bool isSelectedLine, bool isCopiedLine, bool isExsistPastLine)
    {
        foreach (var page in _menuPageList)
        {
            page.SetButtonState(isSelectedLine, isCopiedLine);
        }
        _copyPasteParts.SetButtonState(isSelectedLine, isCopiedLine);
        _ballParts.SetButtonState(isSelectedLine, isCopiedLine);
        _undoButton.interactable = isExsistPastLine;
    }

    public void SetMusicParameter(MusicParameter musicParameter)
    {
        foreach (var page in _menuPageList)
        {
            page.SetMusicParameter(musicParameter);
        }
    }

    // 演奏作成モードかどうかでパネルを切り替え
    public void SetModePanel(bool isPlayMakeMode)
    {
        _menuObject.SetActive(!isPlayMakeMode && _isUnderMenuOpen);
        _copyPasteParts.gameObject.SetActive(!isPlayMakeMode);
        _undoButton.gameObject.SetActive(!isPlayMakeMode);
        _playMakeModeUI.SetActive(isPlayMakeMode);
        _timeJumpParts.gameObject.SetActive(!isPlayMakeMode && !_isUnderMenuOpen);
    }

    private void ChangeUnderMenuOpen(bool isOpen)
    {
        _isUnderMenuOpen = isOpen;
        _openButton.gameObject.SetActive(!isOpen);
        _timeJumpParts.gameObject.SetActive(!isOpen);
        _menuObject.SetActive(isOpen);
    }

    public void SetJumpTimeRate(int index, float timeRate)
    {
        _timeJumpParts.SetTimeRate(index, timeRate);
    }

    private void OnDestroy()
    {
        _onRequest.Dispose();
        _onRequest = null;
    }
}
