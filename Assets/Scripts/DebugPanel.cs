using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour
{
    [SerializeField] InputField _changeMoveTimeInput;
    [SerializeField] Button _changeMoveTimeButton;
    [SerializeField] InputField _changeLevelInput;
    [SerializeField] Button _changeLevelButton;

    void Start()
    {
        _changeMoveTimeButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (float.TryParse(_changeMoveTimeInput.text, out float time))
            {
                ChangeMoveTime(time);
            }
        });
        _changeLevelButton.OnClickAsObservable().Subscribe(_ =>
        {
            if (int.TryParse(_changeLevelInput.text, out int level))
            {
                ChangeLevel(level);
            }
        });
    }

    private void ChangeMoveTime(float time)
    {
        GameManager.instance.MoveTime(time);
        gameObject.SetActive(false);
    }

    private void ChangeLevel(int level)
    {
        GameManager.instance.ChangeLevel(level);
        gameObject.SetActive(false);
    }
}
