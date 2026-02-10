using System.Collections;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class SelectMaskParam
{
    public int StartIndex;
    public int EndIndex;
}

public class ScoreMakerLinePanel : MonoBehaviour
{
    // [Header("References")]
    // [SerializeField] private ScrollRect _scrollRect;
    // [SerializeField] private RectTransform _content;
    // [SerializeField] private RectTransform _itemPrefab;
    // [SerializeField] private GameObject _selectMask;

    // [Header("Settings")]
    // [SerializeField] private float _itemHeight;
    // [SerializeField] private int _extraBuffer = 2;

    // private List<ScoreLineData> _lineDataList = new();
    // private readonly List<ScoreLine> _scoreLineList = new();
    // private readonly List<RectTransform> _lineRects = new();
    // private int _visibleItemCount;
    // private float _viewportHeight;
    // private int _totalItemCount;
    // private SelectMaskParam _selectMaskParam;

    // private Subject<IVirtualScrollItemSubscribeParam> _onOperateItem = new();
    // public Observable<IVirtualScrollItemSubscribeParam> OnOperateItem => _onOperateItem;

    // public float NormalizedPosition => _scrollRect.verticalNormalizedPosition;
    // public float CurrentLineNumber => _scrollRect.verticalNormalizedPosition * _totalItemCount;

    // private void Start()
    // {
    //     _scrollRect.onValueChanged.AddListener(_ => UpdateVisibleItems());
    // }

    // public void Initialize(List<ScoreLineData> dataList)
    // {
    //     _viewportHeight = _scrollRect.viewport.rect.height;
    //     _visibleItemCount = Mathf.CeilToInt(_viewportHeight / _itemHeight) + _extraBuffer;
    //     UpdateItemDataList(dataList);

    //     // Item 生成（必要数だけ）
    //     for (int i = 0; i < _visibleItemCount; i++)
    //     {
    //         var item = Instantiate(_itemPrefab, _content);
    //         _lineRects.Add(item);
    //         var itemComponent = item.GetComponent<ScoreLine>();
    //         itemComponent.RegisterForTutorial(i);
    //         itemComponent.OnOperateItem.Subscribe(param =>
    //         {
    //             _onOperateItem.OnNext(param);
    //         }).AddTo(this);
    //         _scoreLineList.Add(itemComponent);
    //     }
    //     UpdateVisibleItems(force: true);
    // }

    // public void UpdateItemDataList(List<ScoreLineData> dataList)
    // {
    //     _lineDataList = dataList;

    //     // Content は「論理サイズ」だけ設定
    //     _content.sizeDelta = new Vector2(_content.sizeDelta.x, _lineDataList.Count * _itemHeight);
    // }

    // private void UpdateVisibleItems(bool force = false)
    // {
    //     // 論理スクロール位置を計算
    //     float scrollY = _scrollRect.verticalNormalizedPosition * Mathf.Max(0, _totalItemCount * _itemHeight - _viewportHeight);

    //     int startIndex = Mathf.FloorToInt(scrollY / _itemHeight);
    //     startIndex = Mathf.Clamp(
    //         startIndex,
    //         0,
    //         Mathf.Max(0, _totalItemCount - _visibleItemCount)
    //     );

    //     for (int i = 0; i < _scoreLineList.Count; i++)
    //     {
    //         int dataIndex = startIndex + i;
    //         var itemRect = _lineRects[i];

    //         if (dataIndex >= _totalItemCount)
    //         {
    //             itemRect.gameObject.SetActive(false);
    //             continue;
    //         }

    //         itemRect.gameObject.SetActive(true);
    //         itemRect.anchoredPosition = new Vector2(0f, dataIndex * _itemHeight);

    //         _scoreLineList[i].UpdateItem(_lineDataList[dataIndex]);
    //     }
    // }

    // public void SetNormalizedPosition(float normalizedPosition)
    // {
    //     _scrollRect.verticalNormalizedPosition = normalizedPosition;
    //     UpdateVisibleItems(force: true);
    // }

    // public void SetNormalizedPositionByLineNumber(float lineNumber)
    // {
    //     _scrollRect.verticalNormalizedPosition = lineNumber / _totalItemCount;
    //     UpdateVisibleItems(force: true);
    // }

    // public ScoreLine GetNextLine()
    // {
    //     return _scoreLineList.FirstOrDefault(l => !l.IsEnd);
    // }

    // public void SetSelectMaskParam(SelectMaskParam param)
    // {
    //     _selectMaskParam = param;
    // }

    // public void SetSelectMask(int startIndex)
    // {
    //     if (_selectMaskParam == null)
    //     {
    //         _selectMask.SetActive(false);
    //         return;
    //     }
    //     _selectMask.SetActive(true);
    //     var firstLineNumber = _selectedLineList.Min(l => l.LineNumber);
    //     var lastLineNumber = _selectedLineList.Max(l => l.LineNumber);
    //     float lineSpace = _scoreAreaLayoutGroup.spacing + _scoreLineHeight;
    //     _selectMask.transform.SetOffsetMinY(_scoreAreaLayoutGroup.padding.bottom - _selectMaskOffset + lineSpace * firstLineNumber);
    //     _selectMask.transform.SetOffsetMaxY(_scoreAreaLayoutGroup.padding.top - _selectMaskOffset + lineSpace * (_scoreLineList.Count - lastLineNumber - 1));
    // }

    // private void UpdateItem(RectTransform item, int index)
    // {
    //     // 例：Textを更新
    //     // item.GetComponentInChildren<Text>().text = index.ToString();

    //     // ここはあなたのデータ構造に合わせて実装
    // }

    // private void OnDestroy()
    // {
    //     _scrollRect.onValueChanged.RemoveListener(_ => UpdateVisibleItems());
    // }
}
