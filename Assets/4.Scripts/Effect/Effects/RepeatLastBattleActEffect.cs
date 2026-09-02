using System.Collections.Generic;
using UnityEngine;

public class RepeatLastBattleActEffect : Effect
{
    [SerializeField] private int _repeatCount = 1;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        if (BattleSystem.Instance == null || BattleSystem.Instance.LastExecutedPlayerBattleAct == null || _repeatCount <= 0)
        {
            return null;
        }

        return new RepeatLastBattleActGA(_repeatCount);
    }
}
