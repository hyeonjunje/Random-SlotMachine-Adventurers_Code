using System.Collections.Generic;
using UnityEngine;

public class DealDamage_PunishmentEffect : Effect
{
    [SerializeField] private ECharacterAnimationType _characterAnimationType;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new DealDamage_PunishmentGA(targets, _characterAnimationType);
    }
}
