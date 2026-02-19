using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class TargetArrow : MonoBehaviour
{
    public enum ArrowVector
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    [SerializeField] private RectTransform _arrowRect;

    private CancellationTokenSource _arrowCts;
    private const float _positionOffset = 0.4f;
    private const float _amplitude = 40;

    public void Show(TutorialTarget target, ArrowVector vector)
    {
        gameObject.SetActive(true);
        // 本体のポジションを設定
        transform.position = GetArrowFirstPosition(target, vector);
        if (target.OverrideParam != null)
        {
            transform.localPosition += new Vector3(target.OverrideParam.PositionOffset.x, target.OverrideParam.PositionOffset.y, 0f);
        }
        // 矢印の向きを変える
        _arrowRect.localEulerAngles = GetRotation(vector);

        _arrowCts?.Cancel();
        _arrowCts = new CancellationTokenSource();
        VibrateArrow(vector).Forget();
    }

    public void Hide()
    {
        _arrowCts?.Cancel();
        _arrowCts = null;
        gameObject.SetActive(false);
    }

    private async UniTask VibrateArrow(ArrowVector arrowVector)
    {
        _arrowRect.localPosition = Vector3.zero;
        var toVector = VibrateVector(arrowVector);
        var backVector = -toVector;
        while (_arrowCts != null && !_arrowCts.IsCancellationRequested)
        {
            await _arrowRect.DOLocalMove(toVector, 0.15f).SetRelative(true).ToUniTask(cancellationToken: _arrowCts.Token);
            await _arrowRect.DOLocalMove(backVector, 0.65f).SetRelative(true).ToUniTask(cancellationToken: _arrowCts.Token);
        }
    }

    private Vector3 GetRotation(ArrowVector arrowVector)
    {
        var rotationZ = arrowVector switch
        {
            ArrowVector.Up => 0f,
            ArrowVector.Down => 180f,
            ArrowVector.Left => 90f,
            ArrowVector.Right => -90f,
            _ => 0f
        };
        return new Vector3(0f, 0f, rotationZ);
    }

    private Vector3 GetArrowFirstPosition(TutorialTarget target, ArrowVector vector)
    {
        var targetRect = TutorialManager.Instance.GetTargetRect(target);
        Vector3[] corners = new Vector3[4];
        var originalSizeDelta = targetRect.sizeDelta;
        if (target.OverrideParam != null)
        {
            targetRect.sizeDelta = new Vector2(originalSizeDelta.x + target.OverrideParam.Width, originalSizeDelta.y + target.OverrideParam.Height);
        }
        targetRect.GetWorldCorners(corners);
        targetRect.sizeDelta = originalSizeDelta;
        return vector switch
        {
            ArrowVector.Up => (corners[0] + corners[3]) * 0.5f - new Vector3(0f, _positionOffset, 0f),
            ArrowVector.Down => (corners[1] + corners[2]) * 0.5f + new Vector3(0f, _positionOffset, 0f),
            ArrowVector.Left => (corners[2] + corners[3]) * 0.5f + new Vector3(_positionOffset, 0f, 0f),
            ArrowVector.Right => (corners[0] + corners[1]) * 0.5f - new Vector3(_positionOffset, 0f, 0f),
            _ => Vector3.zero
        };
    }

    private Vector3 VibrateVector(ArrowVector arrowVector)
    {
        return arrowVector switch
        {
            ArrowVector.Up => new Vector3(0f, _amplitude, 0f),
            ArrowVector.Down => new Vector3(0f, -_amplitude, 0f),
            ArrowVector.Left => new Vector3(-_amplitude, 0f, 0f),
            ArrowVector.Right => new Vector3(_amplitude, 0f, 0f),
            _ => Vector3.zero
        };
    }
}
