using System.Collections;
using UnityEngine;

public class ClickUseSlotMachineTokenGA : GameAction
{
    public BattleAct BattleAct { get; private set; }

    public ClickUseSlotMachineTokenGA(BattleAct battleAct)
    {
        BattleAct = battleAct;
    }
}