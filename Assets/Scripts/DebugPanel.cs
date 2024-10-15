using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour
{
    [SerializeField] InputField _changeMoveTimeInput;
    [SerializeField] Button _changeMoveTimeButton;

    void Start()
    {
        _changeMoveTimeButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (float.TryParse(_changeMoveTimeInput.text, out float time))
            {
                ChangeMoveTime(time);
            }
        });
    }

    private void ChangeMoveTime(float time)
    {
        GameManager.instance.MoveTime(time);
        gameObject.SetActive(false);
    }
}
