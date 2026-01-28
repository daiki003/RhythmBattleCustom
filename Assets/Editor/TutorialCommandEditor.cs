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
            var maskTarget = element.FindPropertyRelative("MaskTarget");
            var arrowParam = element.FindPropertyRelative("ArrowParam");
            var isEmphasis = element.FindPropertyRelative("IsEmphasis");
            var advanceId = element.FindPropertyRelative("AdvanceId");
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
            EditorGUILayout.PropertyField(maskTarget);
            if (maskTarget.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(isEmphasis);
                EditorGUI.indentLevel--;
            }

            // 操作が必要な場合のパラメータ
            if ((TutorialAdvanceType)advanceType.enumValueIndex != TutorialAdvanceType.TapMessage)
            {
                EditorGUILayout.PropertyField(arrowParam);
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
    public override float OnGUIMain(Rect position, SerializedProperty property, GUIContent label)
    {
        var headerRect = position;
        headerRect.height = EditorGUIUtility.singleLineHeight;
        Rect contentRect = EditorGUI.PrefixLabel(headerRect, label);

        if (property.managedReferenceValue == null)
        {
            DrawNullState(contentRect, property);
        }
        else
        {
            DrawHeaderButtons(contentRect, property);
            position = DrawValueContents(position, contentRect, property, true);
        }
        return position.y;
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
    public override float OnGUIMain(Rect position, SerializedProperty property, GUIContent label)
    {
        var headerRect = position;
        headerRect.height = EditorGUIUtility.singleLineHeight;
        Rect contentRect = EditorGUI.PrefixLabel(headerRect, label);

        property.managedReferenceValue ??= new TutorialTarget();

        // キープロパティの入力 
        var targetIdProp = property.FindPropertyRelative(KeyPropertyName);
        position.y += EditorGUIUtility.singleLineHeight;
        position.x += IndentSize;
        position.width -= IndentSize;
        position = DrawProperty(targetIdProp, position);

        // キーが入力されていれば他のプロパティも表示
        if (!string.IsNullOrEmpty(targetIdProp.stringValue))
        {
            DrawValueContents(position, contentRect, property, true);
        }
        return position.y;
    }
}

public abstract class ReferenceDrawerBase<T> : PropertyDrawer where T : class, new()
{
    protected const float ButtonWidth = 65f;
    protected const float IndentSize = 15f;
    protected virtual List<string> PropertyNames => new List<string>();

    private float _cachedHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        _cachedHeight = OnGUIMain(position, property, label);
        EditorGUI.EndProperty();
    }

    public virtual float OnGUIMain(Rect position, SerializedProperty property, GUIContent label)
    {
        var headerRect = position;
        headerRect.height = EditorGUIUtility.singleLineHeight;

        Rect contentRect = EditorGUI.PrefixLabel(headerRect, label);
        position = DrawValueContents(position, contentRect, property, true);
        return position.y;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight;

        return _cachedHeight;
    }

    protected Rect BeginIndentedContent(Rect position)
    {
        position.y += EditorGUIUtility.singleLineHeight;
        position.x += IndentSize;
        position.width -= IndentSize;
        return position;
    }

    protected Rect DrawProperty(SerializedProperty property, Rect rect, bool isDraw = true)
    {
        float h = EditorGUI.GetPropertyHeight(property, true);
        rect.height = h;
        if (isDraw) EditorGUI.PropertyField(rect, property, true);
        rect.y += h;
        return rect;
    }

    /// <summary>
    /// 戻り値：この Property 全体の高さ
    /// </summary>
    protected Rect DrawValueContents(Rect position, Rect contentRect, SerializedProperty property, bool isDraw)
    {
        var props = new List<SerializedProperty>();
        foreach (var propName in PropertyNames)
        {
            props.Add(property.FindPropertyRelative(propName));
        }

        var rect = BeginIndentedContent(position);
        foreach (var prop in props)
        {
            rect = DrawProperty(prop, rect, isDraw);
        }
        return rect;
    }
}
