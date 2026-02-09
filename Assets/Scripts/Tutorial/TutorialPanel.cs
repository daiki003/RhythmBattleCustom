using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanel : MonoBehaviour
{
    public enum MessagePosition
    {
        Top,
        MiddleTop,
        MiddleUnder,
        Under
    }

    [SerializeField] private RectTransform _unmaskRoot;
    [SerializeField] private RectTransform _touchableRoot;
    [SerializeField] private RectTransform _messagePanel;
    [SerializeField] private Image _unmask;
    [SerializeField] private Image _touchableUnmask;
    [SerializeField] private RectTransform _emphasisPanel;
    [SerializeField] private CanvasGroup _emphasisCanvasGroup;
    [SerializeField] private Text _messageText;

    [SerializeField] private Button _messageNextButton;
    [SerializeField] private Button _exitTutorialButton;

    [SerializeField] private GameObject _nextMessageArrow;
    [SerializeField] private CanvasGroup _messageeArrowCanvasGroup;
    [SerializeField] private TargetArrow _targetArrow;

    private bool _isWaitTapMessage;
    private bool _isWaitPushedButton;
    private string _waitingAdvanceId;
    private bool _isEmphasis;
    private bool _isExit;
    private CancellationTokenSource _onCommandCts;

    private const string _commandListPath = "ScriptableObject/Tutorial/{0}";

    public void Init()
    {
        _isExit = false;
        _messageNextButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (_isWaitTapMessage)
            {
                SEManager.instance.PlaySe(SeName.Button1);
                _isWaitTapMessage = false;
            }
        }).AddTo(this);
        _exitTutorialButton.OnClickAsObservable().Subscribe(_ =>
        {
            _onCommandCts?.CancelAndDispose();
            _onCommandCts = null;
            _isExit = true;
        }).AddTo(this);
    }

    public async UniTask PlayTutorialAsync(TutorialCommandList commandList)
    {
        if (commandList == null) return;
        try
        {
            foreach (var command in commandList.Commands)
            {
                if (_isExit) break;
                await ConductCommandAsync(command);
            }
        }
        catch (OperationCanceledException)
        {
            
        }
    }

    public async UniTask ConductCommandAsync(TutorialCommandParam command)
    {
        InitOnStartCommand();

        _isEmphasis = command.IsEmphasis;
        _emphasisPanel.gameObject.SetActive(_isEmphasis);
        if (_isEmphasis)
        {
            StartBlinkEmphasis().Forget();
        }

        // アンマスクの設定
        SetUnmask(_unmaskRoot, command.MaskTarget);
        if (!string.IsNullOrEmpty(command.ArrowTarget?.TargetId))
        {
            _targetArrow.Show(command.ArrowTarget, command.TargetArrowVector);
            // 矢印があるならタッチ可能にする
            _touchableUnmask.raycastTarget = false;
            SetUnmask(_touchableRoot, command.TouchableTarget);
        }

        // メッセージ表示位置の設定
        _messagePanel.transform.localPosition = new Vector3(0, command.MessagePositionY, 0);
        _messageText.text = command.Message;

        switch (command.AdvanceType)
        {
            case TutorialAdvanceType.TapMessage:
                _isWaitTapMessage = true;
                StartBlinkArrowAsync().Forget();
                await UniTask.WaitWhile(() => _isWaitTapMessage, cancellationToken: _onCommandCts.Token);
                break;
            case TutorialAdvanceType.PushButton:
                if (TutorialManager.Instance.TryGetTargetButton(command.AdvanceId, out var button))
                {
                    var buttonRect = button.gameObject.GetComponent<RectTransform>();
                    _unmask.raycastTarget = false;
                    _isWaitPushedButton = true;
                    using var _ = button.OnClickAsObservable().Subscribe(_ =>
                    {
                        _isWaitPushedButton = false;
                    });
                    await UniTask.WaitWhile(() => _isWaitPushedButton, cancellationToken: _onCommandCts.Token);
                }
                break;
            case TutorialAdvanceType.AdvanceId:
                _unmask.raycastTarget = false;
                _waitingAdvanceId = command.AdvanceId;
                if (!string.IsNullOrEmpty(_waitingAdvanceId))
                {
                    await UniTask.WaitUntil(() => string.IsNullOrEmpty(_waitingAdvanceId), cancellationToken: _onCommandCts.Token);
                }
                break;
            default:
                break;
        }

        _targetArrow.Hide();
        _touchableUnmask.raycastTarget = true;
        _onCommandCts?.CancelAndDispose();
        _onCommandCts = null;
        _isEmphasis = false;
    }

    // コマンド実行前の初期化
    private void InitOnStartCommand()
    {
        _onCommandCts?.CancelAndDispose();
        _onCommandCts = new CancellationTokenSource();
        _targetArrow.Hide();
        _unmask.raycastTarget = true;
    }

    public void AdvanceStepById(string advanceId)
    {
        if (_waitingAdvanceId == advanceId)
        {
            _waitingAdvanceId = "";
        }
    }

    private async UniTask StartBlinkArrowAsync()
    {
        try
        {
            while (_isWaitTapMessage)
            {
                _messageeArrowCanvasGroup.alpha = 1;
                // フェードアウト（0.5秒で透明に）
                await _messageeArrowCanvasGroup.DOFade(0f, 0.7f).ToUniTask(cancellationToken: _onCommandCts.Token);
                await UniTask.WaitForSeconds(0.2f, cancellationToken: _onCommandCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _messageeArrowCanvasGroup.alpha = 0;
        }
    }

    private async UniTask StartBlinkEmphasis()
    {
        try
        {
            while (_isEmphasis)
            {
                await _emphasisCanvasGroup.DOFade(1f, 0.5f).ToUniTask(cancellationToken: _onCommandCts.Token);
                await _emphasisCanvasGroup.DOFade(0f, 0.5f).ToUniTask(cancellationToken: _onCommandCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _emphasisCanvasGroup.alpha = 0;
        }
    }

    public void Dispose()
    {
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        _onCommandCts?.CancelAndDispose();
        _onCommandCts = null;
    }

    void LateUpdate()
    {
        if (_isEmphasis)
        {
            if (!_unmask.TryGetComponent<RectTransform>(out var unmaskRect))
            {
                return;
            }
            _emphasisPanel.position = unmaskRect.position;
            _emphasisPanel.rotation = unmaskRect.rotation;
            _emphasisPanel.localScale = unmaskRect.localScale;
            _emphasisPanel.sizeDelta = unmaskRect.rect.size + new Vector2(25, 25);
        }
    }

    private void SetUnmask(RectTransform unmaskRect, TutorialTarget target)
    {
        var targetRect = TutorialManager.Instance.GetTargetRect(target);
        if (targetRect == null)
        {
            unmaskRect.localScale = Vector3.zero;
            return;
        }
        // FitTarget の位置・サイズをそのまま取得
        unmaskRect.pivot = targetRect.pivot;
        unmaskRect.position = targetRect.position;
        unmaskRect.rotation = targetRect.rotation;
        Vector3 lossyScale = targetRect.lossyScale;
        Vector3 lossyScale2 = unmaskRect.parent.lossyScale;
        unmaskRect.localScale = new Vector3(lossyScale.x / lossyScale2.x, lossyScale.y / lossyScale2.y, lossyScale.z / lossyScale2.z);
        unmaskRect.sizeDelta = targetRect.rect.size;
        Vector2 anchorMax = (unmaskRect.anchorMin = new Vector2(0.5f, 0.5f));
        unmaskRect.anchorMax = anchorMax;

        if (target.OverrideParam != null)
        {
            unmaskRect.sizeDelta += new Vector2(target.OverrideParam.Width, target.OverrideParam.Height);
            unmaskRect.localPosition += new Vector3(target.OverrideParam.PositionOffset.x, target.OverrideParam.PositionOffset.y, 0f);
        }
    }
}
