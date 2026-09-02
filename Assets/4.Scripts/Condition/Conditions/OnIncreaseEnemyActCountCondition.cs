using System;
using UnityEngine;

[Serializable]
public class OnIncreaseEnemyActCountCondition : Condition
{
    [SerializeField] private bool _onlyIncrease = true;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not ChangeEnemyActCountGA changeEnemyActCountGA)
        {
            return false;
        }

        return !_onlyIncrease || changeEnemyActCountGA.ActCountDiff > 0;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<ChangeEnemyActCountGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<ChangeEnemyActCountGA>(reaction, _reactionTiming);
    }
}
