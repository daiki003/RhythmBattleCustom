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

    private List<GameObject> _stripList = new List<GameObject>();
    private Color32 _activeButtonColor = new Color32(255, 255, 255, 255);
    private Color32 _nonActiveButtonColor = new Color32(255, 255, 255, 140);

    public void Init()
    {
        RecreateStrip();
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

    public void RecreateStrip()
    {
        DestroyAllStrip();
        for (int i = 0; i < MasterManager.StageMasterList.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var strip = Instantiate(_stageStripPrefab, _stripTransformList[j]);
                _stripList.Add(strip.gameObject);
                strip.Init(MasterManager.StageMasterList[i].StageId, j);
            }
        }
    }

    public void DestroyAllStrip()
    {
        while (_stripList.Count > 0)
        {
            var gameObject = _stripList[0];
            _stripList.RemoveAt(0);
            Destroy(gameObject);
        }
    }

    private void SetLevelPanel(int level)
    {
        for (int i = 0; i < _levelPanelList.Count; i++)
        {
            bool isActive = level == i;
            _levelPanelList[i].gameObject.SetActive(isActive);
            var buttonImage = _levelButtonList[i].GetComponent<Image>();
            buttonImage.color = isActive ? _activeButtonColor : _nonActiveButtonColor;
        }
    }
}
