using System.Collections.Generic;
using UnityEngine;

public class DealDamage_CounterAttackEffect : Effect
{
    [SerializeField] private ECharacterAnimationType _characterAnimationType;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new DealDamage_CounterAttackGA(targets, _characterAnimationType);
    }
}