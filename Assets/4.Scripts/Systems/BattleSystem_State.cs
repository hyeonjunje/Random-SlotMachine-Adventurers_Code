public partial class BattleSystem
{
    public EBattleState BattleState { get; private set; } = EBattleState.NonBattle;

    public void ChangeBattleState(EBattleState battleState)
    {
        // 같은건 넘어간다.
        if(BattleState == battleState)
        {
            return;
        }

        ExitState();

        BattleState = battleState;

        EnterState();
    }

    private void ExitState()
    {
        switch (BattleState)
        {
            case EBattleState.NonBattle:
                break;
            case EBattleState.StartBattle:
                break;
            case EBattleState.StartTurn:
                break;
            case EBattleState.SlotMachine:
                break;
            case EBattleState.SelectTarget:
                break;
            case EBattleState.InAutoBattle:
                break;
            case EBattleState.ClearBattle:
                break;
        }
    }

    private void EnterState()
    {
        switch (BattleState)
        {
            case EBattleState.NonBattle:
                break;
            case EBattleState.StartBattle:
                break;
            case EBattleState.StartTurn:
                break;
            case EBattleState.SlotMachine:
                break;
            case EBattleState.SelectTarget:
                break;
            case EBattleState.InAutoBattle:
                break;
            case EBattleState.ClearBattle:
                break;
        }
    }
}
