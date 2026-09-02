using System;
using System.Collections;
using UnityEngine;

public class OnApplyAbilityCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if(gameAction is ApplyAbilityGA applyAbilityGA)
        {
            if(applyAbilityGA.Owner == _owner)
            {
                return true;
            }
        }

        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<ApplyAbilityGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<ApplyAbilityGA>(reaction, _reactionTiming);
    }
}