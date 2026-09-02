using System.Collections.Generic;
using UnityEngine;

public class DealDamage_ElectricEffect : Effect
{
    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new DealDamage_ElectricGA(targets);
    }
}
