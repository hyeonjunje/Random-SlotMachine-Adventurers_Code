using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ArtifactBehavior_FillMana : ArtifactBehavior
{
    [SerializeField] private float _amount = 1f;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (Mathf.Approximately(_amount, 0f))
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        triggerGA.AddEffect(new FillManaGA(_amount));
        ActionSystem.Instance.AddReaction(triggerGA);
    }
}

[Serializable]
public class ArtifactBehavior_HealByStatusCount : ArtifactBehavior
{
    [SerializeField] private EStatusCategory _statusCategory = EStatusCategory.Debuff;
    [SerializeField] private float _ratio = 1f;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (caster == null || targets == null || targets.Count == 0)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        HashSet<HealthController> visited = new HashSet<HealthController>();

        foreach (CharacterView target in targets)
        {
            if (target?.Character?.HealthController == null || !visited.Add(target.Character.HealthController))
            {
                continue;
            }

            int statusCount = 0;
            foreach (Status status in target.Character.GetStatusesByCategory(_statusCategory))
            {
                if (status != null)
                {
                    statusCount += Mathf.Max(0, status.RemainTurn);
                }
            }

            int amount = Mathf.RoundToInt(statusCount * _ratio);
            if (amount <= 0)
            {
                continue;
            }

            triggerGA.AddEffect(new ApplyHealingGA(
                caster,
                new List<CharacterView> { target },
                new HealingFormula(EHealingFormulaType.Flat, amount)));
        }

        if (triggerGA.Effects.Count > 0)
        {
            ActionSystem.Instance.AddReaction(triggerGA);
        }
    }
}

[Serializable]
public class ArtifactBehavior_DamageByCurrentShield : ArtifactBehavior
{
    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        int shield = caster?.Character?.HealthController?.Shield ?? CharacterSystem.Instance?.PartyHealth?.Shield ?? 0;
        if (shield <= 0 || CharacterSystem.Instance == null)
        {
            return;
        }

        List<CharacterView> enemyTargets = CharacterSystem.Instance.Enemies
            .Where(enemy => enemy != null && !enemy.Character.IsDead)
            .Cast<CharacterView>()
            .ToList();

        if (enemyTargets.Count == 0)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        triggerGA.AddEffect(new DealDamageGA(
            caster ?? ArtifactExecutionContext.GetDefaultCaster(owner),
            enemyTargets,
            new DamageFormula(EDamageFormulaType.Flat, shield)));

        ActionSystem.Instance.AddReaction(triggerGA);
    }
}

[Serializable]
public class ArtifactBehavior_GainRandomArtifact : ArtifactBehavior
{
    [SerializeField] private int _count = 1;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (ArtifactSystem.Instance == null)
        {
            return;
        }

        List<SO_ArtifactData> candidates = ArtifactSystem.Instance.GetRandomUnownedArtifacts(
            Mathf.Max(1, _count),
            data => data != null &&
                    data.OwnerJob == EPlayerJob.None &&
                    ArtifactSystem.Instance.HasPool(data, EArtifactPool.Special));

        if (candidates.Count == 0)
        {
            candidates = ArtifactSystem.Instance.GetRandomRewardArtifacts(Mathf.Max(1, _count));
        }

        foreach (SO_ArtifactData data in candidates)
        {
            if (data != null)
            {
                ArtifactSystem.Instance.AddArtifact(data.ID);
            }
        }
    }
}

