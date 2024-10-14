using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.UI;

public class ClickHandler
{
    public Subject<bool> OnClickButton = new Subject<bool>();
    public Subject<bool> OnReleaseButton = new Subject<bool>();
    public void Update()
    {
#if UNITY_EDITOR
        // Unity上ではタッチ操作ができないのでこちら
        if (Input.GetMouseButtonDown(0))
        {
            if (IsOnTargetTag("LeftButton"))
            {
                OnClickButton.OnNext(true);
            }
            if (IsOnTargetTag("RightButton"))
            {
                OnClickButton.OnNext(false);
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (IsOnTargetTag("LeftButton"))
            {
                OnReleaseButton.OnNext(true);
            }
            if (IsOnTargetTag("RightButton"))
            {
                OnReleaseButton.OnNext(false);
            }
        }
#endif

        var touchCount = Input.touchCount;
        for (var i = 0; i < touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (IsOnTargetTag("LeftButton"))
                    {
                        OnClickButton.OnNext(true);
                    }
                    if (IsOnTargetTag("RightButton"))
                    {
                        OnClickButton.OnNext(false);
                    }
                    break;
                case TouchPhase.Moved:
                    // 画面上で指が動いたときに行いたい処理をここに書く
                    break;
                case TouchPhase.Stationary:
                    // 指が画面に触れているが動いてはいない時に行いたい処理をここに書く
                    break;
                case TouchPhase.Ended:
                    // 画面から指が離れた時に行いたい処理をここに書く
                    if (IsOnTargetTag("LeftButton"))
                    {
                        OnReleaseButton.OnNext(true);
                    }
                    if (IsOnTargetTag("RightButton"))
                    {
                        OnReleaseButton.OnNext(false);
                    }
                    break;
                case TouchPhase.Canceled:
                    // システムがタッチの追跡をキャンセルした時に行いたい処理をここに書く
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private List<RaycastResult> GetRaycastResults()
	{
		PointerEventData pointer = new PointerEventData(EventSystem.current);
		pointer.position = Input.mousePosition;
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

    public bool IsOnTargetTag(string tagName)
	{
		return GetRaycastResults().Any(r => r.gameObject.CompareTag(tagName));
	}

    public string GetFirstTag()
    {
        return GetRaycastResults().Select(r => r.gameObject.tag).FirstOrDefault(r => r != "Untagged");
    }

    public List<string> GetTargetTagList()
	{
		return GetRaycastResults().Select(r => r.gameObject.tag).ToList();
	}

	public T GetTargetComponent<T>()
	{
        var component = GetRaycastResults().Select(r => r.gameObject.GetComponent<T>()).FirstOrDefault(s => s != null);
        if (component == null)
        {
            // なければ親要素まで見る
            component = GetRaycastResults().Select(r => r.gameObject.transform.parent.GetComponent<T>()).FirstOrDefault(s => s != null);
        }
        return component;
	}
}
