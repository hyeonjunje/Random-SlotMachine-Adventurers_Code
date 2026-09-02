using System.Collections.Generic;
using UnityEngine;

public class RerollSlotMachineKeywordAddTokenInBattlePhaseEffect : Effect
{
    [SerializeField] private ESlotMachineRerollKeywordType _slotMachineRerollKeywordType;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new RerollSlotMachineKeywordAddTokenInBattlePhaseGA(_slotMachineRerollKeywordType);
    }
}
