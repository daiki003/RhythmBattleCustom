using System;
using UnityEngine;
using UnityEngine.UI;

public class JumpButtonIcon : MonoBehaviour
{
    [SerializeField] private Image _iconImage;

    private int _number;
    public int Number => _number;
    private const string _iconSpritePath = "Images/NumberButton/{0}";

    public void Awake()
    {
        TutorialManager.Instance.AddTargetRect("TJIcon", transform as RectTransform);
    }

    public void SetNumber(int number)
    {
        _number = number;
        _iconImage.sprite = Resources.Load<Sprite>(string.Format(_iconSpritePath, number));
    }

    public void SetPosX(float timeRate, float width)
    {
        var rect = transform as RectTransform;
        rect.SetAnchoredPositionX(width * timeRate);
    }

    public void Destroy()
    {
        _iconImage.sprite = null;
        Destroy(gameObject);
    }
}
