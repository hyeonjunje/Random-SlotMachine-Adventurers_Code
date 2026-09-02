using System;
using UnityEngine;

[Serializable]
public class AccumulatedEnemyActCountIncreaseInTurnCondition : Condition
{
    [SerializeField] private int _threshold = 1;
    [SerializeField] private bool _oncePerTurn = true;

    private int _lastTurn = -1;
    private int _accumulatedAmount = 0;
    private int _lastTriggeredTurn = -1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not ChangeEnemyActCountGA changeEnemyActCountGA)
        {
            return false;
        }

        SyncTurn();

        int diff = Mathf.Max(0, changeEnemyActCountGA.ActCountDiff);
        if (diff <= 0 || changeEnemyActCountGA.Targets == null || changeEnemyActCountGA.Targets.Count == 0)
        {
            return false;
        }

        _accumulatedAmount += diff * changeEnemyActCountGA.Targets.Count;
        if (_accumulatedAmount < _threshold)
        {
            return false;
        }

        if (_oncePerTurn && _lastTriggeredTurn == _lastTurn)
        {
            return false;
        }

        _lastTriggeredTurn = _lastTurn;
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<ChangeEnemyActCountGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<ChangeEnemyActCountGA>(reaction, _reactionTiming);
    }

    private void SyncTurn()
    {
        int currentTurn = BattleSystem.Instance != null ? BattleSystem.Instance.CurrentTurn : 0;
        if (_lastTurn == currentTurn)
        {
            return;
        }

        _lastTurn = currentTurn;
        _accumulatedAmount = 0;
        _lastTriggeredTurn = -1;
    }
}
