using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Effect
{
    [field: SerializeReference, SR] public TargetSelector TargetSelector { get; private set; }
    [field: SerializeField] public float DelayTime { get; private set; }
    [field: SerializeField] public string TargetEffect { get; private set; }
    public abstract GameAction GetGameAction(List<CharacterView> targets, CharacterView caster);

    public virtual string GetValue() { return ""; }
}
