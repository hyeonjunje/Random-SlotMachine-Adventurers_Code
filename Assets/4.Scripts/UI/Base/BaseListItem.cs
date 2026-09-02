using UnityEngine;

public abstract class BaseListItem<T> : MonoBehaviour
{
    public T Item { get; private set; }

    public virtual void SetListItem(T item)
    {
        Item = item;
    }
}
