using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapData를 JSON으로 직렬화하기 위한 클래스
/// </summary>
[Serializable]
public class SerializableMapData
{
    public List<SerializableMapNode> nodes;
    public int startNodeId;
    public int bossNodeId;
    public int currentNodeId;
    public int mapWidth;
    public int mapHeight;

    public SerializableMapData(MapData mapData)
    {
        nodes = new List<SerializableMapNode>();
        Dictionary<MapNode, int> nodeToId = new Dictionary<MapNode, int>();
        int currentId = 0;

        mapWidth = mapData.Map.GetLength(0);
        mapHeight = mapData.Map.GetLength(1);

        // 모든 노드를 리스트에 추가하고 ID 부여
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapData.Map[x, y] != null)
                {
                    nodeToId[mapData.Map[x, y]] = currentId++;
                }
            }
        }

        // 시작 노드와 보스 노드도 ID 부여
        if (!nodeToId.ContainsKey(mapData.StartNode))
        {
            nodeToId[mapData.StartNode] = currentId++;
        }
        if (!nodeToId.ContainsKey(mapData.BossNode))
        {
            nodeToId[mapData.BossNode] = currentId++;
        }

        startNodeId = nodeToId[mapData.StartNode];
        bossNodeId = nodeToId[mapData.BossNode];
        currentNodeId = nodeToId[mapData.CurrentNode];

        // 노드 직렬화
        foreach (var kvp in nodeToId)
        {
            MapNode node = kvp.Key;
            int id = kvp.Value;

            List<int> prevNodeIds = new List<int>();
            foreach (var prevNode in node.PrevNodes)
            {
                if (nodeToId.ContainsKey(prevNode))
                {
                    prevNodeIds.Add(nodeToId[prevNode]);
                }
            }

            List<int> nextNodeIds = new List<int>();
            foreach (var nextNode in node.NextNodes)
            {
                if (nodeToId.ContainsKey(nextNode))
                {
                    nextNodeIds.Add(nodeToId[nextNode]);
                }
            }

            nodes.Add(new SerializableMapNode
            {
                id = id,
                gridX = node.GridPosition.x,
                gridY = node.GridPosition.y,
                nodeType = (int)node.NodeType,
                nodeState = (int)node.NodeState,
                prevNodeIds = prevNodeIds,
                nextNodeIds = nextNodeIds
            });
        }
    }

    /// <summary>
    /// 직렬화된 데이터를 MapData로 변환
    /// </summary>
    public MapData ToMapData()
    {
        Dictionary<int, MapNode> idToNode = new Dictionary<int, MapNode>();

        // 먼저 모든 노드를 생성
        foreach (var serNode in nodes)
        {
            MapNode node = new MapNode(new Vector2Int(serNode.gridX, serNode.gridY));
            node.SetNodeType((EMapNodeType)serNode.nodeType);
            node.SetState((EMapNodeState)serNode.nodeState);
            idToNode[serNode.id] = node;
        }

        // 노드 간 연결 설정
        foreach (var serNode in nodes)
        {
            MapNode node = idToNode[serNode.id];

            foreach (int prevId in serNode.prevNodeIds)
            {
                if (idToNode.ContainsKey(prevId))
                {
                    node.AddPrevNode(idToNode[prevId]);
                }
            }

            foreach (int nextId in serNode.nextNodeIds)
            {
                if (idToNode.ContainsKey(nextId))
                {
                    node.AddNextNode(idToNode[nextId]);
                }
            }
        }

        // 2D 배열 재구성
        MapNode[,] map = new MapNode[mapWidth, mapHeight];
        foreach (var serNode in nodes)
        {
            int x = serNode.gridX;
            int y = serNode.gridY;

            // 유효한 그리드 위치인 경우에만 배열에 추가
            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
            {
                map[x, y] = idToNode[serNode.id];
            }
        }

        MapNode startNode = idToNode[startNodeId];
        MapNode bossNode = idToNode[bossNodeId];
        MapNode currentNode = idToNode[currentNodeId];

        MapData mapData = new MapData(map, startNode, bossNode);
        mapData.SetCurrentNode(currentNode);

        return mapData;
    }
}

[Serializable]
public class SerializableMapNode
{
    public int id;
    public int gridX;
    public int gridY;
    public int nodeType;
    public int nodeState;
    public List<int> prevNodeIds;
    public List<int> nextNodeIds;
}
