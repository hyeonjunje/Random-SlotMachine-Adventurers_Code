using UnityEngine;
using UnityEngine.UI;

public class MapNodeUI : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] private RectTransform _rectTransform;

    [Header("시각적 요소 참조")]
    [SerializeField] private Image _nodeIcon; 
    [SerializeField] private Image _outline;  

    [Header("상태별 스프라이트 (임시)")]
    [SerializeField] private Sprite _availableSprite;
    [SerializeField] private Sprite _visitedSprite;
    [SerializeField] private Sprite _lockedSprite;

    [Header("타입별 스프라이트 (임시)")]
    [SerializeField] private Sprite[] _nodeIconSprites;

    public MapNode NodeData { get; private set; }
    public Vector2 RectPosition => _rectTransform.anchoredPosition;

    /// 이 View가 어떤 노드 데이터를 표시할지 초기화합니다.
    public void Init(MapNode nodeData, Vector2 position)
    {
        NodeData = nodeData;
        _rectTransform.anchoredPosition = position;

        UpdateVisualState();
    }

    /// 데이터의 현재 상태에 맞춰 시각적 요소를 갱신합니다.
    public void UpdateVisualState()
    {
        if (NodeData == null) return;

        if((int)NodeData.NodeType < _nodeIconSprites.Length)
        {
            _nodeIcon.sprite = _nodeIconSprites[(int)NodeData.NodeType];
        }

        switch (NodeData.NodeState)
        {
            case EMapNodeState.Available:
                _outline.sprite = _availableSprite;
                break;
            case EMapNodeState.Visited:
                _outline.sprite = _visitedSprite;
                break;
            case EMapNodeState.Locked:
            default:
                _outline.sprite = _lockedSprite;
                break;
        }
    }

    /// 이 노드가 클릭되었을 때 호출됩니다.
    public void OnNodeClicked()
    {
        EventBus.Publish(new StPlayerClickedNodeEvent(NodeData));
    }
}
