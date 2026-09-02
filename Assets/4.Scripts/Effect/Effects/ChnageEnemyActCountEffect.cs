using System.Collections.Generic;
using UnityEngine;

public class ChnageEnemyActCountEffect : Effect
{
    [SerializeField] private int _diff = 0;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        if (targets == null && TargetSelector != null)
        {
            targets = TargetSelector.SelectTarget (caster);
        }

        if (targets == null || targets.Count == 0)
        {
            return null;
        }

        return new ChangeEnemyActCountGA (_diff, targets);
    }
}