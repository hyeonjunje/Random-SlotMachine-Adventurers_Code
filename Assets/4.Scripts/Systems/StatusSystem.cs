using System.Collections;
using System.Collections.Generic;

public class StatusSystem : SingletonScene<StatusSystem>
{
    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();

        ActionSystem.AttachPerformer<AddStatusGA>(AddStatusPerformer);
        ActionSystem.AttachPerformer<RemoveStatusGA>(RemoveStatusPerformer);
        ActionSystem.AttachPerformer<RemoveStatusByCategoryGA>(RemoveStatusByCategoryPerformer);
        ActionSystem.AttachPerformer<RefreshStatusGA>(RefreshStatusPerformer);
        ActionSystem.AttachPerformer<DecreaseStatusGA>(DecreaseStatusPerformer);

        ActionSystem.AttachPerformer<ChangeStatValueGA>(ChangeStatValuePerformer);

        ActionSystem.AttachPerformer<AddDelayedStatusGA>(AddDelayedStatusPerformer);
        ActionSystem.AttachPerformer<ApplyDelayedStatusGA>(ApplyDelayedStatusPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<AddStatusGA>();
        ActionSystem.DetachPerformer<RemoveStatusGA>();
        ActionSystem.DetachPerformer<RemoveStatusByCategoryGA>();
        ActionSystem.DetachPerformer<RefreshStatusGA>();
        ActionSystem.DetachPerformer<DecreaseStatusGA>();

        ActionSystem.DetachPerformer<ChangeStatValueGA>();

        ActionSystem.DetachPerformer<AddDelayedStatusGA>();
        ActionSystem.DetachPerformer<ApplyDelayedStatusGA>();
    }

    private IEnumerator AddStatusPerformer(AddStatusGA addStatusGA)
    {
        yield return null;

        if (addStatusGA.IsBlocked)
        {
            yield break;
        }

        foreach (CharacterView target in addStatusGA.Targets)
        {
            // 사냥감 상태이상이 있을 경우 표식을 못쌓음
            if (target.Character.IsStatus(EStatusType.Prey) && addStatusGA.Status.StatusType == EStatusType.Marking)
            {
                continue;
            }

            Status status = new Status(addStatusGA.Status, addStatusGA.Turn, target, addStatusGA.Caster);
            target.AddStatus(addStatusGA.Caster, status);

            // 표식이 5스택 이상이 될 경우 표식 상태이상을 제거하고 사냥감 상태이상을 보유한다.
            if (target.Character.GetStatus(EStatusType.Marking) >= 5)
            {
                RemoveStatusGA removeStatusGA = new RemoveStatusGA(target.Character.StatusController.Statuses[EStatusType.Marking], new List<CharacterView> { target }, addStatusGA.Caster);
                ActionSystem.Instance.AddReaction(removeStatusGA);

                AddStatusGA extraAddStatusGA = new AddStatusGA(DataManager.Instance.GetStatus(EStatusType.Prey), 1, new List<CharacterView> { target }, addStatusGA.Caster);
                ActionSystem.Instance.AddReaction(extraAddStatusGA);
            }

            // 마비 상태가 아니고 감전이 5스택 이상일 시 마비 보유
            if (target.Character.GetStatus(EStatusType.Electric) >= 5 && target.Character.IsStatus(EStatusType.Paralysis) == false)
            {
                AddStatusGA extraAddStatusGA = new AddStatusGA(DataManager.Instance.GetStatus(EStatusType.Paralysis), 1, new List<CharacterView> { target }, addStatusGA.Caster);
                ActionSystem.Instance.AddReaction(extraAddStatusGA);
            }
        }
    }

    private IEnumerator RemoveStatusPerformer(RemoveStatusGA removeStatusGA)
    {
        yield return null;

        foreach (CharacterView target in removeStatusGA.Targets)
        {
            target.RemoveStatus(removeStatusGA.Caster, removeStatusGA.Status.StatusType);

            foreach (StatusEffect statusEffect in removeStatusGA.Status.StatusEffects)
            {
                if (statusEffect.ReleaseEffect != null)
                {
                    PerformEffectGA performEffectGA = new PerformEffectGA(statusEffect.ReleaseEffect, statusEffect.Effect.TargetSelector.SelectTarget(target), removeStatusGA.Caster);
                    ActionSystem.Instance.AddReaction(performEffectGA);
                }
            }
        }
    }

    private IEnumerator RemoveStatusByCategoryPerformer(RemoveStatusByCategoryGA removeStatusByCategoryGA)
    {
        yield return null;

        foreach (CharacterView target in removeStatusByCategoryGA.Targets)
        {
            List<Status> statusesByCategory = target.Character.GetStatusesByCategory(removeStatusByCategoryGA.StatusCategory);
            statusesByCategory.Shuffle();

            for (int i = 0; i < removeStatusByCategoryGA.RemoveCount; ++i)
            {
                if (i < statusesByCategory.Count)
                {
                    RemoveStatusGA removeStatusGA = new RemoveStatusGA(statusesByCategory[i], new List<CharacterView> { target }, removeStatusByCategoryGA.Caster);
                    ActionSystem.Instance.AddReaction(removeStatusGA);
                }
            }
        }
    }

