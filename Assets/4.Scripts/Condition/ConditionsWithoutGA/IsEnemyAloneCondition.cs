using System;

/// <summary>
/// 적이 혼자있을 때 조건
/// </summary>
public class IsEnemyAloneCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return CharacterSystem.Instance.Enemies.Count == 1;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}
