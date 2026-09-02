public enum ETutorialStep
{
    None,
    Intro,
    SpawnEnemy,
    ExplainSingleLineSlot,
    ExplainCompletedSentence,
    ExplainTargetSelect,
    AloneIsHard,
    AlliesJoin,
    ExplainPartySlot,
    Complete,
}

public enum ETutorialPhase
{
    None,
    Intro,
    Turn1Spin,
    Turn1TargetAndAttack,
    Turn2AlliesJoin,
    Turn2PartySpin,
    FreeBattleUntilClear,
    Complete,
}

public enum ETutorialWaitType
{
    None,
    DialogueConfirm,
    SlotSpinCompleted,
    SlotConfirmClicked,
    PlayerActed,
    BattleCleared,
}
