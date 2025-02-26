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

    public T CreateDialog<T>(string prefabPath, DialogOptionBase dialogOption) where T : DialogBase
    {
        var prefab = ResourceManager.LoadPrefab<T>(prefabPath);
        var dialog = Instantiate(prefab, DialogTransform);
        dialog.Init(dialogOption);
        return dialog;
    }
}
