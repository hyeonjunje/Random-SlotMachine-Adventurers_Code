public class ActAutoBattleGA : GameAction
{
    public BattleAct BattleAct { get; private set; }

    public ActAutoBattleGA(BattleAct battleAct)
    {
        BattleAct = battleAct;
    }
}