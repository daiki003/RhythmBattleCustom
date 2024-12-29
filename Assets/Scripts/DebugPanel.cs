using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour
{
    [SerializeField] InputField _changeMoveTimeInput;
    [SerializeField] Button _changeMoveTimeButton;

    public Subject<float> OnChangeMoveTime = new Subject<float>();

    void Start()
    {
        _changeMoveTimeButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (float.TryParse(_changeMoveTimeInput.text, out float time))
            {
                OnChangeMoveTime.OnNext(time);
            }
        });
    }
}
