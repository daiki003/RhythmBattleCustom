using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogManager : MonoBehaviour
{
    public Transform DialogTransform;

    public static DialogManager instance;
    public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

    public const string MessageDialogPrefabPath = "UI/MessageDialog";
    public const string StageDuplicateDialogPrefabPath = "ScoreMaker/StageDuplicateDialog";

    public T CreateDialog<T>(string prefabPath, DialogOptionBase dialogOption) where T : DialogBase
    {
        var prefab = ResourceManager.LoadPrefab<T>(prefabPath);
        var dialog = Instantiate(prefab, DialogTransform);
        dialog.Init(dialogOption);
        return dialog;
    }

    public void OpenSettingDialog()
    {
        CreateDialog<SettingDialog>(
            "UI/SettingDialog",
            new DialogOptionBase
            {
                TitleText = "設定",
                HideCancelButton  = true,
                HideOkButton  = true,
            }
        );
    }
}
