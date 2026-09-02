using System;

/// <summary>
/// Turn이 다 종료됐을 때 조건
/// </summary>
public class OnTurnEndCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<EndTurnGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<EndTurnGA>(reaction, _reactionTiming);
    }
}
