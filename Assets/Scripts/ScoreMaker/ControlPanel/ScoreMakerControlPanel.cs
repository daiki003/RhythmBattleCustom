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
    [SerializeField] private Button _undoButton;

    // 下部メニュー
    [SerializeField] private TabGroup _tabGroup;
    [SerializeField] private Button _nextPageButton; // 次ページボタン
    [SerializeField] private Button _backPageButton; // 前ページボタン
    [SerializeField] private GameObject _messageMask;
    [SerializeField] private Text _messageText;

    private int _currentPageIndex;
    public bool IsWaitingPaste { get; private set; }
    public bool IsPasteMode => _maskParts.IsPasteMode;

    private Subject<ControlPanelRequestBase> _onRequest = new();
    public Observable<ControlPanelRequestBase> OnRequest => _onRequest;

    public void Init(MusicParameter musicParameter)
    {
        SetPage(0);
        _messageMask.SetActive(false);

        _ballParts.Init();
        _ballParts.OnRequest.Subscribe(request =>
        {
            _onRequest.OnNext(request);
        }).AddTo(this);
        _ballParts.SetSelectBallType(OperationType.Hybrid);

        _maskParts.Init();
        _maskParts.OnRequest.Subscribe(request =>
        {
            _onRequest.OnNext(request);
        }).AddTo(this);

        _undoButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onRequest.OnNext(new ControlPanelRequestUndo());
        }).AddTo(this);

        _tabGroup.Init();
        _tabGroup.OnTabSelected.Subscribe(index =>
        {
            SetPage(index);
        }).AddTo(this);
        for (int i = 0; i < _menuPageList.Count; i++)
        {
            var page = _menuPageList[i];
            _tabGroup.SetTabText(i, page.PageName);
            page.Init();
            page.SetMusicParameter(musicParameter);
            page.OnRequest.Subscribe(request =>
            {
                switch (request)
                {
                    case ControlPanelRequestStartEstimate _:
                        SetMessageMask("計測中...");
                        break;
                    case ControlPanelRequestFinishEstimate _:
                        _messageMask.SetActive(false);
                        break;
                }
                _onRequest.OnNext(request);
            }).AddTo(this);
        }

        _nextPageButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePage(isNext: true);
        }).AddTo(this);
        _backPageButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePage(isNext: false);
        }).AddTo(this);
    }

    private void SetMessageMask(string message)
    {
        _messageMask.SetActive(true);
        _messageText.text = message;
    }

    private void ChangePage(bool isNext)
    {
        int nextPageIndex = _currentPageIndex + (isNext ? 1 : -1);
        _currentPageIndex = Mathf.Clamp(nextPageIndex, 0, _menuPageList.Count - 1);
        SetPage(_currentPageIndex);
    }

    private void SetPage(int pageIndex)
    {
        for (int i = 0; i < _menuPageList.Count; i++)
        {
            _menuPageList[i].gameObject.SetActive(i == pageIndex);
        }
        _backPageButton.gameObject.SetActive(pageIndex > 0);
        _nextPageButton.gameObject.SetActive(pageIndex < _menuPageList.Count - 1);
    }

    public void FinishPaste()
    {
        _maskParts.FinishPaste();
        _messageMask.gameObject.SetActive(false);
        IsWaitingPaste = false;
    }

    public void SetButtonState(bool isSelectedLine, bool isCopiedLine, bool isExsistPastLine)
    {
        foreach (var page in _menuPageList)
        {
            page.SetButtonState(isSelectedLine, isCopiedLine);
        }
        _maskParts.SetButtonState(isSelectedLine, isCopiedLine);
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
}
