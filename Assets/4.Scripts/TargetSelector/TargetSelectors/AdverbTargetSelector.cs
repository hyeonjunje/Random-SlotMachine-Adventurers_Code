using System.Collections.Generic;
using UnityEngine;

public class AdverbTargetSelector : TargetSelector
{
    private bool _isAttack = true;

    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        if(_isAttack) // 최근 공격 스킬의 타겟
        {
            return new List<CharacterView>(BattleSystem.Instance.RecentlyTargets);
        }
        else // 아니면 전투 타겟
        {
            if(BattleSystem.Instance.CurrentTargets.Count != 0)
            {
                return new List<CharacterView>() { BattleSystem.Instance.CurrentTargets[0] };
            }
            else
            {
                return new List<CharacterView>() { null };
            }
        }
    }

    public void SetTargetType(bool isAttack)
    {
        _isAttack = isAttack;
    }
}