    private IEnumerator RefreshStatusPerformer(RefreshStatusGA refreshStatusGA)
    {
        yield return null;

        foreach (CharacterView target in refreshStatusGA.Targets)
        {
            target.UpdateStatus(refreshStatusGA.Status);
        }
    }

    private IEnumerator DecreaseStatusPerformer(DecreaseStatusGA DecreaseStatusGA)
    {
        yield return null;

        DecreaseStatusGA.Status.DecreaseTurn();
    }

    private IEnumerator ChangeStatValuePerformer(ChangeStatValueGA ga)
    {
        yield return null;

        // 마나
        if (ga.StatType == EStatType.MaxMana)
        {
            if (ga.Targets != null && ga.Targets.Count > 0)
            {
                float amount = ga.ModType == EStatModType.Add
                    ? ga.Value
                    : ManaSystem.Instance.MaxMana * ga.Value;

                if (amount > 0f)
                {
                    ActionSystem.Instance.AddReaction(new FillManaGA(amount));
                }
                else if (amount < 0f)
                {
                    ActionSystem.Instance.AddReaction(new SpendManaGA(-amount));
                }

                yield break;
            }

            float currentSystemMaxMana = ManaSystem.Instance.MaxMana;
            float amountToAdd = 0f;

            if (ga.ModType == EStatModType.Add)
            {
                amountToAdd = ga.Value;
            }
            else
            {
                amountToAdd = currentSystemMaxMana * ga.Value;
            }

            if (UnityEngine.Mathf.Abs(amountToAdd) > 0.01f)
            {
                ManaSystem.Instance.ChangeMaxMana(amountToAdd);
            }
        }
        // 체력 
        else if (ga.StatType == EStatType.MaxHp)
        {
            bool isTargetEnemy = (ga.Targets != null && ga.Targets.Count > 0 && ga.Targets[0] is EnemyView);

            if (isTargetEnemy)
            {
                foreach (CharacterView target in ga.Targets)
                {
                    if (target is EnemyView enemy)
                    {
                        var stat = enemy.Character.GetStat(EStatType.MaxHp);
                        float prevVal = stat.Value;
                        stat.AddModifier(ga.ModType, ga.Value); // Stat 변경
                        float currentVal = stat.Value;
                        enemy.Character.HealthController.ChangeMaxHp((int)(currentVal - prevVal));
                    }
                }
            }
            else
            {
                var partyHealth = CharacterSystem.Instance.PartyHealth;
                float currentSystemMaxHp = partyHealth.MaxHp;
                float amountToAdd = 0f;

                if (ga.ModType == EStatModType.Add)
                {
                    amountToAdd = ga.Value;
                }
                else
                {
                    amountToAdd = currentSystemMaxHp * ga.Value;
                }

                if (UnityEngine.Mathf.Abs(amountToAdd) >= 1f)
                {
                    partyHealth.ChangeMaxHp((int)amountToAdd);
                }
            }
        }
        // 기타 스탯 
        else
        {
            if (ga.Targets == null) yield break;

            foreach (CharacterView target in ga.Targets)
            {
                target.Character.GetStat(ga.StatType)?.AddModifier(ga.ModType, ga.Value);
            }
        }

        foreach (CharacterView target in ga.Targets)
        {
            target.Character.OnDataChanged?.Invoke();
        }
    }

    private IEnumerator AddDelayedStatusPerformer(AddDelayedStatusGA addDelayedStatusGA)
    {
        foreach(CharacterView target in addDelayedStatusGA.Targets)
        {
            SO_StatusData statusData = DataManager.Instance.GetStatus(addDelayedStatusGA.StatusType);
            Status delayedStatus = new Status(statusData, addDelayedStatusGA.Value, target, null);
            target.Character.StatusController.AddDelayedStatus(delayedStatus);
        }

        yield return null;
    }

    private IEnumerator ApplyDelayedStatusPerformer(ApplyDelayedStatusGA applyDelayedStatusGA)
    {
        // 우리편
        foreach(Status delayedStatus in CharacterSystem.Instance.PartyStatusController.DelayedStatus)
        {
            List<CharacterView> targets = new List<CharacterView>() { CharacterSystem.Instance.Players[0] };
            SO_StatusData statusData = DataManager.Instance.GetStatus(delayedStatus.StatusType);
            AddStatusGA addStatusGA = new AddStatusGA(statusData, delayedStatus.RemainTurn, targets, null);
            ActionSystem.Instance.AddReaction(addStatusGA);
        }

        CharacterSystem.Instance.PartyStatusController.ClearDelayedStatus();

        // 적편
        foreach(CharacterView enemy in CharacterSystem.Instance.Enemies)
        {
            foreach (Status delayedStatus in enemy.Character.StatusController.DelayedStatus)
            {
                List<CharacterView> targets = new List<CharacterView>() { enemy };
                SO_StatusData statusData = DataManager.Instance.GetStatus(delayedStatus.StatusType);
                AddStatusGA addStatusGA = new AddStatusGA(statusData, delayedStatus.RemainTurn, targets, null);
                ActionSystem.Instance.AddReaction(addStatusGA);
            }

            enemy.Character.StatusController.ClearDelayedStatus();
        }

        yield return null;
    }
}

