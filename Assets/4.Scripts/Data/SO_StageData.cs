using UnityEngine;

[CreateAssetMenu(fileName = "SO_StageData", menuName = "Scriptable Objects/SO_StageData")]
public class SO_StageData : ScriptableObject
{
    [field: SerializeField] public SO_MapConfigData MapConfigData { get; private set; }
    [field: SerializeField] public FloorMatchupData[] MatchupDatas { get; private set; }
    [field: SerializeField] public FloorMatchupData BossMatchupData { get; private set; }
    [field: SerializeField] public FloorMatchupData EliteMatchupData { get; private set; }
}
