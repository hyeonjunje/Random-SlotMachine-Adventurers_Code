using System.Collections.Generic;
using UnityEngine;

public class RemoveStatusByCategoryEffect : Effect
{
    [SerializeField] private EStatusCategory _statusCategory;
    [SerializeField] private int _count;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new RemoveStatusByCategoryGA(_statusCategory, targets, caster, _count);
    }
}