[Serializable]
public class ArtifactBehavior_AddBuffNextTurn : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private EStatType _statType = EStatType.AttackPower;
    [SerializeField] private float _value = 0f;
    [SerializeField] private EStatModType _modType = EStatModType.Add;

    [NonSerialized] private Artifact _ownerArtifact;
    [NonSerialized] private bool _pending;
    [NonSerialized] private bool _active;
    [NonSerialized] private Action<StartTurnGA> _startTurnHandler;
    [NonSerialized] private Action<EndTurnGA> _endTurnHandler;
    [NonSerialized] private Action<ClearNodeGA> _clearNodeHandler;

    public void OnRegister(Artifact owner)
    {
        _ownerArtifact = owner;
        _startTurnHandler = _ => ApplyPendingBuff();
        _endTurnHandler = _ => RemoveActiveBuff();
        _clearNodeHandler = _ => RemoveActiveBuff();

        ActionSystem.SubscribeReaction<StartTurnGA>(_startTurnHandler, EReactionTiming.Post);
        ActionSystem.SubscribeReaction<EndTurnGA>(_endTurnHandler, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<ClearNodeGA>(_clearNodeHandler, EReactionTiming.Pre);
    }

    public void OnUnregister(Artifact owner)
    {
        RemoveActiveBuff();

        if (_startTurnHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartTurnGA>(_startTurnHandler, EReactionTiming.Post);
            _startTurnHandler = null;
        }

        if (_endTurnHandler != null)
        {
            ActionSystem.UnSubscribeReaction<EndTurnGA>(_endTurnHandler, EReactionTiming.Pre);
            _endTurnHandler = null;
        }

        if (_clearNodeHandler != null)
        {
            ActionSystem.UnSubscribeReaction<ClearNodeGA>(_clearNodeHandler, EReactionTiming.Pre);
            _clearNodeHandler = null;
        }

        _pending = false;
        _ownerArtifact = null;
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        _pending = true;
    }

    private void ApplyPendingBuff()
    {
        if (!_pending)
        {
            return;
        }

        CharacterView ownerView = ArtifactExecutionContext.GetOwnerView(_ownerArtifact) ?? ArtifactExecutionContext.GetDefaultCaster(_ownerArtifact);
        if (ownerView == null)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(_ownerArtifact);
        triggerGA.AddEffect(new ChangeStatValueGA(_statType, _modType, _value, new List<CharacterView> { ownerView }, ownerView));
        ActionSystem.Instance.AddReaction(triggerGA);

        _pending = false;
        _active = true;
    }

    private void RemoveActiveBuff()
    {
        if (!_active)
        {
            return;
        }

        CharacterView ownerView = ArtifactExecutionContext.GetOwnerView(_ownerArtifact) ?? ArtifactExecutionContext.GetDefaultCaster(_ownerArtifact);
        if (ownerView != null)
        {
            TriggerArtifactGA triggerGA = new TriggerArtifactGA(_ownerArtifact);
            triggerGA.AddEffect(new ChangeStatValueGA(_statType, _modType, -_value, new List<CharacterView> { ownerView }, ownerView));
            ActionSystem.Instance.AddReaction(triggerGA);
        }

        _active = false;
    }
}

[Serializable]
public class ArtifactBehavior_IncreaseStatWithCap : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private EStatType _statType = EStatType.AttackPower;
    [SerializeField] private float _value = 1f;
    [SerializeField] private EStatModType _modType = EStatModType.Add;
    [SerializeField] private int _cap = 10;

    [NonSerialized] private Artifact _ownerArtifact;
    [NonSerialized] private int _appliedCount;
    [NonSerialized] private Action<StartBattleGA> _startBattleHandler;
    [NonSerialized] private Action<ClearNodeGA> _clearNodeHandler;

    public void OnRegister(Artifact owner)
    {
        _ownerArtifact = owner;
        _startBattleHandler = _ => ResetAppliedBonus();
        _clearNodeHandler = _ => ResetAppliedBonus();

        ActionSystem.SubscribeReaction<StartBattleGA>(_startBattleHandler, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<ClearNodeGA>(_clearNodeHandler, EReactionTiming.Pre);
    }

    public void OnUnregister(Artifact owner)
    {
        ResetAppliedBonus();

        if (_startBattleHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartBattleGA>(_startBattleHandler, EReactionTiming.Pre);
            _startBattleHandler = null;
        }

        if (_clearNodeHandler != null)
        {
            ActionSystem.UnSubscribeReaction<ClearNodeGA>(_clearNodeHandler, EReactionTiming.Pre);
            _clearNodeHandler = null;
        }

        _ownerArtifact = null;
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (_appliedCount >= Mathf.Max(0, _cap))
        {
            return;
        }

        CharacterView ownerView = ArtifactExecutionContext.GetOwnerView(owner) ?? ArtifactExecutionContext.GetDefaultCaster(owner);
        if (ownerView == null)
        {
            return;
        }

        _appliedCount++;

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        triggerGA.AddEffect(new ChangeStatValueGA(_statType, _modType, _value, new List<CharacterView> { ownerView }, ownerView));
        ActionSystem.Instance.AddReaction(triggerGA);
    }

    private void ResetAppliedBonus()
    {
        if (_appliedCount <= 0 || _ownerArtifact == null)
        {
            _appliedCount = 0;
            return;
        }

        CharacterView ownerView = ArtifactExecutionContext.GetOwnerView(_ownerArtifact) ?? ArtifactExecutionContext.GetDefaultCaster(_ownerArtifact);
        if (ownerView != null)
        {
            TriggerArtifactGA triggerGA = new TriggerArtifactGA(_ownerArtifact);
            triggerGA.AddEffect(new ChangeStatValueGA(
                _statType,
                _modType,
                -(_value * _appliedCount),
                new List<CharacterView> { ownerView },
                ownerView));
            ActionSystem.Instance.AddReaction(triggerGA);
        }

        _appliedCount = 0;
    }
}

