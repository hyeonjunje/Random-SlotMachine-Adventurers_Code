using System;
using UnityEngine;

public class OnApplyStatusCondition : Condition
{
    [SerializeField] private EStatusType _statusType;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if(gameAction is AddStatusGA addStatusGA)
        {
            for(int i = 0; i < addStatusGA.Targets.Count; ++i)
            {
                if(addStatusGA.Targets[i] == _owner && addStatusGA.Status.StatusType == _statusType)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }
}
