using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour
{
    [SerializeField] Button _closeButton;
    [SerializeField] InputField _changeMoveTimeInput;
    [SerializeField] Button _changeMoveTimeButton;

    public Subject<float> OnChangeMoveTime = new Subject<float>();

    void Start()
    {
        _closeButton.OnClickAsObservable().Subscribe(_ =>
        {
            gameObject.SetActive(false);
        }).AddTo(this);
        _changeMoveTimeButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (float.TryParse(_changeMoveTimeInput.text, out float time))
            {
                OnChangeMoveTime.OnNext(time);
            }
        }).AddTo(this);
    }
}
