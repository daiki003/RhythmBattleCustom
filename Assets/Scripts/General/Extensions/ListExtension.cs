using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ListExtension
{
    /// <summary>
    /// 最後の要素を取り出して削除
    /// </summary>
    public static T Pop<T>(this List<T> list)
    {
        var ret = list.LastOrDefault();
        if (ret != null)
        {
            list.Remove(ret);
        }
        return ret;
    }
}
