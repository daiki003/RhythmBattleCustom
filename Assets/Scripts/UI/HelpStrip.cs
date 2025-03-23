using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpStrip : MonoBehaviour
{
    [SerializeField] Image _stripImage;

    private const string _helpStripSpritePath = "Images/HelpDialog/";

    private const float _baseWitdh = 700f;
    private const float _sizeRatio = 1.35f;

    public void Init(string helpId)
    {
        var stripSprite = ResourceManager.LoadSprite(_helpStripSpritePath + helpId);
        _stripImage.sprite = stripSprite;
        _stripImage.transform.GetRectTransform().sizeDelta = new Vector2(_baseWitdh * _sizeRatio, stripSprite.texture.height * _sizeRatio);
    }
}
