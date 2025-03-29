using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class ScoreStars : MonoBehaviour
{
    [SerializeField] private GameObject _blackStar;
    [SerializeField] private GameObject _firstStar;
    [SerializeField] private GameObject _secondStars;
    [SerializeField] private GameObject _thirdStars;

    public enum StarState
    {
        None,
        First,
        Second,
        Third
    }

    public void UpdteStar(ClearState clearState)
    {
        var starState = GetStarState(clearState);
        _blackStar.SetActive(starState == StarState.None);
        _firstStar.SetActive(starState == StarState.First);
        _secondStars.SetActive(starState == StarState.Second);
        _thirdStars.SetActive(starState == StarState.Third);
    }

    private StarState GetStarState(ClearState clearState)
    {
        // スコアが100であれば3つ星
        if (clearState.Score >= 100f)
        {
            return StarState.Third;
        }
        // ミスが1つもなければ2つ星
        if (clearState.Score >= 80f && clearState.MissNumber == 0)
        {
            return StarState.Second;
        }
        // スコアが80以上なら1つ星
        if (clearState.Score >= 80f)
        {
            return StarState.First;
        }
        return StarState.None;
    }
}
