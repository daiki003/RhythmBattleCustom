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
    public Subject<Unit> OnClickLeftButton = new Subject<Unit>();
    public Subject<Unit> OnClickRightButton = new Subject<Unit>();
    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsOnTargetTag("LeftButton"))
            {
                OnClickLeftButton.OnNext(default);
            }
            if (IsOnTargetTag("RightButton"))
            {
                OnClickRightButton.OnNext(default);
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
