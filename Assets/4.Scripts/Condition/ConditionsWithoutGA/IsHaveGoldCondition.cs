using System;
using UnityEngine;

/// <summary>
/// 골드가 _needGold만큼 있는지 조건
/// </summary>
public class IsHaveGoldCondition : Condition
{
    [SerializeField] private int _needGold;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return UIHudSystem.Instance.CanPayGold(_needGold);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}
