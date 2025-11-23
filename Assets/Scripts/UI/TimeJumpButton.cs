using R3;
using UnityEngine;
using UnityEngine.UI;

public class TimeJumpButton : MonoBehaviour
{
    [SerializeField] private Button _mainButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private Image _numberIcon;

    private float _targetTimeRate = -1;
    private int _index;

    private const string _numberUIconPath = "Images/NumberButton/{0}";

    public Subject<float> OnClickMainButton = new();
    public Subject<int> OnClickRegisterButton = new();
    public Subject<int> OnClickDeleteButton = new();

    void Awake()
    {
        _mainButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickMainButton.OnNext(_targetTimeRate);
        }).AddTo(this);
        _registerButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickRegisterButton.OnNext(_index);
        }).AddTo(this);
        _deleteButton.OnClickAsObservable().Subscribe(_ =>
        {
            OnClickDeleteButton.OnNext(_index);
        }).AddTo(this);
    }

    public void Init(int index)
    {
        _index = index;
        var icon = Resources.Load<Sprite>(string.Format(_numberUIconPath, _index + 1));
        _numberIcon.sprite = icon;
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
