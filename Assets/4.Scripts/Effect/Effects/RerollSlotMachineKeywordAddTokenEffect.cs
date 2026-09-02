using System.Collections.Generic;
using UnityEngine;

public class RerollSlotMachineKeywordAddTokenEffect : Effect
{
    [SerializeField] private ESlotMachineRerollKeywordType _slotMachineRerollKeywordType;
    [SerializeField] private EKeyword _casedKeyword;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new RerollSlotMachineKeywordAddTokenGA(_slotMachineRerollKeywordType, _casedKeyword);
    }
}
