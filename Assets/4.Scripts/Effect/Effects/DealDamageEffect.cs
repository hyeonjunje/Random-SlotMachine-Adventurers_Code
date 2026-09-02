using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

public class DealDamageEffect : Effect
{
    [SerializeField] private DamageFormula _damageFormula;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new DealDamageGA(caster, targets, _damageFormula);
    }
}
