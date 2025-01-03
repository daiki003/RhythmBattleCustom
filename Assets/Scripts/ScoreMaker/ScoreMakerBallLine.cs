using System.Collections;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

public class ScoreMakerBallLine : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;

    private const float _scoreLineSpacing = 150f;
    private const float _scoreLineHeight = 15f;

    public void Init(ScoreMakerBall headBall, ScoreMakerBall lastBall)
    {
        transform.SetParent(headBall.transform.parent);
        transform.localScale = Vector3.one;
        headBall.AttachLine(this, lastBall, isLast: false);
        lastBall.AttachLine(this, headBall, isLast: true);

        // 線の下端をボールの位置に合わせる
        _rectTransform.anchoredPosition = new Vector2(headBall.transform.position.x, headBall.transform.position.y);
        // 線の長さをボール間の距離に合わせる
        var sd = GetComponent<RectTransform>().sizeDelta;
        sd.y = (_scoreLineSpacing + _scoreLineHeight) * Mathf.Abs(headBall.LineNumber - lastBall.LineNumber);
        GetComponent<RectTransform>().sizeDelta = sd;
    }
}
