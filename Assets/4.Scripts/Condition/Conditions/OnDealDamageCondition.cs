using System;

/// <summary>
/// owner가 때릴 때
/// </summary>
public class OnDealDamageCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is DealDamageGA dealDamageGA)
        {
            // 공격 시 우리 편이고 owner도 우리편이면 그냥 true(파티원은 공유)
            if(dealDamageGA.Caster.Character.BattleSideType == EBattleSideType.OurSide && dealDamageGA.Caster.Character.BattleSideType == _owner.Character.BattleSideType)
            {
                return true;
            }

            if(dealDamageGA.Caster == _owner)
            {
                return true;
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
