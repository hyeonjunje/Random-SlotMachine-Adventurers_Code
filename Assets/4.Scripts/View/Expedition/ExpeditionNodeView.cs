using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpriteRenderer 기반 노드 뷰 (UI가 아닌 월드 스페이스)
/// </summary>
public class ExpeditionNodeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [SerializeField] private Transform _trGuideParent;

    [Header("섬 애니메이션 수치")]
    [SerializeField] private float _highlightScale = 1.2f;
    [SerializeField] private float _highlightDuration = 0.1f;
    [SerializeField] private float _activateScale = 1.05f;
    [SerializeField] private float _activateDuration = 2f;

    public MapNode NodeData { get; private set; }
    public Vector3 Position => transform.position;

    private List<ExpeditionNodeView> _prevNodeViews = new List<ExpeditionNodeView>();

    private Tweener _activateTween = null;

    /// <summary>
    /// 노드 데이터와 위치로 초기화
    /// </summary>
    public void Init(MapNode nodeData, Vector3 worldPosition)
    {
        _prevNodeViews.Clear();

        NodeData = nodeData;
        transform.position = worldPosition;
        UpdateVisualState();

        _activateTween = transform.DOScale(_activateScale, _activateDuration)
            .SetLoops(-1, LoopType.Yoyo) // -1은 무한 반복, Yoyo는 왔다갔다
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .Pause()
            .OnStart(() => transform.transform.localScale = Vector3.one); // 오브젝트 파괴 시 트윈도 자동 제거 (메모리 누수 방지)

        // 갈 수 있는 곳이면 scale animation 좀 해주자
        if (NodeData.NodeState == EMapNodeState.Available)
        {
            _activateTween.Restart();
        }
    }

    public void AddPrevView(ExpeditionNodeView prevNodeView)
    {
        _prevNodeViews.Add(prevNodeView);
    }

    /// <summary>
    /// 이전 노드들과 X 위치가 겹치면 조정
    /// </summary>
    public void AdjustPosition(float yOffsetIfSameY, float noise)
    {
        float adjustX = transform.position.x;
        float adjustY = transform.position.y;

        if (_prevNodeViews.Count > 1)
        {
            adjustX = 0f;
            foreach (var prevView in _prevNodeViews)
            {
                adjustX += prevView.Position.x;
            }
            adjustX /= _prevNodeViews.Count;
        }

        foreach (var prevView in _prevNodeViews)
        {
            if (Mathf.Abs(prevView.Position.x - adjustX) <= noise)
            {
                adjustY += yOffsetIfSameY;
                break;
            }
        }

        transform.position = new Vector3(adjustX, adjustY, transform.position.z);
    }

    /// <summary>
    /// 투명도 설정 (SpriteRenderer 알파)
    /// </summary>
    public void SetTransparency(float alpha)
    {
        if (_spriteRenderer != null)
        {
            Color color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// SortingOrder 설정
    /// </summary>
    public void SetOrder(int order)
    {
        _spriteRenderer.sortingOrder = order;
    }

    /// <summary>
    /// SpriteRenderer의 색 설정
    /// </summary>
    public void SetColor(Color color)
    {
        _spriteRenderer.color = color;
    }

    /// <summary>
    /// 노드 타입에 맞는 스프라이트 갱신
    /// </summary>
    public void UpdateVisualState()
    {
        if (NodeData == null || _spriteRenderer == null) return;

        _spriteRenderer.sprite = SpriteManager.Instance.GetSprite("Island_" + NodeData.NodeType.ToString());
    }

    private void OnMouseEnter()
    {
        if (UIManager.Instance.IsLock)
        {
            return;
        }

        if (NodeData.NodeState != EMapNodeState.Available)
        {
            return;
        }

        _activateTween.Pause();
        transform.DOScale(_highlightScale, _highlightDuration).SetEase(Ease.OutExpo)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        UIManager.Instance.ShowGuidePopup(GetNodeExplain(NodeData.NodeType), _trGuideParent, true);
    }

    private void OnMouseExit()
    {
        if (UIManager.Instance.IsLock)
        {
            return;
        }

        if (NodeData.NodeState != EMapNodeState.Available)
        {
            return;
        }

        transform.DOScale(Vector3.one, _highlightDuration).SetEase(Ease.InExpo)
            .OnComplete(() => _activateTween.Restart())
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        UIManager.Instance.HideGuidePopup(_trGuideParent);
    }

    /// 클릭 이벤트
    private void OnMouseDown()
    {
        if(UIManager.Instance.IsLock)
        {
            return;
        }

        if (NodeData != null)
        {
            EventBus.Publish(new StPlayerClickedNodeEvent(NodeData));
        }
    }

    private EHelpKey GetNodeExplain(EMapNodeType nodeType)
    {
        switch (nodeType)
        {
            case EMapNodeType.Monster:
                return EHelpKey.Island_Monster;
            case EMapNodeType.Elite:
                return EHelpKey.Island_Elite;
            case EMapNodeType.Rest:
                return EHelpKey.Island_Rest;
            case EMapNodeType.Shop:
                return EHelpKey.Island_Shop;
            case EMapNodeType.Event:
                return EHelpKey.Island_Event;
            case EMapNodeType.Treasure:
                return EHelpKey.Island_Treasure;
            case EMapNodeType.Boss:
                return EHelpKey.Island_Boss;
            case EMapNodeType.Start:
                return EHelpKey.Island_Start;
            default:
                return EHelpKey.Unknown;
        }
    }
}
