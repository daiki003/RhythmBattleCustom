using System.Collections;
using System.Collections.Generic;
using R3;
using UnityEngine;

public interface IVirtualScrollItem<T> where T : IVirtualScrollItemData
{
    void UpdateItem(T data);
    void RegisterForTutorial(int number);
    Observable<IVirtualScrollItemSubscribeParam> OnOperateItem { get; }
}

public interface IVirtualScrollItemData
{

}

public interface IVirtualScrollItemSubscribeParam { }

