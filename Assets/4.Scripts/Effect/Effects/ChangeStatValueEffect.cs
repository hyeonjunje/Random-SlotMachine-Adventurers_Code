using System.Collections.Generic;
using UnityEngine;

public class ChangeStatValueEffect : Effect
{
    [SerializeField] private EStatType _statType;
    [SerializeField] private EStatModType _statModType;
    [SerializeField] private float _value;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        if ((_statType == EStatType.MaxHp || _statType == EStatType.MaxMana) &&
            (targets == null || targets.Count == 0))
        {
            return new ChangeStatValueGA (_statType, _statModType, _value, null, caster);
        }

        return new ChangeStatValueGA (_statType, _statModType, _value, targets, caster);
    }
}
