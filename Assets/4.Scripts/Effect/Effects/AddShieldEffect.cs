using System.Collections.Generic;
using UnityEngine;

public class AddShieldEffect : Effect
{
    [SerializeField] private ShieldFormula _shieldFormula;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new AddShieldGA(caster, targets, _shieldFormula);    
    }
}
