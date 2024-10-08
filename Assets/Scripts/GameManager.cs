using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using R3;
using System;

public class GameManager : MonoBehaviour
{
    [SerializeField] private RectTransform _leftButton;
    [SerializeField] private RectTransform _rightTransform;
    [SerializeField] private RectTransform _startTransform;

    [SerializeField] private Ball _ballPrefab;

    [SerializeField] private Text _countText;

    [SerializeField] private AudioSource _seSource;
    [SerializeField] private AudioClip _beatSe;
    [SerializeField] private AudioSource _bgmSource;

    private List<Ball> _leftBallList = new List<Ball>();
    private List<Ball> _rightBallList = new List<Ball>();
    private int _count = 0;
    private const int _ballCount = 300;

    private List<float> _noteList = new List<float>()
    {
        8f, 12f, 16f, 20f, 24f, 28f, 32f, 36f, 40f, 44f, 48f, 52f, 
        52f, 56f, 60f, 64f, 68f, 72f, 76f, 80f, 84f, 88f, 92f, 96f,
        100f, 104f, 108f, 112f, 116f, 120f, 124f, 128f, 132f, 136f, 140f, 144f,
    };
    private const float _noteTimeOffset = 0.5f;
    private const float _noteTimeBuffer = 0.04f;
    private const float _ballTimeOffset = 1.68f;

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
        _bgmSource.Play();
    }

    void Update()
    {
        if (_noteList.Count > 0)
        {
            float noteTime = (_noteList[0] + _noteTimeOffset) * (60f / 130f);
            if (_bgmSource.time >= noteTime - _ballTimeOffset)
            {
                var newBall = Instantiate(_ballPrefab, _startTransform.parent);
                var cts = new CancellationTokenSource();  
                bool isLeft = UnityEngine.Random.Range(0, 2) == 0;
                newBall.Init(isLeft, noteTime, cts);
                _noteList.RemoveAt(0);
                if (isLeft)
                {
                    _leftBallList.Add(newBall);
                }
                else
                {
                    _rightBallList.Add(newBall);
                }

                newBall.transform.DOMove(newBall.IsLeft ? _leftButton.transform.position : _rightTransform.transform.position, 2f).SetEase(Ease.InQuad).OnComplete(() =>
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
                });
            }
        }
        _clickHandler.Update();
    }

    private void OnClickLeftButton()
    {
        var firstActiveBall = _leftBallList.FirstOrDefault(b => b.CriticalTime > _bgmSource.time - _noteTimeBuffer && b.CriticalTime < _bgmSource.time + _noteTimeBuffer);
        if (firstActiveBall != null)
        {
            _count++;
            _countText.text = _count.ToString();
            _seSource.PlayOneShot(_beatSe);
            firstActiveBall.Cts.Cancel();
            _leftBallList.Remove(firstActiveBall);
            Destroy(firstActiveBall.gameObject);
        }
    }

    private void OnClickRightButton()
    {
        var firstActiveBall = _rightBallList.FirstOrDefault(b => b.CriticalTime > _bgmSource.time - _noteTimeBuffer && b.CriticalTime < _bgmSource.time + _noteTimeBuffer);
        if (firstActiveBall != null)
        {
            _count++;
            _countText.text = _count.ToString();
            _seSource.PlayOneShot(_beatSe);
            firstActiveBall.Cts.Cancel();
            _rightBallList.Remove(firstActiveBall);
            Destroy(firstActiveBall.gameObject);
        }
    }
}
