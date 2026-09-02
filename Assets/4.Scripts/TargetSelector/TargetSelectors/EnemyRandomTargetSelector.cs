using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyRandomTargetSelector : TargetSelector
{
    [SerializeField, Tooltip("랜덤으로 때릴 타겟의 개수")] private int _randomTargetCount = 1;

    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        List<CharacterView> result = new List<CharacterView>();

        result.AddRange(CharacterSystem.Instance.Enemies.Where(x => x.Character.IsDead == false).OrderBy(x => Guid.NewGuid())
            .Take(_randomTargetCount)
            .ToList());

        return result;
    }
}
