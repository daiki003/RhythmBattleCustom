using R3;
using UnityEngine;
using UnityEngine.UI;

public class TimeJumpButton : MonoBehaviour
{
    [SerializeField] private Button _mainButton;
    [SerializeField] private Button _registerButton;

    private float _targetTimeRate = -1;

    public Subject<float> OnClickMainButton = new();
    public Subject<Unit> OnClickRegisterButton = new();

    void Awake()
    {
        _mainButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickMainButton.OnNext(_targetTimeRate);
        }).AddTo(this);
        _registerButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickRegisterButton.OnNext(default);
        }).AddTo(this);
    }

    public void SetTimeRate(float timeRate)
    {
        _targetTimeRate = timeRate;
    }

    void Update()
    {
        _mainButton.interactable = _targetTimeRate >= 0;
    }
}
