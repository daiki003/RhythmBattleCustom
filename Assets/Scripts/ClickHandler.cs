using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.UI;

public enum PositionType
{
    None,
    LeftButton,
    RightButton,
    LinePocket,
    Line,
}

public enum ClickType
{
    None,
    Click,
    Release,
}

public class ClickHandler
{
    public Subject<bool> OnClickButton = new Subject<bool>();
    public Subject<bool> OnReleaseButton = new Subject<bool>();
    public Subject<(int number, bool isLeft)> OnClickScoreLine = new Subject<(int number, bool isLeft)>();

    private Vector3 _startClickPosition;
    private const float _moveDiff = 5f;

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

        // スワイプは考慮しない
        bool isClick = clickType == ClickType.Click;
        var clickPosition = GetClickPosition(touch);
        switch (clickPosition)
        {
            // 演奏中左ボタン
            case PositionType.LeftButton:
                if (isClick)
                {
                    OnClickButton.OnNext(true);
                }
                else
                {
                    OnReleaseButton.OnNext(true);
                }
                break;
            // 演奏中右ボタン
            case PositionType.RightButton:
                if (isClick)
                {
                    OnClickButton.OnNext(false);
                }
                else
                {
                    OnReleaseButton.OnNext(false);
                }
                break;
            // 作成中ポケット
            case PositionType.LinePocket:
                Vector3 position;
#if UNITY_EDITOR
                position = Input.mousePosition;
#else
                position = touch.position;
#endif

                if (isClick)
                {
                    _startClickPosition = Input.mousePosition;
                }
                else
                {
                    if (!IsMovePosition(position))
                    {
                        var pocket = GetTargetComponent<LinePocket>(touch);
                        OnClickScoreLine.OnNext((pocket.Number, pocket.IsLeft));
                    }
                }
                break;
        }
    }

    // クリック位置の取得
    private PositionType GetClickPosition(Touch touch = default)
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
#else
        switch (touch.phase)
        {
            case TouchPhase.Began:
                return ClickType.Click;
            case TouchPhase.Ended:
                return ClickType.Release;
            // 以下未使用
            case TouchPhase.Moved:
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
