using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.UI;
using Unity.VisualScripting;

public enum PositionType
{
    None,
    LeftButton,
    RightButton,
    LinePocket,
    Line,
    LineNumber,
}

public enum ClickType
{
    None,
    Click,
    Release,
    Moved,
}

public class ClickHandler
{
    public Subject<bool> OnClickButton = new Subject<bool>();
    public Subject<bool> OnReleaseButton = new Subject<bool>();
    public Subject<(int number, bool isLeft)> OnClickScoreLinePocket = new Subject<(int number, bool isLeft)>();
    public Subject<int> OnClickScoreLine = new Subject<int>();
    public Subject<int> OnClickScoreLineNumber = new Subject<int>();
    public Subject<float> OnDragBattleBg = new();

    public Subject<float> OnPinchOut = new();
    public Subject<float> OnPinchIn = new();

    private Vector3 _startClickPosition;
    private Vector3? _lastPosition;
    private const float _moveDiff = 5f;

#if !UNITY_EDITOR
    private float _previousDistance;
#endif

    public void Update()
    {
#if UNITY_EDITOR
        // PCの場合
        ClickAction();

        // キーの検知
        if (Input.GetKeyDown(KeyCode.V))
        {
            OnClickButton.OnNext(true);
        }
        if (Input.GetKeyUp(KeyCode.V))
        {
            OnReleaseButton.OnNext(true);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            OnClickButton.OnNext(false);
        }
        if (Input.GetKeyUp(KeyCode.M))
        {
            OnReleaseButton.OnNext(false);
        }
#else
        // スマホの場合
        var touchCount = Input.touchCount;
        for (var i = 0; i < touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            ClickAction(touch);
        }
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            float currentDistance = Mathf.Abs(touch0.position.y - touch1.position.y);
            if (_previousDistance == 0f)
            {
                _previousDistance = currentDistance;
            }

            float deltaDistance = currentDistance - _previousDistance;
            if (deltaDistance < 0)
            {
                // ピンチイン（縮小）
                OnPinchIn.OnNext(currentDistance);
            }
            else if (deltaDistance > 0)
            {
                // ピンチアウト（拡大）
                OnPinchOut.OnNext(currentDistance);
            }
            // 前回の距離を更新
            _previousDistance = currentDistance;
        }
        else
        {
            _previousDistance = 0f;
        }
#endif
    }

    private void ClickAction(Touch touch = default)
    {
        var clickType = GetClickType(touch);
        // クリックしていなければ何もしない
        if (clickType == ClickType.None)
        {
            return;
        }

        var clickPosition = GetClickPositionType(touch);
        var position = GetClickPosition(touch);
        if (clickType == ClickType.Click)
        {
            _startClickPosition = Input.mousePosition;
            switch (clickPosition)
            {
                // 演奏中左ボタン
                case PositionType.LeftButton:
                    OnClickButton.OnNext(true);
                    break;
                // 演奏中右ボタン
                case PositionType.RightButton:
                    OnClickButton.OnNext(false);
                    break;
                // 作成中ポケット
                case PositionType.LinePocket:
                case PositionType.Line:
                case PositionType.LineNumber:
                    break;
            }
        }
        else if (clickType == ClickType.Release)
        {
            _lastPosition = null;
            switch (clickPosition)
            {
                // 演奏中左ボタン
                case PositionType.LeftButton:
                    OnReleaseButton.OnNext(true);
                    break;
                // 演奏中右ボタン
                case PositionType.RightButton:
                    OnReleaseButton.OnNext(false);
                    break;
                // 作成中ポケット
                case PositionType.LinePocket:
                    if (!IsMovePosition(position))
                    {
                        var pocket = GetTargetComponent<LinePocket>(touch);
                        OnClickScoreLinePocket.OnNext((pocket.Number, pocket.IsLeft));
                    }
                    break;
                case PositionType.Line:
                    if (!IsMovePosition(position))
                    {
                        var line = GetTargetComponent<ScoreLine>(touch);
                        OnClickScoreLine.OnNext(line.LineNumber);
                    }
                    break;
                case PositionType.LineNumber:
                    if (!IsMovePosition(position))
                    {
                        var line = GetTargetComponent<ScoreLine>(touch);
                        OnClickScoreLineNumber.OnNext(line.LineNumber);
                    }
                    break;
            }
        }
        else if (clickType == ClickType.Moved)
        {
            if (IsOnTargetTag("BattleBg", touch))
            {
                OnDragBattleBg.OnNext(MoveVectorY(position));
                _lastPosition = position;
            }
        }
    }

    // クリック位置の取得
    private PositionType GetClickPositionType(Touch touch = default)
    {
        if (IsOnTargetTag("LeftButton", touch))
        {
            return PositionType.LeftButton;
        }
        if (IsOnTargetTag("RightButton", touch))
        {
            return PositionType.RightButton;
        }
        if (IsOnTargetTag("LinePocket", touch))
        {
            return PositionType.LinePocket;
        }
        if (IsOnTargetTag("LineNumber", touch))
        {
            return PositionType.LineNumber;
        }
        // Pocketのほうが優先
        if (IsOnTargetTag("Line", touch))
        {
            return PositionType.Line;
        }
        return PositionType.None;
    }

    // クリックの状態を取得
    private ClickType GetClickType(Touch touch)
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            return ClickType.Click;
        }
        if (Input.GetMouseButtonUp(0))
        {
            return ClickType.Release;
        }
        if (Input.GetMouseButton(0))
        {
            return ClickType.Moved;
        }
