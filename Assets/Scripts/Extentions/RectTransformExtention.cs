using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectTransformExtention
{
    public static RectTransform GetRectTransform(this Transform transform)
    {
        return transform as RectTransform;
    }

    public static void SetAnchoredPositionX(this Transform transform, float x)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
    }

    public static void SetAnchoredPositionY(this Transform transform, float y)
    {
        var rectTransform = transform.GetRectTransform();
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
    }

    public static void SetAnchoredPositionX(this RectTransform rectTransform, float x)
    {
        rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
    }

    public static void SetAnchoredPositionY(this RectTransform rectTransform, float y)
    {
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
    }
}
