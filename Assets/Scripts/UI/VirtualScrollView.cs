using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using R3;

public class VirtualScrollView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private RectTransform _itemPrefab;

    [Header("Settings")]
    [SerializeField] private float _itemHeight;
    [SerializeField] private int _extraBuffer = 2;

    private List<IVirtualScrollItemData> _itemDataList = new();
    private readonly List<IVirtualScrollItem<IVirtualScrollItemData>> _items = new();
    private readonly List<RectTransform> _itemRects = new();
    private int _visibleItemCount;
    private float _viewportHeight;
    private int _totalItemCount;

    private Subject<IVirtualScrollItemSubscribeParam> _onOperateItem = new();
    public Observable<IVirtualScrollItemSubscribeParam> OnOperateItem => _onOperateItem;

    private void Start()
    {
        _scrollRect.onValueChanged.AddListener(_ => UpdateVisibleItems());
    }

    public void Initialize(List<IVirtualScrollItemData> dataList)
    {
        _viewportHeight = _scrollRect.viewport.rect.height;
        _visibleItemCount = Mathf.CeilToInt(_viewportHeight / _itemHeight) + _extraBuffer;
        UpdateItemDataList(dataList);

        // Item 生成（必要数だけ）
        for (int i = 0; i < _visibleItemCount; i++)
        {
            var item = Instantiate(_itemPrefab, _content);
            _itemRects.Add(item);
            var itemComponent = item.GetComponent<IVirtualScrollItem<IVirtualScrollItemData>>();
            itemComponent.RegisterForTutorial(i);
            itemComponent.OnOperateItem.Subscribe(param =>
            {
                _onOperateItem.OnNext(param);
            }).AddTo(this);
            _items.Add(itemComponent);
        }
        UpdateVisibleItems(force: true);
    }

    public void UpdateItemDataList(List<IVirtualScrollItemData> dataList)
    {
        _itemDataList = dataList;

        // Content は「論理サイズ」だけ設定
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, _itemDataList.Count * _itemHeight);
    }

    private void UpdateVisibleItems(bool force = false)
    {
        // 論理スクロール位置を計算
        float scrollY = _scrollRect.verticalNormalizedPosition * Mathf.Max(0, _totalItemCount * _itemHeight - _viewportHeight);

        int startIndex = Mathf.FloorToInt(scrollY / _itemHeight);
        startIndex = Mathf.Clamp(
            startIndex,
            0,
            Mathf.Max(0, _totalItemCount - _visibleItemCount)
        );

        for (int i = 0; i < _items.Count; i++)
        {
            int dataIndex = startIndex + i;
            var itemRect = _itemRects[i];

            if (dataIndex >= _totalItemCount)
            {
                itemRect.gameObject.SetActive(false);
                continue;
            }

            itemRect.gameObject.SetActive(true);
            itemRect.anchoredPosition = new Vector2(0f, -dataIndex * _itemHeight);

            _items[i].UpdateItem(_itemDataList[dataIndex]);
        }
    }

    private void UpdateItem(RectTransform item, int index)
    {
        // 例：Textを更新
        // item.GetComponentInChildren<Text>().text = index.ToString();

        // ここはあなたのデータ構造に合わせて実装
    }

    private void OnDestroy()
    {
        _scrollRect.onValueChanged.RemoveListener(_ => UpdateVisibleItems());
    }
}
