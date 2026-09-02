using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRandomTargetSelector : TargetSelector
{
    [SerializeField, Tooltip("랜덤으로 고를 플레이어의 개수")] private int _randomPlayerCount = 1;

    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        List<CharacterView> result = new List<CharacterView>();

        result.AddRange(CharacterSystem.Instance.Players.Where(x => x.Character.IsDead == false).OrderBy(x => Guid.NewGuid())
            .Take(_randomPlayerCount)
            .ToList());

        return result;
    }
}
