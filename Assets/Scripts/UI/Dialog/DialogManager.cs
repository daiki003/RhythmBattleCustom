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

    private const string _dialogPrefabPathBase = "UI/Dialog/";
    public const string MessageDialogPrefabName = "MessageDialog";
    public const string CreditDialogPrefabName = "CreditDialog";
    public const string InputDialogPrefabName = "InputDialog";
    public const string SettingDialogPrefabName = "SettingDialog";
    public const string HelpDialogPrefabName = "HelpDialog";
    public const string NewCreateListDialogPrefabName = "NewCreateListDialog";
    public const string StageDuplicateDialogPrefabName = "StageDuplicateDialog";

    public T CreateDialog<T>(string prefabName, DialogOptionBase dialogOption) where T : DialogBase
    {
        var prefab = ResourceManager.LoadPrefab<T>(_dialogPrefabPathBase + prefabName);
        var dialog = Instantiate(prefab, DialogTransform);
        dialog.Init(dialogOption);
        return dialog;
    }

    public void OpenSettingDialog()
    {
        CreateDialog<SettingDialog>(
            SettingDialogPrefabName,
            new DialogOptionBase
            {
                TitleText = "設定",
                HideCancelButton  = true,
                HideOkButton  = true,
            }
        );
    }

    public void OpenHelpDialog(HelpDialogPageType pageType)
    {
        CreateDialog<HelpDialog>(
            HelpDialogPrefabName,
            new HelpDialogOption
            {
                TitleText = "Tips",
                OkButtonText = "次へ",
                CancelButtonText = "前へ",
                UseYellowCancelButton = true,
                OkButtonSeType = ButtonSeType.ChangePage,
                CancelButtonSeType = ButtonSeType.ChangePage,
                FirstPageType = pageType
            }
        );
    }
}
