using System;

/// <summary>
/// 전투 종료 시 Condition
/// </summary>
public class OnClearBattleCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<ClearBattleGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<ClearBattleGA>(reaction, _reactionTiming);
    }
}