[Serializable]
public class ArtifactBehavior_DamageByStatusStack : ArtifactBehavior
{
    [SerializeField] private EStatusType _statusType = EStatusType.Poison;
    [SerializeField] private float _multiplier = 1f;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        foreach (CharacterView target in targets)
        {
            if (target?.Character == null || target.Character.IsDead)
            {
                continue;
            }

            int stack = target.Character.GetStatus(_statusType);
            int damage = Mathf.RoundToInt(stack * _multiplier);
            if (damage <= 0)
            {
                continue;
            }

            triggerGA.AddEffect(new DealDamageGA(
                caster ?? ArtifactExecutionContext.GetDefaultCaster(owner),
                new List<CharacterView> { target },
                new DamageFormula(EDamageFormulaType.Flat, damage)));
        }

        if (triggerGA.Effects.Count > 0)
        {
            ActionSystem.Instance.AddReaction(triggerGA);
        }
    }
}

[Serializable]
public class ArtifactBehavior_GoldRewardPercentPerStack : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _percentPerStack = 1f;
    [SerializeField] private int _maxStack = 30;

    [NonSerialized] private int _stack;
    [NonSerialized] private Action<StartBattleGA> _startBattleHandler;
    [NonSerialized] private Action<GrantPostBattleGoldGA> _goldHandler;

    public void OnRegister(Artifact owner)
    {
        _startBattleHandler = _ => _stack = 0;
        _goldHandler = ModifyGoldReward;
        ActionSystem.SubscribeReaction<StartBattleGA>(_startBattleHandler, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<GrantPostBattleGoldGA>(_goldHandler, EReactionTiming.Pre);
    }

    public void OnUnregister(Artifact owner)
    {
        if (_startBattleHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartBattleGA>(_startBattleHandler, EReactionTiming.Pre);
            _startBattleHandler = null;
        }

        if (_goldHandler != null)
        {
            ActionSystem.UnSubscribeReaction<GrantPostBattleGoldGA>(_goldHandler, EReactionTiming.Pre);
            _goldHandler = null;
        }

        _stack = 0;
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        _stack = Mathf.Min(Mathf.Max(0, _maxStack), _stack + 1);
    }

    private void ModifyGoldReward(GrantPostBattleGoldGA goldGA)
    {
        if (goldGA == null || _stack <= 0)
        {
            return;
        }

        goldGA.reward = Mathf.RoundToInt(goldGA.reward * (1f + (_percentPerStack / 100f * _stack)));
    }
}

[Serializable]
public class ArtifactBehavior_RandomHealOrDamageSelf : ArtifactBehavior
{
    [SerializeField, Range(0f, 100f)] private float _healChancePercent = 50f;
    [SerializeField] private int _healAmount = 5;
    [SerializeField] private int _damageAmount = 3;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        CharacterView target = ArtifactExecutionContext.GetOwnerView(owner) ?? caster ?? ArtifactExecutionContext.GetDefaultCaster(owner);
        if (target == null)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        if (ArtifactRuntimeState.RollChance(_healChancePercent))
        {
            triggerGA.AddEffect(new ApplyHealingGA(
                target,
                new List<CharacterView> { target },
                new HealingFormula(EHealingFormulaType.Flat, _healAmount)));
        }
        else
        {
            triggerGA.AddEffect(new DealDamageGA(
                target,
                new List<CharacterView> { target },
                new DamageFormula(EDamageFormulaType.Flat, _damageAmount)));
        }

        ActionSystem.Instance.AddReaction(triggerGA);
    }
}

