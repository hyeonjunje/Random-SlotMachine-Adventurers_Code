using System.Collections.Generic;
using UnityEngine;

public class Status
{
    public string StatusName { get; private set; }
    public Sprite StatusSprite { get; private set; }
    public string StatusExplain { get; private set; }
    public EStatusType StatusType { get; private set; }
    public EStatusCategory StatusCategory { get; private set; }
    public bool IsSingleTurn { get; private set; }
    public bool IsStackable { get; private set; }
    public IReadOnlyCollection<StatusEffect> StatusEffects { get; private set; }
    public Condition StatusTriggerCondition { get; private set; }
    public Condition StatusExpireCondition { get; private set; }

    // Runtime Data
    public int RemainTurn { get; private set; }

    public CharacterView Caster { get; private set; } // 해당 Status를 건 사람
    public CharacterView Owner { get; private set; }  // 해당 Status를 가지고 있는 사람
    public HashSet<GameAction> AppliedDuringActions { get; private set; }

    public Status(SO_StatusData statusData, int turn, CharacterView owner, CharacterView caster)
    {
        Setup(statusData);
        RemainTurn = turn;
        Owner = owner;
        Caster = caster;
    }

    public void Setup(SO_StatusData statusData)
    {
        StatusName = LocalizationManager.Instance.Get(statusData.StatusName);
        StatusSprite = statusData.StatusSprite;
        StatusExplain = statusData.StatusExplain;
        StatusType = statusData.StatusType;
        StatusCategory = statusData.StatusCategory;
        IsSingleTurn = statusData.IsSingleTurn;
        IsStackable = statusData.IsStackable;
        StatusEffects = statusData.StatusEffects;
        StatusTriggerCondition = statusData.StatusTriggerCondition.Clone();
        StatusExpireCondition = statusData.StatusExpireCondition.Clone();
    }

    public void Add()
    {
        if (ActionSystem.Instance.ActiveActions != null)
        {
            AppliedDuringActions = new HashSet<GameAction>(ActionSystem.Instance.ActiveActions);
        }

        StatusExpireCondition.SetOwner(Owner);
        StatusExpireCondition.SubscribeCondition(DecreaseTurn);

        StatusTriggerCondition.SetOwner(Owner);
        StatusTriggerCondition.SubscribeCondition(ExcuteAction);
    }

    public void Release()
    {
        StatusExpireCondition.UnsubscribeCondition(DecreaseTurn);
        StatusTriggerCondition.UnsubscribeCondition(ExcuteAction);
    }

    public void AddTurn(int addTurn)
    {
        RemainTurn += addTurn;

        // 중첩불가능이면 최대 1로
        if (IsStackable == false)
        {
            RemainTurn = Mathf.Max(RemainTurn, 1);
        }
    }

    public void DecreaseTurn()
    {
        RemainTurn--;

        if (RemainTurn <= 0)
        {
            RemoveStatusGA removeStatusGA = new RemoveStatusGA(this, new List<CharacterView>() { Owner }, null);
            ActionSystem.Instance.AddReaction(removeStatusGA);
        }
        else
        {
            RefreshStatusGA refreshStatusGA = new RefreshStatusGA(this, new List<CharacterView> { Owner });
            ActionSystem.Instance.AddReaction(refreshStatusGA);
        }
    }

    private void DecreaseTurn(GameAction gameAction)
    {
        if (AppliedDuringActions != null && AppliedDuringActions.Contains(gameAction))
        {
            return;
        }

        if(StatusExpireCondition.SubConditionIsMet(gameAction))
        {
            DecreaseStatusGA decreaseStatusGA = new DecreaseStatusGA(this);
            ActionSystem.Instance.AddReaction(decreaseStatusGA);
        }
    }

    private void ExcuteAction(GameAction gameAction)
    {
        if(StatusTriggerCondition.SubConditionIsMet(gameAction))
        {
            foreach (StatusEffect statusEffect in StatusEffects)
            {
                PerformEffectGA performEffectGA = new PerformEffectGA(statusEffect.Effect, statusEffect.Effect.TargetSelector.SelectTarget(Owner), null);
                ActionSystem.Instance.AddReaction(performEffectGA);
            }
        }
    }
}