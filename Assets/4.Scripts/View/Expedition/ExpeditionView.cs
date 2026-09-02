using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// SpriteRenderer 기반 탐험 맵 뷰 (UI_Expedition의 월드 스페이스 버전)
/// </summary>
public class ExpeditionView : MonoBehaviour
{
    [Header("배치 컨테이너")]
    [SerializeField] private Transform _nodeContainer;
    [SerializeField] private Transform _edgeContainer;

    [Header("프리팹")]
    [SerializeField] private ExpeditionNodeView _nodePrefab;
    [SerializeField] private ExpeditionEdgeView _edgePrefab;

    [Header("피봇 위치")]
    [SerializeField] private Transform _pivotEnv;
    [SerializeField] private Transform _pivotStarting;
    [SerializeField] private Transform _pivotBoss;

    [Header("보스 관련")]
    [SerializeField] private SpriteRenderer _bossSprite;
    [SerializeField] private Vector2 _diffBossAlpha = new Vector2(0.2f, 1f); // 진행될수록 보스의 알파가 커짐
    [SerializeField] private Vector2 _diffBossScale = new Vector2(0.5f, 1f); // 진행될수록 보스의 크기가 커짐

    [Header("레이아웃 설정")]
    [SerializeField] private Vector2[] _spacingNode = new Vector2[] { new Vector2(2f, 2f) };
    [SerializeField] private float _xOffsetValueIfSameX = 1f;
    [SerializeField] private float _noiseForce = 0.5f;
    [SerializeField] private float _diffScale = 0.1f;
    [SerializeField] private float _diffTransparency = 0.15f;

    private Dictionary<MapNode, ExpeditionNodeView> _cachedViews = new Dictionary<MapNode, ExpeditionNodeView>();
    private int _maxLayer = 0;
    private MapNode _bossNode;
    private Color _islandColor = Color.white;
    private Color _isNextLandColor = Color.white;

    private System.IDisposable _onFinishedCreateMapEvent;
    private System.IDisposable _onLeaveNodeEvent;
    private System.IDisposable _onDecideBossMatchupEvent;

    private void Awake()
    {
        _onFinishedCreateMapEvent = EventBus.Subscribe<StFinishedCreateMapEvent>(OnFinishedCreateMapEvent);
        _onLeaveNodeEvent = EventBus.Subscribe<StLeaveNodeEvent>(OnLeaveNodeEvent);
        _onDecideBossMatchupEvent = EventBus.Subscribe<StDecideBossMatchupEvent>(OnDecideBossMatchupEvent);
    }