[Serializable]
public class ArtifactBehavior_ScheduleNextTurnDamageWithHpCost : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _hpCost = 3;
    [SerializeField] private int _damage = 10;

    [NonSerialized] private Artifact _ownerArtifact;
    [NonSerialized] private bool _pending;
    [NonSerialized] private Action<StartTurnGA> _startTurnHandler;
    [NonSerialized] private Action<ClearNodeGA> _clearNodeHandler;

    public void OnRegister(Artifact owner)
    {
        _ownerArtifact = owner;
        _startTurnHandler = _ => ExecutePending();
        _clearNodeHandler = _ => _pending = false;
        ActionSystem.SubscribeReaction<StartTurnGA>(_startTurnHandler, EReactionTiming.Post);
        ActionSystem.SubscribeReaction<ClearNodeGA>(_clearNodeHandler, EReactionTiming.Pre);
    }

    public void OnUnregister(Artifact owner)
    {
        if (_startTurnHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartTurnGA>(_startTurnHandler, EReactionTiming.Post);
            _startTurnHandler = null;
        }

        if (_clearNodeHandler != null)
        {
            ActionSystem.UnSubscribeReaction<ClearNodeGA>(_clearNodeHandler, EReactionTiming.Pre);
            _clearNodeHandler = null;
        }

        _pending = false;
        _ownerArtifact = null;
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        _pending = true;
    }

    private void ExecutePending()
    {
        if (!_pending || CharacterSystem.Instance == null)
        {
            return;
        }

        CharacterView caster = ArtifactExecutionContext.GetOwnerView(_ownerArtifact) ?? ArtifactExecutionContext.GetDefaultCaster(_ownerArtifact);
        List<CharacterView> enemies = CharacterSystem.Instance.Enemies
            .Where(enemy => enemy != null && !enemy.Character.IsDead)
            .Cast<CharacterView>()
            .ToList();

        if (caster == null || enemies.Count == 0)
        {
            _pending = false;
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(_ownerArtifact);
        triggerGA.AddEffect(new DealDamageGA(
            caster,
            new List<CharacterView> { caster },
            new DamageFormula(EDamageFormulaType.Flat, _hpCost)));
        triggerGA.AddEffect(new DealDamageGA(
            caster,
            enemies,
            new DamageFormula(EDamageFormulaType.Flat, _damage)));

        ActionSystem.Instance.AddReaction(triggerGA);
        _pending = false;
    }
}

[Serializable]
public class ArtifactBehavior_DestroyAfterBattleClears : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _battleCount = 1;

    [NonSerialized] private Artifact _ownerArtifact;
    [NonSerialized] private int _remainingBattles;
    [NonSerialized] private Action<ClearBattleGA> _clearBattleHandler;

    public void OnRegister(Artifact owner)
    {
        _ownerArtifact = owner;
        _remainingBattles = Mathf.Max(1, _battleCount);
        _clearBattleHandler = _ => TickBattle();
        ActionSystem.SubscribeReaction<ClearBattleGA>(_clearBattleHandler, EReactionTiming.Post);
    }

    public void OnUnregister(Artifact owner)
    {
        if (_clearBattleHandler != null)
        {
            ActionSystem.UnSubscribeReaction<ClearBattleGA>(_clearBattleHandler, EReactionTiming.Post);
            _clearBattleHandler = null;
        }

        _ownerArtifact = null;
        _remainingBattles = 0;
    }

    private void TickBattle()
    {
        if (_ownerArtifact == null)
        {
            return;
        }

        _remainingBattles--;
        if (_remainingBattles <= 0 && ArtifactSystem.Instance != null)
        {
            ArtifactSystem.Instance.RemoveArtifact(_ownerArtifact);
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyGreatSuccessProbabilityMultiplier : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _multiplier = 1f;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.GreatSuccessProbabilityMultiplier *= Mathf.Max(0f, _multiplier);
        EventBus.Publish(new StSlotMachineProbabilityChangedEvent());
    }

    public void OnUnregister(Artifact owner)
    {
        float divisor = Mathf.Max(0.0001f, _multiplier);
        ArtifactRuntimeState.GreatSuccessProbabilityMultiplier /= divisor;
        EventBus.Publish(new StSlotMachineProbabilityChangedEvent());
    }
}

[Serializable]
public class ArtifactBehavior_UpgradeGreatSuccessToUltraChance : ArtifactBehavior
{
    [SerializeField, Range(0f, 100f)] private float _chancePercent = 50f;

    public override void ModifyAction(GameAction action)
    {
        if (action is SpinSlotMachineGA spinSlotMachineGA &&
            spinSlotMachineGA.SuccessType == ESlotMachineSuccessType.GreatSuccess &&
            ArtifactRuntimeState.RollChance(_chancePercent))
        {
            spinSlotMachineGA.SetSuccessType(ESlotMachineSuccessType.UltraSuccess);
        }
    }
}
