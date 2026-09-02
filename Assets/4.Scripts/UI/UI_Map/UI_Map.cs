using Spine;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_Map : UI_Base
{
    [Header("컴포넌트")]
    [SerializeField] private RectTransform _scrollViewContent;

    [Header("프리팹")]
    [SerializeField] private MapNodeUI _nodeViewPrefab;
    [SerializeField] private MapEdgeUI _linePrefab;

    [Header("배치 컨테이너")]
    [SerializeField] private RectTransform _nodeContainer;
    [SerializeField] private RectTransform _lineContainer;

    [Header("맵 레이아웃 설정")]
    [SerializeField] private Vector2 _positionBossNode;  // 보스 방 위치
    [SerializeField] private float _scaleBossNode;       // 보스 방 크기
    [SerializeField] private float _topPadding = 30f;    // 맵 스크롤 뷰 위 패딩
    [SerializeField] private float _bottomPadding = 30f; // 맵 스크롤 뷰 아래 패딩
    [SerializeField] private float _bossNodeSize = 600;  // 보스 노드의 크기
    [SerializeField] private float _nodeSpacing = 250f;  // 층(가로) 간격
    [SerializeField] private float _floorSpacing = 120f;   // 층 내 노드(세로) 간격

    private List<MapNodeUI> _spawnedNodeViews = new List<MapNodeUI>();

    private IDisposable _onCreateMapEvent;
    private IDisposable _onMapStateUpdatedEvent;

    public override void Initialize()
    {
        base.Initialize();

        _onCreateMapEvent = EventBus.Subscribe<StFinishedCreateMapEvent>(OnFinishedCreateMapEvent);
        _onMapStateUpdatedEvent = EventBus.Subscribe<StMapStateUpdatedEvent>(OnMapStateUpdated);

        if (AppConfig.InGame.IsShowMinimap == false)
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        _onCreateMapEvent?.Dispose();
        _onMapStateUpdatedEvent?.Dispose();
    }

    public override void Close()
    {
        gameObject.SetActive(false);
    }

    public override void Open()
    {
        gameObject.SetActive(true);

        if (AppConfig.InGame.IsShowMinimap == false)
        {
            Close();
        }
    }

    /// 전달받은 StFinishedCreateMapEvent를 기반으로 맵 전체를 그립니다.
    private void OnFinishedCreateMapEvent(StFinishedCreateMapEvent finishedCreateMapEvent)
    {
        DrawMap(finishedCreateMapEvent.MapData);
    }

    /// 맵 상태가 업데이트되었다는 이벤트를 받았을 때, 모든 노드의 시각적 상태를 갱신합니다.
    private void OnMapStateUpdated(StMapStateUpdatedEvent mapStateUpdatedEvent)
    {
        foreach (var nodeView in _spawnedNodeViews)
        {
            nodeView.UpdateVisualState();
        }
    }

    private void DrawMap(MapData mapData)
    {
        ClearMap();

        if (mapData == null)
        {
            Debug.LogError("표시할 맵 데이터가 없습니다.");
            return;
        }

        // 노드 생성 및 배치
        for (int y = 0; y < mapData.Map.GetLength(1); ++y)
        {
            for(int x = 0; x < mapData.Map.GetLength(0); ++x)
            {
                if (mapData.Map[x, y] != null)
                {
                    MapNode nodeData = mapData.Map[x, y];

                    MapNodeUI nodeView = Instantiate(_nodeViewPrefab, _nodeContainer);

                    Vector2 position = CalculateNodePosition(nodeData, mapData);

                    nodeView.Init(nodeData, position);
                    _spawnedNodeViews.Add(nodeView);
                }
            }
        }

        // 선 생성 및 연결
        foreach (var nodeView in _spawnedNodeViews)
        {
            MapNode startNodeData = nodeView.NodeData;

            foreach (var endNodeData in startNodeData.NextNodes)
            {
                MapNodeUI endNodeView = FindNodeView(endNodeData);
                if (endNodeView == null)
                {
                    continue;
                }

                MapEdgeUI line = Instantiate(_linePrefab, _lineContainer);
                line.DrawLine(nodeView.RectPosition, endNodeView.RectPosition);
            }
        }

        // 스크롤 뷰의 높이 계산
        float totalHeight = (mapData.Map.GetLength(1) * _floorSpacing) + _bossNodeSize + _topPadding + _bottomPadding;
        _scrollViewContent.sizeDelta = new Vector2(_scrollViewContent.sizeDelta.x, totalHeight);

        // 보스 노드 연결
        MapNodeUI bossNodeUI = Instantiate(_nodeViewPrefab, _nodeContainer);
        bossNodeUI.Init(mapData.BossNode, _positionBossNode);
        bossNodeUI.transform.localScale = Vector3.one * _scaleBossNode;
        foreach (MapNode node in mapData.BossNode.PrevNodes)
        {
            MapNodeUI topNode = FindNodeView(node);
            MapEdgeUI line = Instantiate(_linePrefab, _lineContainer);
            line.DrawLine(bossNodeUI.RectPosition, topNode.RectPosition);
        }
    }

    private Vector2 CalculateNodePosition(MapNode nodeData, MapData mapData)
    {
        int nodeIndex = nodeData.GridPosition.x;
        int floorIndex = nodeData.GridPosition.y;

        int nodeCountInFloor = mapData.Map.GetLength(0);
        float xOffset = (nodeCountInFloor - 1) * 0.5f; // 중앙 정렬을 위한 오프셋

        float x = (nodeIndex - xOffset) * _nodeSpacing;
        float y = floorIndex * _floorSpacing + _bottomPadding;

        return new Vector2(x, y);
    }

    private MapNodeUI FindNodeView(MapNode nodeData)
    {
        return _spawnedNodeViews.Find(view => view.NodeData == nodeData);
    }

    private void ClearMap()
    {
        _nodeContainer.DestroyAllChildren();
        _lineContainer.DestroyAllChildren();
        _spawnedNodeViews.Clear();
    }
}
