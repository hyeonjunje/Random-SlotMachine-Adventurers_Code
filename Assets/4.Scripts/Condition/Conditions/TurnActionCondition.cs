using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class TurnActionCondition : Condition
{
    [Header ("조건 설정")]
    [Tooltip ("몇 번째 턴에 발동할지 (0이면 모든 턴)")]
    [SerializeField] private int _targetTurn = 0;

    [Tooltip ("몇 번째 행동에 발동할지 (비워두면 모든 행동에 발동, [1] -> 첫타만, [2] -> 2타만)")]
    [SerializeField] private List<int> _targetActionIndexes;

    private Action<DealDamageGA> _damageHandler;

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _damageHandler = (dealDamageGA) =>
        {
            if (CheckCondition ())
            {
                reaction?.Invoke (dealDamageGA);
            }
        };

        ActionSystem.SubscribeReaction<DealDamageGA> (_damageHandler, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        if (_damageHandler != null)
        {
            ActionSystem.UnSubscribeReaction (_damageHandler, _reactionTiming);
        }

        _damageHandler = null;
    }

    public override bool SubConditionIsMet(GameAction gameAction) => true;

    private bool CheckCondition()
    {
        int currentTurn = BattleSystem.Instance.CurrentTurn;
        if (_targetTurn != 0 && currentTurn != _targetTurn)
        {
            return false;
        }

        if (_targetActionIndexes == null || _targetActionIndexes.Count == 0)
        {
            return true;
        }

        int partyActionCount = BattleSystem.Instance.CurrentTurnPartyActionCount;
        if (!_targetActionIndexes.Contains (partyActionCount))
        {
            return false;
        }

        return true;
    }
}