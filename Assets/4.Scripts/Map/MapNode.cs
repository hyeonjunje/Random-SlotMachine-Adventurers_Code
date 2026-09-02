using System.Collections.Generic;
using UnityEngine;

public class MapNode
{
    public List<MapNode> PrevNodes { get; private set; } // 이전 연결된 맵
    public List<MapNode> NextNodes { get; private set; } // 다음 연결된 맵
    public EMapNodeType NodeType { get; private set; } = EMapNodeType.None;
    public EMapNodeState NodeState { get; private set; }
    public Vector2Int GridPosition { get; private set; }

    public MapNode(Vector2Int gridPosition)
    {
        NodeState = EMapNodeState.Locked;
        GridPosition = gridPosition;
        PrevNodes = new List<MapNode>();
        NextNodes = new List<MapNode>();
    }

    public void SetNodeType(EMapNodeType type)
    {
        NodeType = type;
    }

    /// 다음 노드 추가
    public void AddNextNode(MapNode nextNode)
    {
        if (nextNode == null || NextNodes.Contains(nextNode))
        {
            return;
        }
        NextNodes.Add(nextNode);
    }

    /// 이전 노드 추가
    public void AddPrevNode(MapNode prevNode)
    {
        if (prevNode == null || PrevNodes.Contains(prevNode))
        {
            return;
        }
        PrevNodes.Add(prevNode);
    }

    // 노드 상태 변경
    public void SetState(EMapNodeState newState)
    {
        NodeState = newState;
    }
}
