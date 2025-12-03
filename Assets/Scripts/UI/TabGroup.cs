using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;

public class TabGroup : MonoBehaviour
{
    [SerializeField] private List<TabButton> _tabButtonList;

    private Subject<int> _onTabSelected = new();
    public Observable<int> OnTabSelected => _onTabSelected;

    public void Init(int firstIndex = 0)
    {
        for (int i = 0; i < _tabButtonList.Count; i++)
        {
            int index = i;
            _tabButtonList[i].Init();
            _tabButtonList[i].OnClick.Subscribe(_ =>
            {
                SetActiveTab(index);
            }).AddTo(this);
        }
        SetActiveTab(firstIndex);
    }

    public void SetActiveTab(int index)
    {
        for (int i = 0; i < _tabButtonList.Count; i++)
        {
            _tabButtonList[i].SetActive(i == index);
        }
        _onTabSelected.OnNext(index);
    }

    public void SetTabText(int index, string text)
    {
        if (index < 0 || index >= _tabButtonList.Count) return;
        _tabButtonList[index].SetText(text);
    }

    void OnDestroy()
    {
        _onTabSelected.Dispose();
        _onTabSelected = null;
    }
}
