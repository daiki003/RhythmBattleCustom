using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TutorialCommandList))]
public class TutorialCommandEditor : Editor
{
    private SerializedProperty _tutorialName;
    private SerializedProperty _tutorialType;
    private SerializedProperty _scoreMakerStartParam;
    private SerializedProperty _paramsList;

    private const float _buttonWidth = 45;
    private const float _buttonHeight = 18;

    void OnEnable()
    {
        _tutorialName = serializedObject.FindProperty("TutorialName");
        _tutorialType = serializedObject.FindProperty("TutorialType");
        _scoreMakerStartParam = serializedObject.FindProperty("ScoreMakerStartParam");
        _paramsList = serializedObject.FindProperty("Commands");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_tutorialName);
        EditorGUILayout.PropertyField(_tutorialType);
        EditorGUILayout.PropertyField(_scoreMakerStartParam);

        EditorGUILayout.Space();

        // List サイズ
        EditorGUILayout.PropertyField(_paramsList.FindPropertyRelative("Array.size"));
        for (int i = 0; i < _paramsList.arraySize; i++)
        {
            var element = _paramsList.GetArrayElementAtIndex(i);

            var advanceType = element.FindPropertyRelative("AdvanceType");
            var advanceId = element.FindPropertyRelative("AdvanceId");
            var maskTarget = element.FindPropertyRelative("MaskTarget");
            var arrowTarget = element.FindPropertyRelative("ArrowTarget");
            var touchableTarget = element.FindPropertyRelative("TouchableTarget");
            var targetArrowVector = element.FindPropertyRelative("TargetArrowVector");
            var isEmphasis = element.FindPropertyRelative("IsEmphasis");
            var message = element.FindPropertyRelative("Message");
            var messagePositionY = element.FindPropertyRelative("MessagePositionY");

            EditorGUILayout.BeginVertical(GUI.skin.box);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            EditorGUILayout.LabelField($"Command {i}", headerStyle);
            EditorGUILayout.PropertyField(advanceType);
            if ((TutorialAdvanceType)advanceType.enumValueIndex != TutorialAdvanceType.TapMessage)
            {
                EditorGUILayout.PropertyField(advanceId);
            }

            // メッセージ
            EditorGUILayout.PropertyField(message);
            EditorGUILayout.PropertyField(messagePositionY);

            // マスク
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(maskTarget);
            InheritButton(maskTarget, "TargetId", element, "AdvanceId");
            EditorGUILayout.EndHorizontal();
            if (maskTarget.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(isEmphasis);
                EditorGUI.indentLevel--;
            }

            // 操作が必要な場合のパラメータ
            if ((TutorialAdvanceType)advanceType.enumValueIndex != TutorialAdvanceType.TapMessage)
            {
                arrowTarget.managedReferenceValue ??= new TutorialTarget();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(arrowTarget);
                InheritButton(arrowTarget, "TargetId", maskTarget, "TargetId", touchableTarget);
                EditorGUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(arrowTarget.FindPropertyRelative("TargetId").stringValue))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(targetArrowVector);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(touchableTarget);
                InheritButton(touchableTarget, "TargetId", arrowTarget, "TargetId");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            // 削除ボタン
            if (GUILayout.Button("Remove"))
            {
                _paramsList.DeleteArrayElementAtIndex(i);
                break;
            }

            // 追加ボタン
            if (GUILayout.Button("Insert Command"))
            {
                _paramsList.InsertArrayElementAtIndex(i);
                var elem = _paramsList.GetArrayElementAtIndex(i + 1);
                elem.FindPropertyRelative("MaskTarget").managedReferenceValue = null;
                elem.FindPropertyRelative("ArrowTarget").managedReferenceValue = null;
                elem.FindPropertyRelative("TouchableTarget").managedReferenceValue = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // 追加ボタン
        if (GUILayout.Button("Add Command"))
        {
            _paramsList.InsertArrayElementAtIndex(_paramsList.arraySize);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void InheritButton(SerializedProperty targetProp, string targetName, SerializedProperty baseProp, string baseName, SerializedProperty subTargetProp = null)
    {
        if (string.IsNullOrEmpty(targetProp.FindPropertyRelative(targetName).stringValue))
        {
            // 継承ボタン
            if (GUILayout.Button("Inherit", GUILayout.Width(_buttonWidth), GUILayout.Height(_buttonHeight)))
            {
                targetProp.FindPropertyRelative(targetName).stringValue = baseProp.FindPropertyRelative(baseName).stringValue;
                if (subTargetProp != null)
                {
                    subTargetProp.FindPropertyRelative(targetName).stringValue = baseProp.FindPropertyRelative(baseName).stringValue;
                }
            }
        }
        else
        {
            // 削除ボタン
            if (GUILayout.Button("Clear", GUILayout.Width(_buttonWidth), GUILayout.Height(_buttonHeight)))
            {
                targetProp.FindPropertyRelative(targetName).stringValue = "";
            }
        }
    }
    
    private void CustomSizePropertyField(string label, float labelSize, SerializedProperty property, float mainSize)
    {
        EditorGUILayout.LabelField(label, GUILayout.Width(labelSize));
        EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(mainSize));
    }
}

[CustomPropertyDrawer(typeof(TutorialTarget))]
public class TutorialTargetEditor : StringKeyReferenceDrawerBase<TutorialTarget>
{
    protected override string KeyPropertyName => "TargetId";
    protected override string NullablePropertyName => "OverrideParam";
    protected override List<string> PropertyNames => new List<string>()
    {
        "OverrideParam",
    };
}

[CustomPropertyDrawer(typeof(ArrowParam))]
public class ArrowTargetEditor : NullableReferenceDrawerBase<ArrowParam>
{
    protected override List<string> PropertyNames => new List<string>()
    {
        "TargetArrowVector",
        "OverrideTarget",
        "TouchableOverride",
    };
}

[CustomPropertyDrawer(typeof(OverrideTargetParam))]
public class OverrideTargetParamEditor : NullableReferenceDrawerBase<OverrideTargetParam>
{
    protected override List<string> PropertyNames => new List<string>()
    {
        "Width",
        "Height",
        "PositionOffset"
    };
}

public abstract class NullableReferenceDrawerBase<T> : ReferenceDrawerBase<T> where T : class, new()
{
    public override float OnGUIMain(Rect position, SerializedProperty property, GUIContent label, bool onlyCalc = false)
    {
        float firstPosY = position.y;
        var headerRect = position;
        headerRect.height = EditorGUIUtility.singleLineHeight;
        Rect contentRect = EditorGUI.PrefixLabel(headerRect, label);

        if (property.managedReferenceValue == null)
        {
            if (!onlyCalc) DrawNullState(contentRect, property);
        }


        else
        {
            if (!onlyCalc) DrawHeaderButtons(contentRect, property);
            position = BeginIndentedContent(position);
            position = DrawValueContents(position, property, onlyCalc);
        }
        return position.y - firstPosY;
    }

    protected virtual void DrawNullState(Rect contentRect, SerializedProperty property)
    {
        float line = EditorGUIUtility.singleLineHeight;

        EditorGUI.LabelField(new Rect(contentRect.x, contentRect.y, contentRect.width - ButtonWidth, line), "None");
        if (GUI.Button(new Rect(contentRect.xMax - ButtonWidth, contentRect.y, ButtonWidth, line), "Create"))
        {
            property.managedReferenceValue = new T();
        }
    }

    protected virtual void DrawHeaderButtons(Rect contentRect, SerializedProperty property)
    {
        float line = EditorGUIUtility.singleLineHeight;

        if (GUI.Button(new Rect(contentRect.xMax - ButtonWidth, contentRect.y, ButtonWidth, line), "Clear"))
        {
            property.managedReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }
    }
}

public abstract class StringKeyReferenceDrawerBase<T> : ReferenceDrawerBase<T> where T : class, new()
{
    protected virtual string KeyPropertyName => "";
    protected virtual string NullablePropertyName => "";
    public override float OnGUIMain(Rect position, SerializedProperty property, GUIContent label, bool onlyCalc = false)
    {
        float firstPosY = position.y;
        // var headerRect = position;
        // headerRect.height = EditorGUIUtility.singleLineHeight;

        property.managedReferenceValue ??= new TutorialTarget();
        position = EditorGUI.PrefixLabel(position, label);

        // キープロパティの入力 
        var targetIdProp = property.FindPropertyRelative(KeyPropertyName);
        var overrideProp = property.FindPropertyRelative(NullablePropertyName);
        position = DrawPropertyWithButton(targetIdProp, position, GUIContent.none, () =>
        {
            if (overrideProp.managedReferenceValue == null)
            {
                overrideProp.managedReferenceValue = new OverrideTargetParam();
            }
            else
            {
                overrideProp.managedReferenceValue = null;
            }
        }, overrideProp.managedReferenceValue == null ? "▶" : "▼", onlyCalc);
        position.x = IndentSize * 2;
        position.width = 300f;

        // キーが入力されていれば他のプロパティも表示
        if (!string.IsNullOrEmpty(targetIdProp.stringValue) && overrideProp.managedReferenceValue != null)
        {
            position = DrawValueContents(position, property, onlyCalc);
        }
        return position.y - firstPosY;
    }
}

public abstract class ReferenceDrawerBase<T> : PropertyDrawer where T : class, new()
{
    protected const float ButtonWidth = 55f;
    protected const float OverrideButtonWidth = 20f;
    protected const float IndentSize = 15f;
    protected virtual List<string> PropertyNames => new List<string>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        OnGUIMain(position, property, label);
        EditorGUI.EndProperty();
    }

    public virtual float OnGUIMain(Rect position, SerializedProperty property, GUIContent label, bool onlyCalc = false)
    {
        float firstPosY = position.y;
        var headerRect = position;
        headerRect.height = EditorGUIUtility.singleLineHeight;

        Rect contentRect = EditorGUI.PrefixLabel(headerRect, label);
        position = BeginIndentedContent(position);
        position = DrawValueContents(position, property, onlyCalc);
        return position.y - firstPosY;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight;

        return OnGUIMain(new Rect(0, 0, 0, 0), property, label, true);
    }

    protected Rect BeginIndentedContent(Rect position)
    {
        position.y += EditorGUIUtility.singleLineHeight;
        position.x += IndentSize;
        position.width -= IndentSize;
        return position;
    }

    protected Rect DrawProperty(SerializedProperty property, Rect rect, GUIContent label, bool onlyCalc = false)
    {
        float h = EditorGUI.GetPropertyHeight(property, true);
        rect.height = h;
        if (!onlyCalc) EditorGUI.PropertyField(rect, property, label, true);
        rect.y += h;
        return rect;
    }

    protected Rect DrawPropertyWithButton(SerializedProperty property, Rect rect, GUIContent label, Action onClick, string buttonLabel, bool onlyCalc = false)
    {
        float h = EditorGUI.GetPropertyHeight(property, true);
        rect.height = h;
        var buttonRect = rect;
        if (!onlyCalc)
        {
            buttonRect.width = OverrideButtonWidth;
            buttonRect.x = rect.xMin - OverrideButtonWidth - 2;
            EditorGUI.PropertyField(rect, property, label, true);
            if (GUI.Button(buttonRect, buttonLabel))
            {
                onClick?.Invoke();
            }
        }
        rect.y += h;
        return rect;
    }

    /// <summary>
    /// 戻り値：この Property 全体の高さ
    /// </summary>
    protected Rect DrawValueContents(Rect position, SerializedProperty property, bool onlyCalc = false)
    {
        var props = new List<SerializedProperty>();
        foreach (var propName in PropertyNames)
        {
            props.Add(property.FindPropertyRelative(propName));
        }

        foreach (var prop in props)
        {
            position = DrawProperty(prop, position, null, onlyCalc);
        }
        return position;
    }
}
