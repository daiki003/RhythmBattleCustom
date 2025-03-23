using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectTransformExtention
{
    public static RectTransform GetRectTransform(this Transform transform)
    {
        return transform as RectTransform;
    }

    public static void SetWidth(this Transform transform, float width)
    {
        var rectTransform = transform.GetRectTransform();
        var sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
        rectTransform.sizeDelta = sizeDelta;
    }

    public static void SetHeight(this Transform transform, float height)
    {
        var rectTransform = transform.GetRectTransform();
        var sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
        rectTransform.sizeDelta = sizeDelta;
    }

    public static void SetAnchoredPositionX(this Transform transform, float x)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
    }
    public static void SetAnchoredPositionX(this RectTransform rectTransform, float x)
    {
        rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
    }

    public static void SetAnchoredPositionY(this Transform transform, float y)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
    }
    public static void SetAnchoredPositionY(this RectTransform rectTransform, float y)
    {
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
    }

    public static void SetOffsetMaxX(this Transform transform, float x)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.offsetMax = new Vector2(-x, rectTransform.offsetMax.y);
    }
    public static void SetOffsetMaxY(this Transform transform, float y)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -y);
    }
    public static void SetOffsetMinX(this Transform transform, float x)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.offsetMin = new Vector2(x, rectTransform.offsetMin.y);
    }
    public static void SetOffsetMinY(this Transform transform, float y)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, y);
    }
}
