using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_MapConfigData", menuName = "Scriptable Objects/SO_MapConfigData")]
public class SO_MapConfigData : ScriptableObject
{
    [Header("맵 구조 설정")]
    [field: SerializeField] public Vector2Int MapSize { get; private set; } = new Vector2Int(7, 15);
    [field: SerializeField] public int RoutineCount { get; private set; } = 6;

    [Header("노드 타입 확률 (가중치)")]
    [SerializeField] private List<NodeTypeWeight> _nodeTypeWeights;

    [Header("특수 규칙")]
    [field: SerializeField] public int MonsterFloor { get; private set; } = 0;         // 무조건 몬스터 층
    [field: SerializeField] public int TreasureFloor { get; private set; } = 8;        // 무조건 보물방 층
    [field: SerializeField] public int RestFloor { get; private set; } = 14;           // 무조건 휴식 층
    [field: SerializeField] public int EliteAndRestMinFloor { get; private set; } = 5; // 휴식, 엘리트 등장 가능한 최소 층

    [Header("맵 프리팹")]
    [field: SerializeField] public GameObject MapPrefab { get; private set; }
    [field: SerializeField] public Color IslandColor { get; private set; }
    [field: SerializeField] public Color IsNextLandColor { get; private set; }

    public EMapNodeType GetNode(List<EMapNodeType> possible)
    {
        var validWeights = _nodeTypeWeights
        .Where(data => possible.Contains(data.NodeType) && data.Weight > 0)
        .Select(data => new NodeTypeWeight
        {
            NodeType = data.NodeType,
            Weight = Mathf.Max(0f, data.Weight + ArtifactRuntimeState.GetMapNodeWeightDelta(data.NodeType))
        })
        .Where(data => data.Weight > 0)
        .ToList();

        float totalWeight = validWeights.Sum(data => data.Weight);

        if (totalWeight <= 0f)
        {
            if (possible.Count > 0)
            {
                return possible[0];
            }

            return default(EMapNodeType);
        }

        float randomValue = Random.Range(0f, totalWeight);

        foreach (var weightData in validWeights)
        {
            if (randomValue <= weightData.Weight)
            {
                return weightData.NodeType;
            }
            else
            {
                randomValue -= weightData.Weight;
            }
        }

        return validWeights[validWeights.Count - 1].NodeType;
    }

    /// <summary>
    /// 노드 타입별 생성 가중치를 정의하는 내부 클래스
    /// </summary>
    [System.Serializable]
    public class NodeTypeWeight
    {
        public EMapNodeType NodeType;
        public float Weight;
    }
}
