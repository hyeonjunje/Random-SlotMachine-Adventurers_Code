using System.Collections.Generic;
using UnityEngine;

public class ApplyGoldDeltaEffect : Effect
{
    [SerializeField] private int _amount = 0;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new ApplyGoldDeltaGA(_amount);
    }
}