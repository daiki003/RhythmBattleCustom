using System;
using UnityEngine;
using UnityEngine.UI;

public class JumpButtonIcon : MonoBehaviour
{
    [SerializeField] private Image _iconImage;

    private int _number;
    private const string _iconSpritePath = "Images/NumberButton/{0}";
    public void SetSprite(int number)
    {
        _number = number;
        _iconImage.sprite = Resources.Load<Sprite>(string.Format(_iconSpritePath, number));
    }
}
