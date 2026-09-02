using System;
using UnityEngine;

/// <summary>
/// owner가 죽고 난 후 조건
/// </summary>

public class OnDieCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if(gameAction is EnemyDeadGA characterDeadGA)
        {
            if(characterDeadGA.Killed == _owner)
            {
                return true;
            }
        }
        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<EnemyDeadGA> (reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<EnemyDeadGA> (reaction, _reactionTiming);
    }
}
