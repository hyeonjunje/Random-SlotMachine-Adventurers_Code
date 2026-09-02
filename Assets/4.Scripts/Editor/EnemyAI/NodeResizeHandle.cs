#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 노드의 오른쪽 테두리를 드래그해서 너비를 조절하는 헬퍼.
/// MouseManipulator가 아닌 직접 콜백 방식으로 동작.
/// </summary>
public class NodeResizeHandle
{
    private bool _isResizing;
    private float _startMouseX;
    private float _startWidth;
    private const float HANDLE_WIDTH = 8f;
    private const float MIN_WIDTH = 200f;
    private const float MAX_WIDTH = 800f;

    private VisualElement _resizeHandle;
    private VisualElement _targetNode;
    private Action<float> _onResize;

    public NodeResizeHandle(VisualElement targetNode, Action<float> onResize)
    {
        _targetNode = targetNode;
        _onResize = onResize;

        // 리사이즈 핸들 (오른쪽 테두리)
        _resizeHandle = new VisualElement();
        _resizeHandle.style.position = Position.Absolute;
        _resizeHandle.style.right = 0;
        _resizeHandle.style.top = 0;
        _resizeHandle.style.bottom = 0;
        _resizeHandle.style.width = HANDLE_WIDTH;
        _resizeHandle.style.backgroundColor = new Color(0, 0, 0, 0);
        _resizeHandle.AddToClassList("resize-handle");
        _resizeHandle.pickingMode = PickingMode.Position;

        targetNode.Add(_resizeHandle);

        _resizeHandle.RegisterCallback<MouseDownEvent>(OnMouseDown);
        _resizeHandle.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _resizeHandle.RegisterCallback<MouseUpEvent>(OnMouseUp);
        _resizeHandle.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        _resizeHandle.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        _resizeHandle.style.backgroundColor = new Color(0.4f, 0.6f, 1f, 0.3f);
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        if (!_isResizing)
        {
            _resizeHandle.style.backgroundColor = new Color(0, 0, 0, 0);
        }
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0)
        {
            _isResizing = true;
            _startMouseX = evt.mousePosition.x;
            _startWidth = _targetNode.resolvedStyle.width;
            _resizeHandle.style.backgroundColor = new Color(0.4f, 0.6f, 1f, 0.5f);
            _resizeHandle.CaptureMouse();
            evt.StopPropagation();
        }
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (_isResizing)
        {
            float delta = evt.mousePosition.x - _startMouseX;
            float newWidth = Mathf.Clamp(_startWidth + delta, MIN_WIDTH, MAX_WIDTH);
            _targetNode.style.width = newWidth;
            _onResize?.Invoke(newWidth);
            evt.StopPropagation();
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (_isResizing && evt.button == 0)
        {
            _isResizing = false;
            _resizeHandle.style.backgroundColor = new Color(0, 0, 0, 0);
            if (_resizeHandle.HasMouseCapture())
                _resizeHandle.ReleaseMouse();
            evt.StopPropagation();
        }
    }
}
#endif
