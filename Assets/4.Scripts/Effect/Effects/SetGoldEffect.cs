using System.Collections.Generic;
using UnityEngine;

public class SetGoldEffect : Effect
{
    [SerializeField] private int _amount = 0;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new SetGoldGA(_amount);
    }
}
