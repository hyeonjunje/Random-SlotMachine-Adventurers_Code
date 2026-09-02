using SerializeReferenceEditor;
using UnityEngine;

[System.Serializable]
public class AdverbSkillEffect
{
    [field: SerializeField] public EAdverbAdjustTiming AdverbEffectType;
    [field: SerializeField] public EAdverbEffectTargetType AdverbEffectTargetType;

    [field: SerializeReference, SR] public Effect Effect { get; private set; }
}
