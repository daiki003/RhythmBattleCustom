using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMakerControlPanel : MonoBehaviour
{
    [SerializeField] private List<ControlPanelPageBase> _pageList;

    // 全体
    [SerializeField] private Button _nextPageButton; // 次ページボタン
    [SerializeField] private Button _backPageButton; // 前ページボタン
    [SerializeField] private GameObject _messageMask;
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _pasteCancelButton; // ペーストキャンセルボタン

    private int _currentPageIndex;
    public bool IsEditMode => _currentPageIndex == 1;
    public bool IsWaitingPaste { get; private set; }

    private Subject<ControlPanelRequestBase> _onRequest = new();
    public Observable<ControlPanelRequestBase> OnRequest => _onRequest;

    public void Init(MusicParameter musicParameter, AudioClip audioClip)
    {
        SetPage(0);
        _messageMask.SetActive(false);

        foreach (var page in _pageList)
        {
            page.Init();
            page.SetMusicParameter(musicParameter);
            page.OnRequest.Subscribe(request =>
            {
                switch (request)
                {
                    case ControlPanelRequestStartPaste _:
                        SetMessageMask("貼り付け先の最初の列を選択してください", isDisplayCancelButton: true);
                        IsWaitingPaste = true;
                        break;
                    case ControlPanelRequestStartEstimate _:
                        SetMessageMask("計測中...", isDisplayCancelButton: false);
                        break;
                    case ControlPanelRequestFinishEstimate _:
                        _messageMask.SetActive(false);
                        break;
                }
                _onRequest.OnNext(request);
            }).AddTo(this);
        }

        _pasteCancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            FinishPaste();
        }).AddTo(this);

        _nextPageButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePage(isNext: true);
        }).AddTo(this);
        _backPageButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePage(isNext: false);
        }).AddTo(this);
    }

    private void SetMessageMask(string message, bool isDisplayCancelButton)
    {
        _messageMask.SetActive(true);
        _messageText.text = message;
        _pasteCancelButton.gameObject.SetActive(isDisplayCancelButton);
    }

    private void ChangePage(bool isNext)
    {
        int nextPageIndex = _currentPageIndex + (isNext ? 1 : -1);
        _currentPageIndex = Mathf.Clamp(nextPageIndex, 0, _pageList.Count - 1);
        SetPage(_currentPageIndex);
    }

    private void SetPage(int pageIndex)
    {
        _onRequest.OnNext(new ControlPanelRequestSelectCancel());
        for (int i = 0; i < _pageList.Count; i++)
        {
            _pageList[i].gameObject.SetActive(i == pageIndex);
        }
        _backPageButton.gameObject.SetActive(pageIndex > 0);
        _nextPageButton.gameObject.SetActive(pageIndex < _pageList.Count - 1);
    }

    public void FinishPaste()
    {
        _messageMask.gameObject.SetActive(false);
        IsWaitingPaste = false;
    }

    public void SetButtonState(bool isSelectedLine, bool isCopiedLine, bool isExsistPastLine)
    {
        foreach (var page in _pageList)
        {
            page.SetButtonState(isSelectedLine, isCopiedLine, isExsistPastLine);
        }
    }

    public void SetMusicParameter(MusicParameter musicParameter)
    {
        foreach (var page in _pageList)
        {
            page.SetMusicParameter(musicParameter);
        }
    }
}
