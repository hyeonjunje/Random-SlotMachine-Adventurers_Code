using System;
using UnityEngine;

[Serializable]
public class AccumulatedStatusAppliedInTurnCondition : Condition
{
    [SerializeField] private EStatusType _statusType;
    [SerializeField] private int _threshold = 1;
    [SerializeField] private EBattleSideType _targetSide = EBattleSideType.EnemySide;
    [SerializeField] private bool _oncePerTurn = true;

    private int _lastTurn = -1;
    private int _accumulatedAmount = 0;
    private int _lastTriggeredTurn = -1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not AddStatusGA addStatusGA)
        {
            return false;
        }

        SyncTurn();

        if (addStatusGA.Status == null || addStatusGA.Status.StatusType != _statusType)
        {
            return false;
        }

        int matchedTargetCount = 0;
        foreach (CharacterView target in addStatusGA.Targets)
        {
            if (target != null && target.Character.BattleSideType == _targetSide)
            {
                matchedTargetCount++;
            }
        }

        if (matchedTargetCount == 0)
        {
            return false;
        }

        _accumulatedAmount += Mathf.Max(0, addStatusGA.Turn) * matchedTargetCount;
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
        ActionSystem.SubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
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
