using System;

[Serializable]
public class OnCounterAttackCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is DealDamage_CounterAttackGA;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamage_CounterAttackGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamage_CounterAttackGA>(reaction, _reactionTiming);
    }
}
