using UnityEngine;

public enum EHelpKey
{
    Unknown,          
    HP,
    Gold,
    Keyword,
    Setting,
    Mana,
    Process,
    SlotReroll,
    Exchange,
    Copy,
    ShowSlowMachine,
    Reroll,
    Island_Monster,
    Island_Elite,
    Island_Rest,
    Island_Shop,
    Island_Event,
    Island_Treasure,
    Island_Boss,
    Island_Start,
    Remove_Word,
    SlotMachine_Fail,
    SlotMachine_Success,
    SlotMachine_GreatSuccess,
    SlotMachine_GreatGreatSuccess,
}


[CreateAssetMenu(fileName = "SO_HelpData", menuName = "Scriptable Objects/SO_HelpData")]
public class SO_HelpData : ScriptableObject
{
    [field: SerializeField] public StHelpData[] HelpDatas { get; private set; }
}

[System.Serializable]
public struct StHelpData
{
    [field: SerializeField] public EHelpKey HelpKey { get; private set; }
    [field: SerializeField] public string Title { get; private set; }
    [field: SerializeField] public string Contents { get; private set; }
}