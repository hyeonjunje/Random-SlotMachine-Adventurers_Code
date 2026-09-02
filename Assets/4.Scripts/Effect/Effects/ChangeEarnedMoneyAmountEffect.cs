using System.Collections.Generic;
using UnityEngine;

public class ChangeEarnedMoneyAmountEffect : Effect
{
    [SerializeField] private float _amount;
    [SerializeField] private EChangeType _changeType;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new ChangeEarnedMoneyAmountGA(_amount, _changeType);
    }
}
