using System;
using UnityEngine;

public class OnDecreaseStatusCondition : Condition
{
    [SerializeField] private EStatusType _statusType;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is DecreaseStatusGA decreaseStatusGA)
        {
            if (decreaseStatusGA.Status.Owner == _owner && decreaseStatusGA.Status.StatusType == _statusType)
            {
                return true;
            }
        }
        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DecreaseStatusGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DecreaseStatusGA>(reaction, _reactionTiming);
    }
}
