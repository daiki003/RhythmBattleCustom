using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using R3;

public class GameManager : MonoBehaviour
{
    [SerializeField] private RectTransform _leftButton;
    [SerializeField] private RectTransform _rightTransform;
    [SerializeField] private RectTransform _startTransform;

    [SerializeField] private Ball _ballPrefab;

    [SerializeField] private Text _countText;

    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioClip _beatSe;

    private List<Ball> _leftBallList = new List<Ball>();
    private List<Ball> _rightBallList = new List<Ball>();
    private int _count = 0;
    private const int _ballCount = 100;

    private ClickHandler _clickHandler;

    void Start()
    {
        _clickHandler = new ClickHandler();
        _clickHandler.OnClickLeftButton.Subscribe(_ =>
        {
            OnClickLeftButton();
        });
        _clickHandler.OnClickRightButton.Subscribe(_ =>
        {
            OnClickRightButton();
        });
        CreateBall().Forget();
    }

    void Update()
    {
        _clickHandler.Update();
    }

    private async UniTask CreateBall()
    {
        int ballCount = 0;
        while (ballCount < _ballCount)
        {
            var newBall = Instantiate(_ballPrefab, _startTransform.parent);
            var cts = new CancellationTokenSource();  
            bool isLeft = Random.Range(0, 2) == 0;
            newBall.Init(isLeft, cts);
            if (isLeft)
            {
                _leftBallList.Add(newBall);
            }
            else
            {
                _rightBallList.Add(newBall);
            }
            _ = newBall.transform.DOMove(newBall.IsLeft ? _leftButton.transform.position : _rightTransform.transform.position, 2f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                if (newBall.IsLeft)
                {
                    _leftBallList.Remove(newBall);
                }
                else
                {
                    _rightBallList.Remove(newBall);
                }
                Destroy(newBall.gameObject);
            }).ToUniTask();
            await UniTask.WaitForSeconds(1);
            ballCount++;
        }
    }

    private void OnClickLeftButton()
    {
        var firstActiveBall = _leftBallList.FirstOrDefault(b => b.IsActive);
        if (firstActiveBall != null)
        {
            _count++;
            _countText.text = _count.ToString();
            _seSource.PlayOneShot(_beatSe);
            firstActiveBall.Cts.Cancel();
            Destroy(firstActiveBall.gameObject);
        }
    }

    private void OnClickRightButton()
    {
        var firstActiveBall = _rightBallList.FirstOrDefault(b => b.IsActive);
        if (firstActiveBall != null)
        {
            _count++;
            _countText.text = _count.ToString();
            _seSource.PlayOneShot(_beatSe);
            firstActiveBall.Cts.Cancel();
            Destroy(firstActiveBall.gameObject);
        }
    }
}
