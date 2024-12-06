using System.Collections;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

public class ScoreMakerBallLine : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    private ScoreMakerBall _headBall;
    private ScoreMakerBall _lastBall;

    private const float _lineWidth = 0.2f;

    public void Init(ScoreMakerBall headBall, ScoreMakerBall lastBall)
    {
        //線の幅を決める
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;

        //頂点の数を決める
        _lineRenderer.positionCount = 2;

        _headBall = headBall;
        _lastBall = lastBall;
        _headBall.AttachLine(this, _lastBall, isLast: false);
        _lastBall.AttachLine(this, _headBall, isLast: true);
        _lineRenderer.SetPosition(0, _headBall.transform.position);
        _lineRenderer.SetPosition(1, _lastBall.transform.position);
    }

    void Update()
    {
        if (_headBall != null && _lastBall != null)
        {
            _lineRenderer.SetPosition(0, _headBall.transform.position);
            _lineRenderer.SetPosition(1, _lastBall.transform.position);
        }
    }
}
