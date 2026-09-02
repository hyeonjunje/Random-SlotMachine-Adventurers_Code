using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SlotInteractor : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public object Slot { get; private set; }

    private Action<object, PointerEventData> _onBeginDragSlot;
    private Action<object, PointerEventData> _onDragSlot;
    public Action<object, PointerEventData> _onEndDragSlot;

    public virtual void SetInteractor(object slot, Action<object, PointerEventData> onBegineDragSlot, Action<object, PointerEventData> onDragSlot, Action<object, PointerEventData> onEndDragSlot)
    {
        Slot = slot;
        _onBeginDragSlot = onBegineDragSlot;
        _onDragSlot = onDragSlot;
        _onEndDragSlot = onEndDragSlot;
    }

    public virtual void Release()
    {
        Slot = null;
        _onBeginDragSlot = null;
        _onDragSlot = null;
        _onEndDragSlot = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        _onBeginDragSlot?.Invoke(Slot, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        _onDragSlot?.Invoke(Slot, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        _onEndDragSlot?.Invoke(Slot, eventData);
    }
}