    private void OnDestroy()
    {
        _onFinishedCreateMapEvent?.Dispose();
        _onLeaveNodeEvent?.Dispose();
        _onDecideBossMatchupEvent?.Dispose();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if(DataManager.Instance.GameModel.Stage == 0)
        {
            // AudioManager.Instance.PlayBGM(EBgmId.Stage1);
        }
        else if (DataManager.Instance.GameModel.Stage == 1)
        {
            // AudioManager.Instance.PlayBGM(EBgmId.Stage2);
        }
        else if (DataManager.Instance.GameModel.Stage == 2)
        {
            // AudioManager.Instance.PlayBGM(EBgmId.Stage3);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnFinishedCreateMapEvent(StFinishedCreateMapEvent finishedCreateMapEvent)
    {
        Show ();

        ClearExpedition ();

        CreateEnv (finishedCreateMapEvent.MapData.MapEnvObject);
        _islandColor = finishedCreateMapEvent.MapData.IslandColor;
        _isNextLandColor = finishedCreateMapEvent.MapData.IsNextLandColor;

        _bossNode = finishedCreateMapEvent.MapData.BossNode;
        _maxLayer = _bossNode.GridPosition.y;

        Vector3 startPosition = _pivotStarting != null ? _pivotStarting.position : Vector3.zero;

        MapNode baseNode = finishedCreateMapEvent.MapData.CurrentNode ?? finishedCreateMapEvent.MapData.StartNode;

        DrawNode (baseNode, startPosition);
        AdjustNodePos ();
        DrawEdge (baseNode, startPosition);
        DrawBoss (baseNode);
    }

    private void OnLeaveNodeEvent(StLeaveNodeEvent leaveModeEvent)
    {
        Show();

        ClearExpedition();

        Vector3 startPosition = _pivotStarting != null ? _pivotStarting.position : Vector3.zero;

        DrawNode(leaveModeEvent.CurrentNode, startPosition);

        AdjustNodePos();

        DrawEdge(leaveModeEvent.CurrentNode, startPosition);
        DrawBoss(leaveModeEvent.CurrentNode);
    }

    private void OnDecideBossMatchupEvent(StDecideBossMatchupEvent decideBossMatchupEvent)
    {
        if (_bossSprite != null)
        {
            _bossSprite.gameObject.SetActive(true);
            _bossSprite.sprite = decideBossMatchupEvent.BossMatchupEnemyBundle.MatchupSprite;
        }
    }

    private void ClearExpedition()
    {
        // 노드 컨테이너의 모든 자식 삭제
        _nodeContainer.DestroyAllChildren();

        // 간선 컨테이너의 모든 자식 삭제
        _edgeContainer.DestroyAllChildren();

        _cachedViews.Clear();
    }

    private void CreateEnv(GameObject envObject)
    {
        _pivotEnv.DestroyAllChildren();
        GameObject instanceEnvObject = Instantiate(envObject, _pivotEnv);
        instanceEnvObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private void DrawNode(MapNode node, Vector3 currentPosition, int layer = 0)
    {
        if (layer >= _spacingNode.Length)
        {
            return;
        }

        List<MapNode> nextNodes = node.NextNodes.OrderBy(n => n.GridPosition.x).ToList();

        // 시작점 구하기
        Vector3 nextNodeStartPosition = Vector3.zero;
        if (nextNodes.Count % 2 == 0) // 짝수
        {
            float startX = currentPosition.x - ((nextNodes.Count - 1) / 2f * _spacingNode[layer].x) - _spacingNode[layer].x / 2f;
            nextNodeStartPosition = new Vector3(startX, currentPosition.y + _spacingNode[layer].y, 0);
        }
        else // 홀수
        {
            float startX = currentPosition.x - (nextNodes.Count / 2 * _spacingNode[layer].x);
            nextNodeStartPosition = new Vector3(startX, currentPosition.y + _spacingNode[layer].y, 0);
        }

        // 시작점부터 x spacing 거리로 다음 노드 뷰 생성
        for (int i = 0; i < nextNodes.Count; ++i)
        {
            // 보스방은 따로 그릴거기 때문에 return
            if (nextNodes[i].NodeType == EMapNodeType.Boss)
            {
                return;
            }

            if (!_cachedViews.TryGetValue(nextNodes[i], out ExpeditionNodeView nodeView))
            {
                Vector2 noise = Random.insideUnitCircle * _noiseForce;
                Vector3 worldPosition = nextNodeStartPosition + Vector3.right * _spacingNode[layer].x * i + new Vector3(noise.x, noise.y, 0);

                nodeView = Instantiate(_nodePrefab, _nodeContainer);
                nodeView.Init(nextNodes[i], worldPosition);
                nodeView.transform.localScale = Vector3.one * (1 - _diffScale * layer);
                nodeView.SetTransparency(1 - _diffTransparency * layer);
                nodeView.SetOrder(Mathf.CeilToInt(nodeView.transform.localScale.x * 10));
                nodeView.SetColor(layer == 0 ? _islandColor : _isNextLandColor);
                _cachedViews.Add(nextNodes[i], nodeView);
            }

            // 이전 위치를 기반으로 현재 위치 조정
            if (layer > 0 && _cachedViews.TryGetValue(node, out ExpeditionNodeView prevView))
            {
                nodeView.AddPrevView(prevView);
            }

            DrawNode(nextNodes[i], nodeView.Position, layer + 1);
        }
    }

    private void AdjustNodePos()
    {
        foreach(ExpeditionNodeView nodeView in _cachedViews.Values)
        {
            nodeView.AdjustPosition(_xOffsetValueIfSameX, _noiseForce);
        }
    }

    private void DrawEdge(MapNode node, Vector3 currentPosition, int layer = 0)
    {
        if (layer >= _spacingNode.Length)
        {
            return;
        }

        for (int i = 0; i < node.NextNodes.Count; ++i)
        {
            if (_cachedViews.TryGetValue(node.NextNodes[i], out ExpeditionNodeView nodeView))
            {
                ExpeditionEdgeView edgeView = Instantiate(_edgePrefab, _edgeContainer);
                edgeView.DrawLine(currentPosition, nodeView.Position);
                DrawEdge(node.NextNodes[i], nodeView.Position, layer + 1);
            }
        }
    }

    private void DrawBoss(MapNode currentNode)
    {
        if (_bossSprite == null) return;

        Vector3 bossPosition = _pivotBoss != null ? _pivotBoss.position : Vector3.zero;

        // 현재 위치가 마지막 노드의 이전 노드라면 보스를 보여준다.
        if (currentNode.GridPosition.y == _maxLayer - 1)
        {
            _bossSprite.color = Color.white;
            _bossSprite.transform.localScale = Vector3.one;

            // 보스와 일직선 간선 생성
            Vector3 startPosition = _pivotStarting != null ? _pivotStarting.position : Vector3.zero;
            ExpeditionEdgeView edgeView = Instantiate(_edgePrefab, _edgeContainer);
            edgeView.DrawLine(startPosition, bossPosition);
        }
        else
        {
            float progress = (float)currentNode.GridPosition.y / _maxLayer;
            Color color = Color.black;
            color.a = Mathf.Lerp(_diffBossAlpha.x, _diffBossAlpha.y, progress);
            _bossSprite.color = color;
            _bossSprite.transform.localScale = Vector3.one * Mathf.Lerp(_diffBossScale.x, _diffBossScale.y, progress);
        }
    }

    /// <summary>
    /// 보스 노드 클릭 시 호출 (버튼이나 Collider에서 호출)
    /// </summary>
    public void OnClickBossNode()
    {
        if (_bossNode != null)
        {
            EventBus.Publish(new StPlayerClickedNodeEvent(_bossNode));
        }
    }
}
