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
    [SerializeField] private SingleBall _startBall;
    [SerializeField] private SingleBall _endBall;
    [SerializeField] private LineRenderer _lineRenderer;
    public SingleBall StartBall => _startBall;
    public SingleBall EndBall => _endBall;
    public LineRenderer LineRenderer => _lineRenderer;

    public bool IsEndBallLaunched;
    public bool IsStartBallClicked;

    private const float _lineWidth = 0.2f;

    public void Init(NoteMaster noteMaster, float criticalTime, float endCriticalTime)
    {
        _startBall.Init(noteMaster, criticalTime, BallType.LongStart);
        _endBall.Init(noteMaster.notes[0], endCriticalTime, BallType.LongEnd);
        _startBall.OnWhenLaunched.Subscribe(_ =>
        {
            gameObject.SetActive(true);
            IsStartBallClicked = true;
        });
        _startBall.OnWhenClicked.Subscribe(_ =>
        {
            IsStartBallClicked = true;
        });
        _startBall.OnWhenDestroyed.Subscribe(_ =>
        {
            Destroy(gameObject);
        });
        _endBall.OnWhenDestroyed.Subscribe(_ =>
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
        if (StartBall != null && EndBall != null)
        {
            _lineRenderer.SetPosition(0, StartBall.transform.position);
            _lineRenderer.SetPosition(1, EndBall.transform.position);
        }
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
    }
}
