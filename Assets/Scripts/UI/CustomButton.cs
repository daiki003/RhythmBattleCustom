using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ButtonSeType
{
    None,
    Button1,
    Button2,
    Button3,
    Button4,
    Cancel,
    ChangePage,
}

public class CustomButton : Button
{
    [SerializeField] public Text _text;
    [SerializeField] public GameObject _highLight;
    [SerializeField] public ButtonSeType _seType;

    protected override void Awake()
    {
        this.OnClickAsObservable().Subscribe(_ =>
        {
            switch (_seType)
            {
                case ButtonSeType.Button1:
                    SEManager.instance.PlaySe(SeName.Button1);
                    break;
                case ButtonSeType.Button2:
                    SEManager.instance.PlaySe(SeName.Button2);
                    break;
                case ButtonSeType.Button3:
                    SEManager.instance.PlaySe(SeName.Button3);
                    break;
                case ButtonSeType.Button4:
                    SEManager.instance.PlaySe(SeName.Button4);
                    break;
                case ButtonSeType.Cancel:
                    SEManager.instance.PlaySe(SeName.Cancel);
                    break;
                case ButtonSeType.ChangePage:
                    SEManager.instance.PlaySe(SeName.ChangePage);
                    break;
            }
        }).AddTo(this);
    }

    public void SetText(string text)
    {
        _text.text = text;
    }

    public void SetHighLight(bool isActive)
    {
        _highLight.SetActive(isActive);
    }

    public void SetSeType(ButtonSeType seType)
    {
        _seType = seType;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CustomButton))]
public class CustomButtonEditor : UnityEditor.UI.ButtonEditor
{
   
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var component = (CustomButton) target;

        PropertyField(nameof(component._text), "Text");
        PropertyField(nameof(component._highLight), "HighLight");
        PropertyField(nameof(component._seType), "SeType");

        serializedObject.ApplyModifiedProperties();
    }

   private void PropertyField(string property, string label)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(property), new GUIContent(label));
    }
}
#endif