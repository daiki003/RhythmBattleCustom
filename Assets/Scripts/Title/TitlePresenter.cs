using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class TitlePresenter : MonoBehaviour
{
    [SerializeField] private Button _bgButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Text _nextText;

    private Tweener _blinkingTween;
    private const float _blinkingTime = 0.8f;

    public void Init()
    {
        _bgButton.OnClickAsObservable().Subscribe(_ =>
        {
            GameManager.instance.OpenScene(SceneType.Home, new HomeSceneInfo()).Forget();
        }).AddTo(this);
        _settingButton.OnClickAsObservable().Subscribe(_ =>
        {
            DialogManager.instance.OpenSettingDialog();
        }).AddTo(this);
        _blinkingTween = DOTween.ToAlpha(() => _nextText.color, color => _nextText.color = color, 0.0f, _blinkingTime).SetEase(Ease.InQuad).SetLoops(-1, LoopType.Yoyo);

        BGMManager.instance.SetClip(BgmName.Title, isLoop: true, isFade: true);
    }

    private void OnDestroy()
    {
        _blinkingTween?.Kill();
    }
}
