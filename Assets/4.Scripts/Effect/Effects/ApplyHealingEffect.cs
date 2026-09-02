using System.Collections.Generic;
using UnityEngine;
public class ApplyHealingEffect : Effect
{
    [SerializeField] private HealingFormula _healingFormula;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new ApplyHealingGA(caster, targets, _healingFormula);
    }
}