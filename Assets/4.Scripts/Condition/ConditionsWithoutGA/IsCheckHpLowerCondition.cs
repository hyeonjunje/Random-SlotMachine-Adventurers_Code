using System;
using UnityEngine;

/// <summary>
/// Owner의 현재 체력이 일정 수준보다 낮은지 체크
/// </summary>
public class IsCheckHpLowerCondition : Condition
{

    [SerializeField, Header("체력 상수값")] public int _flatHp = 0;
    [SerializeField, Header("최대 체력의 n 퍼"), Range(0, 1)] private float _probability = 0;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        HealthController healthController = _owner.Character.HealthController;

        if (healthController.CurrentHp <= _flatHp || (healthController.CurrentHp / (float)healthController.MaxHp) <= _probability)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}
