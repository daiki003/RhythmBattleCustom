using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class LongBall : MonoBehaviour
{
    [SerializeField] private Image _ballImage;
    [SerializeField] private Ball _startBall;
    [SerializeField] private Ball _endBall;
    [SerializeField] private LineRenderer _lineRenderer;
    public Ball StartBall => _startBall;
    public Ball EndBall => _endBall;
    public LineRenderer LineRenderer => _lineRenderer;

    public bool IsEndBallLaunched;
    public bool IsStartBallClicked;

    private const float _lineWidth = 0.2f;

    public Subject<LongBall> OnWhenDestroyed = new Subject<LongBall>();

    public void Init(bool isleft, float criticalTime, float endCriticalTime, CancellationTokenSource cts)
    {
        _startBall.Init(isleft, criticalTime, BallType.LongStart, cts);
        _endBall.Init(isleft, endCriticalTime, BallType.LongEnd, cts);
        _startBall.OnWhenClicked.Subscribe(_ =>
        {
            IsStartBallClicked = true;
        });
        _endBall.OnWhenDestroy.Subscribe(_ =>
        {
            Destroy(gameObject);
        });

        //線の幅を決める
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;

        //頂点の数を決める
        _lineRenderer.positionCount = 2;
    }

    void Update()
    {
        _lineRenderer.SetPosition(0, StartBall.transform.position);
        _lineRenderer.SetPosition(1, EndBall.transform.position);
    }

    void OnDestroy()
    {
        if (_startBall != null)
        {
            Destroy(_startBall.gameObject);
        }
        if (_endBall != null)
        {
            Destroy(_endBall.gameObject);
        }
        OnWhenDestroyed.OnNext(this);
    }
}
