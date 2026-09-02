using System.Collections.Generic;

public class StartAutoBattleGA : GameAction
{
    public List<BattleAct> BattleActs { get; private set; }

    public StartAutoBattleGA(List<BattleAct> battleAct)
    {
        BattleActs = new List<BattleAct>(battleAct);
    }
}