#else
        switch (touch.phase)
        {
            case TouchPhase.Began:
                return ClickType.Click;
            case TouchPhase.Ended:
                return ClickType.Release;
            // 以下未使用
            case TouchPhase.Moved:
                return ClickType.Moved;
            case TouchPhase.Stationary:
                // 指が画面に触れているが動いてはいない時に行いたい処理をここに書く
            case TouchPhase.Canceled:
                // システムがタッチの追跡をキャンセルした時に行いたい処理をここに書く
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
#endif
        return ClickType.None;
    }

    private List<RaycastResult> GetRaycastResults(Vector2 touchPosition)
	{
		PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = touchPosition;
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointer, results);

        var returnResults = new List<RaycastResult>();
        foreach (RaycastResult raycastResult in results)
        {
            returnResults.Add(raycastResult);
            // Blockタグを持ったオブジェクトがあった場合、それ以下の要素は取得しない
            // if (raycastResult.gameObject.CompareTag("Block"))
            // {
            //     break;
            // }
            // 有効なButtonコンポーネントを持っている場合、それ以下の要素は取得しない
            var button = raycastResult.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                break;
            }
        }
		return returnResults;
	}

    private Vector3 GetClickPosition(Touch touch = default)
    {
#if UNITY_EDITOR
        return Input.mousePosition;
#else
        return touch.position;
#endif
    }

    public bool IsOnTargetTag(string tagName, Touch touch = default)
	{
		return GetTagNames(touch).Any(n => n == tagName);
	}

    public List<string> GetTagNames(Touch touch = default)
    {
        List<RaycastResult> results;
#if UNITY_EDITOR
        results = GetRaycastResults(Input.mousePosition);
#else
        results = GetRaycastResults(touch.position);
#endif
        return results.Select(r => r.gameObject.tag).ToList();
    }

    // Y方向の移動量を方向込みで返す
    private float MoveVectorY(Vector3 currentPosition)
    {
        if (_lastPosition == null)
        {
            return 0f;
        }
        return currentPosition.y - _lastPosition?.y ?? 0f;
    }

    public bool IsMovePosition(Vector3 currentPosition)
    {
        if (_startClickPosition == null)
        {
            return false;
        }
        return Math.Abs(currentPosition.x - _startClickPosition.x) > _moveDiff && Math.Abs(currentPosition.y - _startClickPosition.y) > _moveDiff;
    }

    // public string GetFirstTag(Touch touch = default)
    // {
    //     return GetRaycastResults(touch.position).Select(r => r.gameObject.tag).FirstOrDefault(r => r != "Untagged");
    // }

    // public List<string> GetTargetTagList(Touch touch = default)
	// {
	// 	return GetRaycastResults(touch).Select(r => r.gameObject.tag).ToList();
	// }

	public T GetTargetComponent<T>(Touch touch = default)
	{
#if UNITY_EDITOR
        var position = Input.mousePosition;
#else
        var position = touch.position;
#endif
        var component = GetRaycastResults(position).Select(r => r.gameObject.GetComponent<T>()).FirstOrDefault(s => s != null);
        if (component == null)
        {
            // なければ親要素まで見る
            component = GetRaycastResults(position).Select(r => r.gameObject.transform.parent.GetComponent<T>()).FirstOrDefault(s => s != null);
        }
        return component;
	}
}
