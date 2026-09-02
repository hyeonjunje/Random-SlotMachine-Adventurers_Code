using UnityEngine;

public class RerollSlotMachineKeywordAddTokenInBattlePhaseGA : GameAction
{
    public ESlotMachineRerollKeywordType SlotMachineRerollKeywordType { get; private set; }

    public RerollSlotMachineKeywordAddTokenInBattlePhaseGA(ESlotMachineRerollKeywordType slotMachineRerollKeywordType)
    {
        SlotMachineRerollKeywordType = slotMachineRerollKeywordType;
    }
}
