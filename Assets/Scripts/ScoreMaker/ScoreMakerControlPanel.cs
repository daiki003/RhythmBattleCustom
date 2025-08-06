using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ScoreMakerControlPanel : MonoBehaviour
{
    [SerializeField] private List<GameObject> _pageList;
    // 1ページ目
    [SerializeField] private CustomButton _selectSingleBallButton; // シングルボール選択ボタン
    [SerializeField] private CustomButton _selectLongBallButton; // ロングボール選択ボタン
    [SerializeField] private Button _practiceButton; // 練習ボタン
    // 2ページ目
    [SerializeField] private Button _copyButton; // コピーボタン
    [SerializeField] private Button _pasteButton; // ペーストボタン
    [SerializeField] private Button _selectCancelButton; // 選択解除ボタン
    [SerializeField] private Button _inversionButton; // 左右反転ボタン
    [SerializeField] private Button _deleteRangeButton; // 範囲削除ボタン
    [SerializeField] private Button _stageDuplicateButton; // ステージ複製ボタン
    // 3ページ目
    [SerializeField] private Button _evenlySpacedButton; // 均等配置ボタン
    public Button EvenlySpacedButton => _evenlySpacedButton;
    [SerializeField] private Button _autoCreateButton; // 自動生成ボタン
    public Button AutoCreateButton => _autoCreateButton;
    [SerializeField] private Button _playMakeButton; // 演奏作成ボタン
    public Button PlayMakeButton => _playMakeButton;
    [SerializeField] private Button _allClearButton; // 全削除ボタン
    public Button AllClearButton => _allClearButton;
    // 4ページ目
    [SerializeField] private ValueAdjuster _bpmInput;
    [SerializeField] private ValueAdjuster _startTimeInput;
    [SerializeField] private ValueAdjuster _endTimeInput;
    [SerializeField] private Button _estimateBpmButton;
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
    private Subject<Unit> _onPractice = new();
    public Observable<Unit> OnPractice => _onPractice;
    private Subject<Unit> _onDeleteRange = new();
    public Observable<Unit> OnDeleteRange => _onDeleteRange;
    private Subject<Unit> _onStageDuplicate = new();
    public Observable<Unit> OnStageDuplicate => _onStageDuplicate;
    private ReactiveProperty<float> _bpm = new(0f);
    public Observable<float> Bpm => _bpm;
    private ReactiveProperty<float> _startTime = new(0f);
    public Observable<float> StartTime => _startTime;
    private ReactiveProperty<float> _endTime = new(0f);
    public Observable<float> EndTime => _endTime;

    public void Init(MusicParameter musicParameter, AudioClip audioClip)
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
        _practiceButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onPractice.OnNext(default);
        }).AddTo(this);

        _copyButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onCopy.OnNext(Unit.Default);
        }).AddTo(this);
        _pasteButton.OnClickAsObservable().Subscribe(_ =>
        {
            SetMessageMask("貼り付け先の最初の列を選択してください", isDisplayCancelButton: true);
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
        }).AddTo(this);

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

        _bpmInput.Init(_bpm.Value, 0.1f);
        _bpmInput.CurrentValue.Subscribe(value =>
        {
            _bpm.Value = value;
        });
        _startTimeInput.Init(_startTime.Value, 0.1f);
        _startTimeInput.CurrentValue.Subscribe(value =>
        {
            _startTime.Value = value;
        });
        _endTimeInput.Init(_endTime.Value, 0.1f);
        _endTimeInput.CurrentValue.Subscribe(value =>
        {
            _endTime.Value = value;
        });
        _estimateBpmButton.OnClickAsObservable().Subscribe(async _ =>
        {
            // EstimateBPMに時間がかかるので先にSEを鳴らす
            SEManager.instance.PlaySe(SeName.Button1);
            SetMessageMask("計測中...", isDisplayCancelButton: false);
            await UniTask.NextFrame();
            var bpm = AudioClipUtility.EstimateBPM(audioClip);
            var startTime = AudioClipUtility.GetStartSoundTime(audioClip);
            _bpmInput.SetValue(bpm);
            _startTimeInput.SetValue(startTime);
            _messageMask.SetActive(false);
        }).AddTo(this);

        _bpm = new ReactiveProperty<float>(musicParameter.Bpm);
        _startTime = new ReactiveProperty<float>(musicParameter.StartTime);
        _endTime = new ReactiveProperty<float>(musicParameter.EndTime);
    }

    private void SetMessageMask(string message, bool isDisplayCancelButton)
    {
        _messageMask.SetActive(true);
        _messageText.text = message;
        _pasteCancelButton.gameObject.SetActive(isDisplayCancelButton);
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

    public void SetMusicParameter(MusicParameter musicParameter)
    {
        _bpmInput.SetValue(musicParameter.Bpm);
        _startTimeInput.SetValue(musicParameter.StartTime);
        _endTimeInput.SetValue(musicParameter.EndTime);
    }

    void Update()
    {
        _copyButton.interactable = _isSelectedLine;
        _inversionButton.interactable = _isSelectedLine;
        _selectCancelButton.interactable = _isSelectedLine;
        _deleteRangeButton.interactable = _isSelectedLine;
        _pasteButton.interactable = _isCopiedLine;
    }
}
