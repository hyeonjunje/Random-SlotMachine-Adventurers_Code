using System.Collections.Generic;
using UnityEngine;

public class AddTempExtraValueEffect : Effect
{
    [SerializeField] private EAdverbEffectTargetType _adverbEffectTargetType;
    [SerializeField] private float _extraValue;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new AddTempExtraValueGA(_adverbEffectTargetType, _extraValue);
    }
}
