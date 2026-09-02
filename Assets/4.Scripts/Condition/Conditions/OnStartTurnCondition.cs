using System;
using UnityEngine;

/// <summary>
/// 턴 시작 후 드로우를 모두 마친 후
/// </summary>
public class OnStartTurnCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<StartTurnGA> (reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<StartTurnGA> (reaction, _reactionTiming);
    }
}
