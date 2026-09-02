using System;

public class OnCharacterActCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is ActAutoBattleGA actAutoBattleGA)
        {
            if(actAutoBattleGA.BattleAct.CharacterView == _owner)
            {
                return true;
            }
        }
        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<ActAutoBattleGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<ActAutoBattleGA>(reaction, _reactionTiming);
    }
}
