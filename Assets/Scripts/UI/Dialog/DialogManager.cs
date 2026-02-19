using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
    public const string CreditDialogPrefabName = "CreditDialog";

    public async UniTask<T2> ShowDialogAsync<T1, T2>(DialogOptionBase dialogOption) where T1 : DialogBase<T2> where T2 : DialogResultBase
    {
        var prefab = ResourceManager.LoadPrefab<T1>(_dialogPrefabPathBase + typeof(T1).Name);
        var dialog = Instantiate(prefab, DialogTransform);
        return await dialog.ShowAsync(dialogOption);
    }

    public void OpenSettingDialog()
    {
        ShowDialogAsync<SettingDialog, DialogResultBase>(
            new DialogOptionBase
            {
                TitleText = "設定",
                HideCancelButton  = true,
                HideOkButton  = true,
            }
        ).Forget();
    }

    public async UniTask<TutorialDialogResult> OpenTutorialDialogAsync(TutorialCommandList[] tutorialCommands)
    {
        return await ShowDialogAsync<TutorialDialog, TutorialDialogResult>(
            new TutorialDialogOption
            {
                CommandList = tutorialCommands,
                TitleText = "チュートリアル",
                HideCancelButton = true,
                HideOkButton = true,
            }
        );
    }

    // ライフ消費の確認ダイアログ
    public async UniTask<bool> ConfirmConsumeLifeDialogAsync(string title, string message, string buttonText, string shortageText)
    {
        // 無限ライフ購入後はライフ消費なしで進める
        if (SaveDataManager.IsInfiniteLife) return true;

        if (SaveDataManager.CurrentLife <= 0)
        {
            // ライフがないなら広告視聴誘導
            if (!await OpenPurchaseLifeDialogAsync(
                title: "ライフ不足",
                message: "ライフが足りません。\n広告を視聴して" + shortageText,
                isShortage: true
            ))
            {
                return false;
            }
            return true;
        }
        else
        {
            string currentLifeText = $"\n現在のライフ: {SaveDataManager.CurrentLife}";
            // ライフ消費確認
            var dialogResult = await ShowDialogAsync<MessageDialog, DialogResultBase>(new MessageDialogOption
            {
                TitleText = title,
                MessageText = message + currentLifeText,
                OkButtonText = buttonText,
                IsBgCancel = false,
            });
            if (dialogResult.ResultType != DialogResultType.Ok) return false;
            // ライフ消費までする
            await MasterManager.AddLife(-1);
        }
        return true;
    }

    public async UniTask<bool> OpenPurchaseLifeDialogAsync(string title, string message, bool isShortage)
    {
        bool isPurchased = false;
        var dialogResult = await ShowDialogAsync<MessageDialog, DialogResultBase>(new MessageDialogOption
        {
            TitleText = title,
            MessageText = message,
            OkButtonText = "視聴する",
            OkButton2Text = "無限ライフ\n(120円)",
            CancelButtonText = "キャンセル",
            HideOkButton2 = false,
            IsBgCancel = false,
        });

        switch (dialogResult.ResultType)
        {
            case DialogResultType.Ok:
                bool isRewardEarned = await AdsManager.ShowRewardAsync();
                if (isRewardEarned)
                {
                    // ライフ不足でダイアログを開いた場合、即消費するので獲得なしでtrueを返す
                    if (isShortage) return true;

                    await MasterManager.AddLife(1);
                    isPurchased = true;
                    await ShowDialogAsync<MessageDialog, DialogResultBase>(new MessageDialogOption
                    {
                        TitleText = "ライフ獲得",
                        MessageText = "ライフを1つ獲得しました。",
                        OkButtonText = "OK",
                        HideCancelButton = true
                    });
                }
                else
                {
                    await ShowDialogAsync<MessageDialog, DialogResultBase>(
                        new MessageDialogOption
                        {
                            TitleText = "エラー",
                            MessageText = "広告を視聴できませんでした。",
                            OkButtonText = "OK",
                            HideCancelButton = true,
                            IsBgCancel = false,
                        }
                    );
                }
                break;
            case DialogResultType.Ok2:
                // ライフ購入処理
                await MasterManager.PurchaseInfiniteLife();
                await ShowDialogAsync<MessageDialog, DialogResultBase>(new MessageDialogOption
                {
                    TitleText = "無限ライフ購入",
                    MessageText = "無限ライフを購入しました。",
                    OkButtonText = "OK",
                    HideCancelButton = true
                });
                isPurchased = true;
                break;
        }
        return isPurchased;
    }
}
