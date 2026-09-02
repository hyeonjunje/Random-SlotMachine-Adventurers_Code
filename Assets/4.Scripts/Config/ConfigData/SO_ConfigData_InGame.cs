using UnityEngine;

[CreateAssetMenu(fileName = "SO_ConfigData_InGame", menuName = "Scriptable Objects/Config/SO_ConfigData_InGame")]
public class SO_ConfigData_InGame : ScriptableObject
{
    [field: SerializeField] public bool IsSkipBattle { get; private set; } = false;
    [field: SerializeField] public bool IsSkipBoss { get; private set; } = false;
    [field: SerializeField] public bool IsSkipRest { get; private set; } = false;
    [field: SerializeField] public bool IsSkipShop { get; private set; } = false;
    [field: SerializeField] public bool IsSkipTreasure { get; private set; } = false;
    [field: SerializeField] public bool IsSkipEvent { get; private set; } = false;
    [field: SerializeField] public bool IsSkipTitleStartSequence { get; private set; } = false;
    [field: SerializeField] public bool IsShowMinimap { get; private set; } = false;
    [field: SerializeField] public bool IsEnableCheat { get; private set; } = false;
}
