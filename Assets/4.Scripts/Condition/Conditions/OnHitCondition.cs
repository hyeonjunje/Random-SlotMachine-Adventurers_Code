using System;
using UnityEngine;

/// <summary>
/// owner가 맞고 난 후 조건
/// </summary>
public class OnHitCondition : Condition
{
    [Header("실제로 피해를 입었으면 true, 상관없으면 false")]
    [SerializeField] private bool _isDamageTaken = false;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if(gameAction is DealDamageGA dealDamageGA)
        {
            foreach (CharacterView target in dealDamageGA.Targets)
            {
                if (target == _owner || (target is PlayerView && _owner is PlayerView)) // 플레이어는 누가 맞든 같이 맞는다.
                {
                    if(_isDamageTaken)
                    {
                        return target.Character.GetExpectedDamage(dealDamageGA.Caster, dealDamageGA.DamageFormula) > 0;
                    }

                    return true;
                }
            }
        }
        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}
