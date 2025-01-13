using System.Collections;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

public class ScoreMakerBallLine : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;

    private ScoreMakerBall _headBall;
    private ScoreMakerBall _lastBall;

    public Subject<ScoreMakerBallLine> OnWhenDestroyed = new();

    private const float _scoreLineHeight = 15f;

    public void Init(ScoreMakerBall headBall, ScoreMakerBall lastBall, float lineSpacing)
    {
        _headBall = headBall;
        _lastBall = lastBall;
        transform.SetParent(_headBall.transform.parent);
        transform.localScale = Vector3.one;
        _headBall.AttachLine(this, _lastBall, isLast: false);
        _lastBall.AttachLine(this, _headBall, isLast: true);

        UpdateLineSpacing(lineSpacing);
    }

    public void UpdateLineSpacing(float spacing)
    {
        // 線の下端をボールの位置に合わせる
        _rectTransform.anchoredPosition = Vector2.zero;
        // new Vector2(_headBall.transform.position.x, _headBall.transform.position.y);
        // 線の長さをボール間の距離に合わせる
        var sd = GetComponent<RectTransform>().sizeDelta;
        sd.y = (spacing + _scoreLineHeight) * Mathf.Abs(_headBall.LineNumber - _lastBall.LineNumber);
        GetComponent<RectTransform>().sizeDelta = sd;
    }

    void OnDestroy()
    {
        OnWhenDestroyed.OnNext(this);
    }
}
