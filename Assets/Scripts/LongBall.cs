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
    [SerializeField] private SingleBall _startBall;
    [SerializeField] private SingleBall _endBall;
    [SerializeField] private LineRenderer _lineRenderer;
    public SingleBall StartBall => _startBall;
    public SingleBall EndBall => _endBall;

    private const float _lineWidth = 0.2f;

    public void Init(NoteMaster noteMaster, float criticalTime, float endCriticalTime, float ballTimeOffset)
    {
        _startBall.Init(noteMaster, criticalTime, BallType.LongStart, ballTimeOffset);
        _endBall.Init(noteMaster.notes[0], endCriticalTime, BallType.LongEnd, ballTimeOffset);
        _startBall.OnWhenSetBallState.Subscribe(state =>
        {
            switch (state)
            {
                case BallState.Wait:
                    gameObject.SetActive(false);
                    break;
                case BallState.Launched:
                    SetLinePosition();
                    gameObject.SetActive(true);
                    _endBall.gameObject.SetActive(true);
                    break;
                case BallState.End:
                    _endBall.SetBallState(BallState.End);
                    gameObject.SetActive(false);
                    break;
            }
        }).AddTo(this);
        _endBall.OnWhenSetBallState.Subscribe(state =>
        {
            switch (state)
            {
                case BallState.Launched:
                    gameObject.SetActive(true);
                    break;
                case BallState.End:
                    _startBall.SetBallState(BallState.End);
                    gameObject.SetActive(false);
                    break;
            }
        }).AddTo(this);
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
            SetLinePosition();
        }
    }

    private void SetLinePosition()
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
    }
}
