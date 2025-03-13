using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMakerControlPanel : MonoBehaviour
{
    [SerializeField] private List<GameObject> _pageList;
    // 1ページ目
    [SerializeField] private CustomButton _selectSingleBallButton; // シングルボール選択ボタン
    [SerializeField] private CustomButton _selectLongBallButton; // ロングボール選択ボタン
    [SerializeField] private Button _undoButton; // 一手戻すボタン
    // 2ページ目
    [SerializeField] private Button _copyButton; // コピーボタン
    [SerializeField] private Button _pasteButton; // ペーストボタン
    [SerializeField] private Button _selectCancelButton; // 選択解除ボタン
    [SerializeField] private Button _inversionButton; // 左右反転ボタン
    [SerializeField] private Button _deleteRangeButton; // 範囲削除ボタン
    [SerializeField] private Button _stageDuplicateButton; // ステージ複製ボタン
    // 3ページ目
    [SerializeField] private Slider _lineSpacingSlider; // ライン間隔調整スライダー
    [SerializeField] private Slider _ballSizeSlider; // ボールサイズ調整スライダー
    // 全体
    [SerializeField] private Button _nextPageButton; // 次ページボタン
    [SerializeField] private Button _backPageButton; // 前ページボタン
    [SerializeField] private GameObject _messageMask;
    [SerializeField] private Text _messageText;
    [SerializeField] private Button _pasteCancelButton; // ペーストキャンセルボタン

    private int _currentPageIndex;
    public bool IsEditMode => _currentPageIndex == 1;
    public bool IsWaitingPaste { get; private set; }

    private bool _isSelectedLine;
    private bool _isCopiedLine;
    private bool _isExsistPastLine;

    private Subject<ScoreMakerView.ScoreMakerBallType> _onSelectBall = new();
    public Observable<ScoreMakerView.ScoreMakerBallType> OnSelectBall => _onSelectBall;
    private Subject<Unit> _onCopy = new();
    public Observable<Unit> OnCopy => _onCopy;
    private Subject<Unit> _onSelectCancel = new();
    public Observable<Unit> OnSelectCancel => _onSelectCancel;
    private Subject<Unit> _onInversion = new();
    public Observable<Unit> OnInversion => _onInversion;
    private Subject<Unit> _onUndo = new();
    public Observable<Unit> OnUndo => _onUndo;
    private Subject<Unit> _onDeleteRange = new();
    public Observable<Unit> OnDeleteRange => _onDeleteRange;
    private Subject<Unit> _onStageDuplicate = new();
    public Observable<Unit> OnStageDuplicate => _onStageDuplicate;
    private Subject<float> _onChangeLineSpacing = new();
    public Observable<float> OnChangeLineSpacing => _onChangeLineSpacing;

    public void Init()
    {
        SetPage(0);
        _messageMask.SetActive(false);

        _selectSingleBallButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onSelectBall.OnNext(ScoreMakerView.ScoreMakerBallType.Single);
            SetButtonHighLight(true);
        }).AddTo(this);
        _selectLongBallButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onSelectBall.OnNext(ScoreMakerView.ScoreMakerBallType.Long);
            SetButtonHighLight(false);
        }).AddTo(this);
        _undoButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onUndo.OnNext(default);
        }).AddTo(this);

        _copyButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onCopy.OnNext(Unit.Default);
        }).AddTo(this);
        _pasteButton.OnClickAsObservable().Subscribe(_ =>
        {
            _messageText.text = "貼り付け先の最初の列を選択してください";
            _messageMask.gameObject.SetActive(true);
            IsWaitingPaste = true;
        }).AddTo(this);
        _selectCancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onSelectCancel.OnNext(default);
        }).AddTo(this);
        _inversionButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onInversion.OnNext(default);
        }).AddTo(this);
        _deleteRangeButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onDeleteRange.OnNext(default);
        }).AddTo(this);
        _stageDuplicateButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onStageDuplicate.OnNext(default);
        });

        _pasteCancelButton.OnClickAsObservable().Subscribe(_ =>
        {
            FinishPaste();
        }).AddTo(this);

        _lineSpacingSlider.OnValueChangedAsObservable().Subscribe(value =>
        {
            _onChangeLineSpacing.OnNext(value);
        }).AddTo(this);
        // 初期値は中間にしておく
        _lineSpacingSlider.value = 0.5f;

        _nextPageButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePage(isNext: true);
        }).AddTo(this);
        _backPageButton.OnClickAsObservable().Subscribe(_ =>
        {
            ChangePage(isNext: false);
        }).AddTo(this);;
    }

    private void SetButtonHighLight(bool isSingle)
    {
        _selectSingleBallButton.SetHighLight(isSingle);
        _selectLongBallButton.SetHighLight(!isSingle);
    }

    private void ChangePage(bool isNext)
    {
        int nextPageIndex = _currentPageIndex + (isNext ? 1 : -1);
        _currentPageIndex = Mathf.Clamp(nextPageIndex, 0, _pageList.Count - 1);
        SetPage(_currentPageIndex);
    }

    private void SetPage(int pageIndex)
    {
        _onSelectCancel.OnNext(default);
        for (int i = 0; i < _pageList.Count; i++)
        {
            _pageList[i].SetActive(i == pageIndex);
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
        _isSelectedLine = isSelectedLine;
        _isCopiedLine = isCopiedLine;
        _isExsistPastLine = isExsistPastLine;
    }

    void Update()
    {
        _undoButton.interactable = _isExsistPastLine;

        _copyButton.interactable = _isSelectedLine;
        _inversionButton.interactable = _isSelectedLine;
        _selectCancelButton.interactable = _isSelectedLine;
        _deleteRangeButton.interactable = _isSelectedLine;
        _pasteButton.interactable = _isCopiedLine;
    }
}
