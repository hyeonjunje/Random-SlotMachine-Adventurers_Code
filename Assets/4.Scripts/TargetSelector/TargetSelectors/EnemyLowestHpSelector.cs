using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyLowestHpSelector : TargetSelector
{
    [SerializeField] private int _countTarget = 1;

    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        List<CharacterView> targets = new List<CharacterView>(CharacterSystem.Instance.Enemies
            .OrderBy(character => character.Character.HealthController.CurrentHp)
            .Take(_countTarget).ToList());

        return targets;
    }
}
