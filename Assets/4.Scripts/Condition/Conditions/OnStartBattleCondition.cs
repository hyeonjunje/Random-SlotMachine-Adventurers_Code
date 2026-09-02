using System;

/// <summary>
/// 전투 시작 시 Condition
/// </summary>
public class OnStartBattleCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<StartBattleGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<StartBattleGA>(reaction, _reactionTiming);
    }
}
