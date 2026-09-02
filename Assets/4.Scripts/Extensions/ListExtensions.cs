using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    public static bool TryDraw<T>(this List<T> list, out T value)
    {
        if (list == null || list.Count == 0)
        {
            value = default;
            return false;
        }

        int idx = Random.Range (0, list.Count);
        value = list[idx];

        int last = list.Count - 1;
        list[idx] = list[last];
        list.RemoveAt (last);

        return true;
    }
}
