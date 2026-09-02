using System;
using UnityEngine;

/// <summary>
/// Owner의 최대 Hp가 _flatHp보다 많은지 조건
/// </summary>
public class IsCheckMaxHpCondition : Condition
{
    [SerializeField, Header("최대체력 상수값")] public int _flatHp = 0;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        HealthController healthController = _owner.Character.HealthController;

        return healthController.MaxHp > _flatHp;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}
