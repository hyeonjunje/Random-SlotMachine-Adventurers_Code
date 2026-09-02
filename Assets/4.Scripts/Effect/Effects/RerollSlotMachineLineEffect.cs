using System.Collections.Generic;
using UnityEngine;

public class RerollSlotMachineLineEffect : Effect
{
    [SerializeField] private ESlotMachineLineDirection _direction = ESlotMachineLineDirection.Horizontal;
    [SerializeField] private int _lineCount = 1;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new RerollSlotMachineLineGA(_direction, _lineCount);
    }
}
