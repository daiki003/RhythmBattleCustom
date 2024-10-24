using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R3;
using UnityEngine.UI;
using System.Linq;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private StageStrip _stageStripPrefab;
    [SerializeField] private List<Button> _levelButtonList;
    [SerializeField] private List<GameObject> _levelPanelList;
    [SerializeField] private List<Transform> _stripTransformList;


    void Start()
    {

    }

    public void Init()
    {
        for (int i = 0; i < 4; i++)
        {
            var strip = Instantiate(_stageStripPrefab, _stripTransformList[i]);
            strip.Init(1, i);
        }
        for (int i = 0; i < _levelButtonList.Count; i++)
        {
            int level = i;
            _levelButtonList[i].OnClickAsObservable().Subscribe(_ =>
            {
                SEManager.instance.PlayBeatSe();
                SetLevelPanel(level);
            });
        }
        SetLevelPanel(0);
    }

    private void SetLevelPanel(int level)
    {
        for (int i = 0; i < _levelPanelList.Count; i++)
        {
            _levelPanelList[i].gameObject.SetActive(level == i);
        }
    }
}
