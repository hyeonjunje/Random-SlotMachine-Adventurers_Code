using UnityEngine;

[CreateAssetMenu(fileName = "SO_EnemyData", menuName = "Scriptable Objects/SO_EnemyData")]
public class SO_EnemyData : SO_CharacterData
{
    [field: Header("----- Enemy -----")]
    [field: SerializeField] public string Name { get; private set; }

    [field: Header("----- 적 AI -----")]
    [field: SerializeField] public SO_EnemyAI EnemyAI { get; private set; }
}
