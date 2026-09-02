using System;

[Serializable]
public class OnRerollSlotMachineCondition : Condition
{
    private Action<RerollSlotMachineKeywordAddTokenGA> _addTokenHandler;
    private Action<RerollSlotMachineKeywordAddTokenInBattlePhaseGA> _battlePhaseHandler;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _addTokenHandler = ga => reaction?.Invoke(ga);
        _battlePhaseHandler = ga => reaction?.Invoke(ga);

        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(_addTokenHandler, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(_battlePhaseHandler, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        if (_addTokenHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(_addTokenHandler, _reactionTiming);
            _addTokenHandler = null;
        }

        if (_battlePhaseHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(_battlePhaseHandler, _reactionTiming);
            _battlePhaseHandler = null;
        }
    }
}
