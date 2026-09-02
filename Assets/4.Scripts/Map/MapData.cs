using UnityEngine;

public class MapData
{
    public MapNode[,] Map { get; private set; } // 전체 노드들(층 기준으로 분리)
    public MapNode StartNode { get; private set; }
    public MapNode BossNode { get; private set; }
    public MapNode CurrentNode { get; private set; }
    public GameObject MapEnvObject { get; private set; }
    public Color IslandColor { get; private set; }
    public Color IsNextLandColor { get; private set; }

    public MapData(MapNode[,] map, MapNode startNode , MapNode bossNode)
    {
        Map = map;
        StartNode = startNode;
        BossNode = bossNode;
    }

    public void SetVisualData(GameObject mapEnvObject, Color islandColor, Color isNextLandColor)
    {
        MapEnvObject = mapEnvObject;
        IslandColor = islandColor;
        IsNextLandColor = isNextLandColor;
    }

    public void SetCurrentNode(MapNode currentNode)
    {
        CurrentNode = currentNode;
    }
}