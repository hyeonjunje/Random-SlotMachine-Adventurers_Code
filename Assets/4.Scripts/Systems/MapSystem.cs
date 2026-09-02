using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystem : SingletonScene<MapSystem>
{
    private SO_MapConfigData _mapConfig;

    private MapNode[,] _nodesByFloor;
    private MapData _currentMapData;

    private System.IDisposable _onCreateMapEvent;
    private System.IDisposable _onFinishedCreateMapEvent;
    private System.IDisposable _onPlayerClickedNodeEvent;
    public MapData CurrentMapData => _currentMapData;
    private bool _suppressAutoSaveOnFinishedCreate;
    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();
        _onCreateMapEvent = EventBus.Subscribe<StCreateMapEvent>(OnCreateMapEvent);
        _onFinishedCreateMapEvent = EventBus.Subscribe<StFinishedCreateMapEvent>(OnFinishedCreateMapEvent);
        _onPlayerClickedNodeEvent = EventBus.Subscribe<StPlayerClickedNodeEvent>(OnPlayerClickedNode);
    }

    private void OnDisable()
    {
        _onCreateMapEvent?.Dispose();
        _onFinishedCreateMapEvent?.Dispose();
        _onPlayerClickedNodeEvent?.Dispose();
    }

    private void OnCreateMapEvent(StCreateMapEvent createMapEvent)
    {
        _mapConfig = createMapEvent.MapConfigData;
        _nodesByFloor = new MapNode[_mapConfig.MapSize.x, _mapConfig.MapSize.y];

        // 시작점 만들기
        int centerXIndex = _mapConfig.MapSize.x / 2;
        MapNode startNode = new MapNode(new Vector2Int(centerXIndex, -1));
        startNode.SetNodeType(EMapNodeType.Start);

        // 경로 만들기
        for (int i = 0; i < _mapConfig.RoutineCount; ++i)
        {
            MakeRoutine(startNode);
        }

        // 맵 타입 정하기
        SetMapNodeType();

        // 보스 방 연결
        MapNode bossNode = new MapNode(new Vector2Int(centerXIndex, _mapConfig.MapSize.y));
        bossNode.SetNodeType(EMapNodeType.Boss);
        for(int x = 0; x < _mapConfig.MapSize.x; ++x)
        {
            if(IsExistNode(x, _mapConfig.MapSize.y - 1))
            {
                _nodesByFloor[x, _mapConfig.MapSize.y - 1].AddNextNode(bossNode);
                bossNode.AddPrevNode(_nodesByFloor[x, _mapConfig.MapSize.y - 1]);
            }
        }

        // 만든 맵을 가지고 맵 데이터 생성
        MapData mapData = new MapData(_nodesByFloor, startNode, bossNode);
        mapData.SetVisualData(_mapConfig.MapPrefab, _mapConfig.IslandColor, _mapConfig.IsNextLandColor);
        mapData.SetCurrentNode(startNode);
        EventBus.Publish(new StFinishedCreateMapEvent(mapData));
    }

    private void OnFinishedCreateMapEvent(StFinishedCreateMapEvent finishedCreateMapEvent)
    {
        _currentMapData = finishedCreateMapEvent.MapData;
        _nodesByFloor = finishedCreateMapEvent.MapData.Map;
        UpdateAllNodeStates();

        if (_suppressAutoSaveOnFinishedCreate)
        {
            _suppressAutoSaveOnFinishedCreate = false;
            return;
        }

        RunSaveService.SaveMapOnly (_currentMapData, StageSystem.Instance.CurrentBossMatchupIndex);

    }

    private bool IsExistNode(int x, int y)
    {
        if(x < 0 || x >= _nodesByFloor.GetLength(0) || y < 0 || y >= _nodesByFloor.GetLength(1))
        {
            return false;
        }

        return _nodesByFloor[x, y] != null;
    }

    private void MakeRoutine(MapNode mapNode)
    {
        Vector2Int curPos = mapNode.GridPosition;

        if(curPos.y >= _mapConfig.MapSize.y - 1)
        {
            return;
        }

        int[] diffX;
        bool isProceed = false;
        int nextX = 0;

        // 갈 수 있는게 나올 때까지 반복
        while (isProceed == false)
        {
            if(curPos.x == 0)
            {
                diffX = new int[] { 0, 1 };
            }
            else if(curPos.x == _mapConfig.MapSize.x - 1)
            {
                diffX = new int[] { -1, 0 };
            }
            else
            {
                diffX = new int[] { -1, 0, 1 };
            }

            nextX = curPos.x + diffX[Random.Range(0, diffX.Length)];

            // 교차 검증
            if(nextX - curPos.x == -1)
            {
                if(IsExistNode(curPos.x - 1, curPos.y) && IsExistNode(curPos.x, curPos.y + 1))
                {
                    if (_nodesByFloor[curPos.x - 1, curPos.y].NextNodes.Contains(_nodesByFloor[curPos.x, curPos.y + 1]))
                    {
                        continue;
                    }
                }
            }
            else if(nextX - curPos.x == 1)
            {
                if (IsExistNode(curPos.x + 1, curPos.y) && IsExistNode(curPos.x, curPos.y + 1))
                {
                    if (_nodesByFloor[curPos.x + 1, curPos.y].NextNodes.Contains(_nodesByFloor[curPos.x, curPos.y + 1]))
                    {
                        continue;
                    }
                }
            }

            isProceed = true;
        }

        if(IsExistNode(nextX, curPos.y + 1) == false)
        {
            _nodesByFloor[nextX, curPos.y + 1] = new MapNode(new Vector2Int(nextX, curPos.y + 1));
        }

        mapNode.AddNextNode(_nodesByFloor[nextX, curPos.y + 1]);
        _nodesByFloor[nextX, curPos.y + 1].AddPrevNode(mapNode);

        MakeRoutine(_nodesByFloor[nextX, curPos.y + 1]);
    }

    private void SetMapNodeType()
    {
        for(int x = 0; x < _nodesByFloor.GetLength(0); ++x)
        {
            if (IsExistNode(x, _mapConfig.MonsterFloor))
            {
                _nodesByFloor[x, _mapConfig.MonsterFloor].SetNodeType(EMapNodeType.Monster);
            }

            if (IsExistNode(x, _mapConfig.TreasureFloor))
            {
                _nodesByFloor[x, _mapConfig.TreasureFloor].SetNodeType(EMapNodeType.Treasure);
            }

            if (IsExistNode(x, _mapConfig.RestFloor))
            {
                _nodesByFloor[x, _mapConfig.RestFloor].SetNodeType(EMapNodeType.Rest);
            }
        }

        int cnt = 0;

        while(!SetMapNodeTypeRandom())
        {
            if(cnt++ > 100)
            {
                Debug.Log("이건 뭔가 로직에 문제가 있음");
                break;
            }

            Debug.Log("조건에 맞는 맵을 만들 수 없습니다. 다시 시도합니다.");
        }
    }

    private bool SetMapNodeTypeRandom()
    {
        // 초기화
        for (int x = 0; x < _nodesByFloor.GetLength(0); ++x)
        {
            for (int y = 0; y < _nodesByFloor.GetLength(1); ++y)
            {
                if (y != _mapConfig.MonsterFloor && y != _mapConfig.TreasureFloor && y != _mapConfig.RestFloor)
                {
                    if(IsExistNode(x, y))
                    {
                        _nodesByFloor[x, y].SetNodeType(EMapNodeType.None);
                    }
                }
            }
        }

        for (int x = 0; x < _nodesByFloor.GetLength(0); ++x)
        {
            for (int y = 0; y < _nodesByFloor.GetLength(1); ++y)
            {
                if (IsExistNode(x, y) && _nodesByFloor[x, y].NodeType == EMapNodeType.None)
                {
                    List<EMapNodeType> possible = null;

                    if (y >= _mapConfig.EliteAndRestMinFloor)
                    {
                        possible = new List<EMapNodeType> { EMapNodeType.Monster, EMapNodeType.Event, EMapNodeType.Shop, EMapNodeType.Elite, EMapNodeType.Rest, EMapNodeType.Treasure };
                    }
                    else
                    {
                        possible = new List<EMapNodeType> { EMapNodeType.Monster, EMapNodeType.Event, EMapNodeType.Shop, EMapNodeType.Treasure };
                    }

                    // 형제 노드와 안겹치게
                    foreach (MapNode prevNode in _nodesByFloor[x, y].PrevNodes)
                    {
                        foreach (MapNode nextNode in prevNode.NextNodes)
                        {
                            if (nextNode.NodeType != EMapNodeType.None)
                            {
                                possible.Remove(nextNode.NodeType);
                            }
                        }
                    }

                    // 엘리트, 상점, 휴식방은 연속으로 못나오게
                    foreach (MapNode prevNode in _nodesByFloor[x, y].PrevNodes)
                    {
                        if (prevNode.NodeType == EMapNodeType.Elite || prevNode.NodeType == EMapNodeType.Shop || prevNode.NodeType == EMapNodeType.Rest)
                        {
                            possible.Remove(prevNode.NodeType);
                        }
                    }

                    foreach (MapNode nextNode in _nodesByFloor[x, y].NextNodes)
                    {
                        if (nextNode.NodeType == EMapNodeType.Elite || nextNode.NodeType == EMapNodeType.Shop || nextNode.NodeType == EMapNodeType.Rest)
                        {
                            possible.Remove(nextNode.NodeType);
                        }
                    }

                    if (possible.Count == 0)
                    {
                        return false;
                    }

                    EMapNodeType nodeType = _mapConfig.GetNode(possible);
                    _nodesByFloor[x, y].SetNodeType(nodeType);
                }
            }
        }
        return true;
    }

    private void OnPlayerClickedNode(StPlayerClickedNodeEvent playerClickedNodeEvent)
    {
        MapNode clickedNode = playerClickedNodeEvent.TargetNode;

        if (clickedNode.NodeState != EMapNodeState.Available)
        {
            // 방문 불가능한 노드를 클릭
            EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_MAPSYSTEM_014"), EMessageType.Warning));
            return;
        }

        StartCoroutine(CoOnPlayerClickedNode(playerClickedNodeEvent));
    }

    private IEnumerator CoOnPlayerClickedNode(StPlayerClickedNodeEvent playerClickedNodeEvent)
    {
        MapNode clickedNode = playerClickedNodeEvent.TargetNode;

        // Fade Out 효과
        yield return StartCoroutine(UIManager.Instance.TransitionFadeOut());

        // 현재 노드 세팅
        _currentMapData.SetCurrentNode(clickedNode);

        // 현재 세팅된 노드를 기반으로 맵 전체 노드 상태 갱신
        UpdateAllNodeStates();

        // UI가 갱신되도록 이벤트를 다시 발행!
        EventBus.Publish(new StMapStateUpdatedEvent(_currentMapData.CurrentNode));

        yield return StartCoroutine(UIManager.Instance.TransitionFadeIn());
    }

    /// <summary>
    /// 플레이어의 현재 위치(_currentMapData.CurrentNode)를 기준으로 모든 노드의 상태를 갱신합니다.
    /// </summary>
    private void UpdateAllNodeStates()
    {
        if (_currentMapData == null)
        {
            return;
        }

        for (int i = 0; i < _nodesByFloor.GetLength(0); ++i)
        {
            for (int j = 0; j < _nodesByFloor.GetLength(1); ++j)
            {
                if (IsExistNode(i, j))
                {
                    MapNode node = _nodesByFloor[i, j];

                    if (node.NodeState == EMapNodeState.Visited) // 방문한 곳이면 건드리지 않음
                    {
                        continue;
                    }

                    if (node == _currentMapData.CurrentNode)
                    {
                        node.SetState(EMapNodeState.Visited); // 현재 위치는 '방문함'
                    }
                    else if (_currentMapData.CurrentNode.NextNodes.Contains(node))
                    {
                        node.SetState(EMapNodeState.Available); // 현재 위치 다음 방들은 '갈 수 있음'
                    }
                    else // 그 밖에 방들은 Locked처리
                    {
                        node.SetState(EMapNodeState.Locked);
                    }
                }
            }
        }

        bool isVisitedLastNode = false;

        for (int i = 0; i < _nodesByFloor.GetLength(0); ++i)
        {
            if (IsExistNode(i, _nodesByFloor.GetLength(1) - 1))
            {
                MapNode node = _nodesByFloor[i, _nodesByFloor.GetLength(1) - 1];
                if (node.NodeState == EMapNodeState.Visited)
                {
                    isVisitedLastNode = true;
                }
            }
        }

        if(isVisitedLastNode)
        {
            _currentMapData.BossNode.SetState(EMapNodeState.Available);
        }
    }

    public void SetLoadedMapData(MapData mapData)
    {
        _suppressAutoSaveOnFinishedCreate = true;

        _currentMapData = mapData;
        _nodesByFloor = mapData.Map;
        UpdateAllNodeStates ();

        EventBus.Publish (new StFinishedCreateMapEvent (mapData));
    }
}

