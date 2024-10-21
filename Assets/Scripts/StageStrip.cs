using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class StageStrip : MonoBehaviour
{
    [SerializeField] private List<Button> _levelButtonList;

    private int _stageId;
    public Subject<(int stageId, int level)> OnWhenClickLevelButton = new Subject<(int stageId, int level)>();

    public void Init(int stageId)
    {
        _stageId = stageId;
        for (int i = 0; i < _levelButtonList.Count; i++)
        {
            int level = i;
            _levelButtonList[i].OnClickAsObservable().Subscribe(_ =>
            {
                ClickLevelButton(level);
            });
        }
    }

    public void ClickLevelButton(int level)
    {
        OnWhenClickLevelButton.OnNext((_stageId, level));
    }
}
