using SerializeReferenceEditor;
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IArtifactBehaviorLifecycle
{
    void OnRegister(Artifact owner);
    void OnUnregister(Artifact owner);
}

public static class ArtifactRuntimeState
{
    public static int PlayerDamageTakenFlatModifier { get; set; } = 0;
    public static float PlayerDamageTakenMultiplier { get; set; } = 1f;
    public static int SlotClickRerollManaCostDelta { get; set; } = 0;
    public static int FirstTurnTemporaryFreeRerolls { get; set; } = 0;
    public static int DisableRerollCount { get; set; } = 0;
    public static int PlayerTokenMultiplier { get; set; } = 1;
    public static int CurrentTurnPlayerTokenMultiplier { get; private set; } = 1;
    public static float ClickRerollReintroduceChancePercent { get; set; } = 0f;
    public static int UpgradeAllSlotsOnNthRerollInterval { get; set; } = 0;
    public static float DamageOnRerollPartyAttackRatio { get; set; } = 0f;
    public static int DamageOnRerollTargetCount { get; set; } = 1;
    public static float GreatSuccessProbabilityMultiplier { get; set; } = 1f;
    public static int UniqueSlotEndTurnMarkStacks { get; set; } = 0;
    public static float GrowthPotionChancePercent { get; set; } = 0f;
    public static int GrowthPotionLevelDiff { get; set; } = 0;
    public static float NullifyWeakenedEnemyDamageChancePercent { get; set; } = 0f;
    public static bool PendingManaSpentReroll { get; private set; } = false;
    public static bool PartyReviveArmed { get; set; } = false;
    public static bool PartyReviveUsed { get; set; } = false;
    public static float PartyReviveRatio { get; set; } = 0f;
    public static float ShopPriceMultiplier { get; set; } = 1f;
    public static int SkipPlayerActionsTurn { get; private set; } = -1;
    public static int CurrentBattlePartyDamageTaken { get; private set; } = 0;

    private static readonly Dictionary<EMapNodeType, float> _mapNodeWeightDeltas = new Dictionary<EMapNodeType, float>();

    public static bool IsRerollDisabled => DisableRerollCount > 0;
    public static int EffectivePlayerTokenMultiplier => Mathf.Max(1, PlayerTokenMultiplier * CurrentTurnPlayerTokenMultiplier);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        ResetAll();
    }

    public static void ResetAll()
    {
        PlayerDamageTakenFlatModifier = 0;
        PlayerDamageTakenMultiplier = 1f;
        SlotClickRerollManaCostDelta = 0;
        FirstTurnTemporaryFreeRerolls = 0;
        DisableRerollCount = 0;
        PlayerTokenMultiplier = 1;
        CurrentTurnPlayerTokenMultiplier = 1;
        ClickRerollReintroduceChancePercent = 0f;
        UpgradeAllSlotsOnNthRerollInterval = 0;
        DamageOnRerollPartyAttackRatio = 0f;
        DamageOnRerollTargetCount = 1;
        GreatSuccessProbabilityMultiplier = 1f;
        UniqueSlotEndTurnMarkStacks = 0;
        GrowthPotionChancePercent = 0f;
        GrowthPotionLevelDiff = 0;
        NullifyWeakenedEnemyDamageChancePercent = 0f;
        PendingManaSpentReroll = false;
        PartyReviveArmed = false;
        PartyReviveUsed = false;
        PartyReviveRatio = 0f;
        ShopPriceMultiplier = 1f;
        SkipPlayerActionsTurn = -1;
        CurrentBattlePartyDamageTaken = 0;
        _mapNodeWeightDeltas.Clear();
    }

    public static int GetAdjustedSlotClickRerollManaCost(int baseCost)
    {
        return Mathf.Max(0, baseCost + SlotClickRerollManaCostDelta);
    }

    public static bool RollChance(float percent)
    {
        if (percent <= 0f)
        {
            return false;
        }

        if (percent >= 100f)
        {
            return true;
        }

        return UnityEngine.Random.Range(0f, 100f) < percent;
    }

    public static void MarkNextSpinAsManaSpentReroll()
    {
        PendingManaSpentReroll = true;
    }

    public static bool ConsumeManaSpentRerollFlag()
    {
        bool value = PendingManaSpentReroll;
        PendingManaSpentReroll = false;
        return value;
    }

    public static int GetAdjustedShopPrice(int basePrice)
    {
        return Mathf.Max(0, Mathf.RoundToInt(basePrice * ShopPriceMultiplier));
    }

    public static void MultiplyCurrentTurnPlayerTokens(int multiplier)
    {
        CurrentTurnPlayerTokenMultiplier *= Mathf.Max(1, multiplier);
    }

    public static void ResetTurnScopedState()
    {
        CurrentTurnPlayerTokenMultiplier = 1;
    }

    public static void ResetBattleScopedState()
    {
        CurrentBattlePartyDamageTaken = 0;
    }

    public static void AddPartyDamageTaken(int damage)
    {
        CurrentBattlePartyDamageTaken += Mathf.Max(0, damage);
    }

    public static void ScheduleSkipNextPlayerTurn()
    {
        if (BattleSystem.Instance == null)
        {
            return;
        }

        SkipPlayerActionsTurn = BattleSystem.Instance.CurrentTurn + 1;
    }

    public static bool ShouldSkipPlayerActionsThisTurn()
    {
        return BattleSystem.Instance != null &&
               SkipPlayerActionsTurn == BattleSystem.Instance.CurrentTurn;
    }

    public static void ConsumeSkipPlayerActionsTurn()
    {
        SkipPlayerActionsTurn = -1;
    }

    public static void AddMapNodeWeightDelta(EMapNodeType nodeType, float delta)
    {
        _mapNodeWeightDeltas.TryGetValue(nodeType, out float current);
        _mapNodeWeightDeltas[nodeType] = current + delta;
    }

    public static float GetMapNodeWeightDelta(EMapNodeType nodeType)
    {
        return _mapNodeWeightDeltas.TryGetValue(nodeType, out float delta) ? delta : 0f;
    }

    public static bool TryConsumeFirstTurnTemporaryFreeReroll()
    {
        if (FirstTurnTemporaryFreeRerolls <= 0)
        {
            return false;
        }

        FirstTurnTemporaryFreeRerolls--;
        return true;
    }

    public static void ArmPartyRevive(float ratio)
    {
        PartyReviveArmed = true;
        PartyReviveUsed = false;
        PartyReviveRatio = ratio;
    }

    public static void DisarmPartyRevive(float ratio)
    {
        if (!PartyReviveArmed || PartyReviveUsed)
        {
            return;
        }

        if (Mathf.Approximately(PartyReviveRatio, ratio))
        {
            PartyReviveArmed = false;
            PartyReviveRatio = 0f;
        }
    }

    public static bool TryConsumePartyRevive(out float ratio)
    {
        ratio = PartyReviveRatio;
        if (!PartyReviveArmed || PartyReviveUsed || ratio <= 0f)
        {
            return false;
        }

        PartyReviveUsed = true;
        PartyReviveArmed = false;
        return true;
    }
}

public static class ArtifactActionMath
{
    public static void MultiplyDamageFormula(DamageFormula formula, float multiplier)
    {
        if (formula == null)
        {
            return;
        }

        switch (formula.DamageFormulaType)
        {
            case EDamageFormulaType.AddPercentForAttackPower:
                formula.Value = ((1f + formula.Value) * multiplier) - 1f;
                break;
            default:
                formula.Value *= multiplier;
                break;
        }
    }

    public static void MultiplyHealingFormula(HealingFormula formula, float multiplier)
    {
        if (formula == null)
        {
            return;
        }

        switch (formula.HealingFormulaType)
        {
            case EHealingFormulaType.AddPercentForAttackPower:
                formula.Value = ((1f + formula.Value) * multiplier) - 1f;
                break;
            default:
                formula.Value *= multiplier;
                break;
        }
    }

    public static void MultiplyShieldFormula(ShieldFormula formula, float multiplier)
    {
        if (formula == null)
        {
            return;
        }

        switch (formula.ShieldFormulaType)
        {
            case EShieldFormulaType.AddPercentForAttackPower:
                formula.Value = ((1f + formula.Value) * multiplier) - 1f;
                break;
            default:
                formula.Value *= multiplier;
                break;
        }
    }

    public static DamageFormula CloneDamageFormula(DamageFormula formula)
    {
        if (formula == null)
        {
            return null;
        }

        return new DamageFormula(formula.DamageFormulaType, formula.Value)
        {
            IsIgnoresDefense = formula.IsIgnoresDefense
        };
    }
}

[Serializable]
public abstract class ArtifactBehavior
{
    // 스킬 발동
    public virtual void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets) { }

    // 스탯 적용 
    public virtual void OnApply(CharacterView target) { }

    // 스탯 해제
    public virtual void OnRemove(CharacterView target) { }

    // 행동 조작
    public virtual void ModifyAction(GameAction action) { }
}
[Serializable]
// Effect 발동하는 유물
public class ArtifactBehavior_UseEffect : ArtifactBehavior
{
    [SerializeReference, SR] public Effect Effect; 

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (Effect != null)
        {
            List<CharacterView> resolvedTargets = targets;
            if (Effect.TargetSelector != null)
            {
                resolvedTargets = Effect.TargetSelector.SelectTarget(caster);
            }

            GameAction action = Effect.GetGameAction (resolvedTargets, caster);
            if (action != null)
            {
                if (action is DealDamageGA damageGA)
                {
                    damageGA.MarkArtifactGenerated();
                }

                TriggerArtifactGA triggerGA = new TriggerArtifactGA (owner);
                triggerGA.AddEffect (action);
                ActionSystem.Instance.AddReaction (triggerGA);
            }
        }
    }
}

[Serializable]
public class ArtifactBehavior_SetGold : ArtifactBehavior
{
    [SerializeField] private int _amount = 0;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (UIHudSystem.Instance == null)
        {
            return;
        }

        UIHudSystem.Instance.SetGold(_amount);
    }
}

[Serializable]
public class ArtifactBehavior_ModifyDamage : ArtifactBehavior
{
    [SerializeField] private float _multiplier = 2.0f;

    public override void ModifyAction(GameAction action)
    {
        if (action is DealDamageGA damageGA)
        {
            ArtifactActionMath.MultiplyDamageFormula(damageGA.DamageFormula, _multiplier);

            Debug.Log ($"데미지 계수(Value)가 {_multiplier}배 되었습니다.");
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyEventWeight : ArtifactBehavior
{
    [Serializable]
    public struct WeightModifierData
    {
        public EEventRiskRewardType TargetType;
        public int AddAmount;
    }

    [SerializeField]
    private List<WeightModifierData> _modifiers = new List<WeightModifierData> ();

    public int OnModifyWeight(EEventRiskRewardType type, int currentWeight)
    {
        int finalWeight = currentWeight;

        foreach (var data in _modifiers)
        {
            if (data.TargetType == type)
            {
                finalWeight += data.AddAmount;
            }
        }

        return finalWeight;
    }
}

[Serializable]
public class ArtifactBehavior_UnlockDiagonal : ArtifactBehavior
{
    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        DataManager.Instance.GameModel.IsAllowDiagonal = true;
    }

    public override void OnRemove(CharacterView target)
    {
        if (DataManager.Instance != null && DataManager.Instance.GameModel != null)
        {
            DataManager.Instance.GameModel.IsAllowDiagonal = false;
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyGameModelFloat : ArtifactBehavior
{
    [SerializeField] private EArtifactGameModelFloatStat _stat;
    [SerializeField] private float _delta;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        Apply(_delta);
    }

    public override void OnRemove(CharacterView target)
    {
        Apply(-_delta);
    }

    private void Apply(float delta)
    {
        if (DataManager.Instance == null || DataManager.Instance.GameModel == null)
        {
            return;
        }

        switch (_stat)
        {
            case EArtifactGameModelFloatStat.CounterAttackValue:
                DataManager.Instance.GameModel.CounterAttackValue += delta;
                break;
            case EArtifactGameModelFloatStat.SuccessProbability:
                DataManager.Instance.GameModel.SuccessProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.SuccessProbability + delta);
                break;
            case EArtifactGameModelFloatStat.GreatSuccessProbability:
                DataManager.Instance.GameModel.GreatSuccessProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.GreatSuccessProbability + delta);
                break;
            case EArtifactGameModelFloatStat.UltraSuccessProbability:
                DataManager.Instance.GameModel.UltraSuccessProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.UltraSuccessProbability + delta);
                break;
            case EArtifactGameModelFloatStat.FailureProbability:
                DataManager.Instance.GameModel.FailureProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.FailureProbability + delta);
                break;
        }

        if (IsSlotMachineProbabilityStat(_stat))
        {
            EventBus.Publish(new StSlotMachineProbabilityChangedEvent());
        }
    }

    private static bool IsSlotMachineProbabilityStat(EArtifactGameModelFloatStat stat)
    {
        return stat == EArtifactGameModelFloatStat.SuccessProbability ||
               stat == EArtifactGameModelFloatStat.GreatSuccessProbability ||
               stat == EArtifactGameModelFloatStat.UltraSuccessProbability ||
               stat == EArtifactGameModelFloatStat.FailureProbability;
    }
}

[Serializable]
public class ArtifactBehavior_AddGameModelFloat : ArtifactBehavior
{
    [SerializeField] private EArtifactGameModelFloatStat _stat;
    [SerializeField] private float _delta;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (DataManager.Instance == null || DataManager.Instance.GameModel == null)
        {
            return;
        }

        switch (_stat)
        {
            case EArtifactGameModelFloatStat.CounterAttackValue:
                DataManager.Instance.GameModel.CounterAttackValue += _delta;
                break;
            case EArtifactGameModelFloatStat.SuccessProbability:
                DataManager.Instance.GameModel.SuccessProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.SuccessProbability + _delta);
                break;
            case EArtifactGameModelFloatStat.GreatSuccessProbability:
                DataManager.Instance.GameModel.GreatSuccessProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.GreatSuccessProbability + _delta);
                break;
            case EArtifactGameModelFloatStat.UltraSuccessProbability:
                DataManager.Instance.GameModel.UltraSuccessProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.UltraSuccessProbability + _delta);
                break;
            case EArtifactGameModelFloatStat.FailureProbability:
                DataManager.Instance.GameModel.FailureProbability =
                    Mathf.Clamp01(DataManager.Instance.GameModel.FailureProbability + _delta);
                break;
        }

        if (IsSlotMachineProbabilityStat(_stat))
        {
            EventBus.Publish(new StSlotMachineProbabilityChangedEvent());
        }
    }

    private static bool IsSlotMachineProbabilityStat(EArtifactGameModelFloatStat stat)
    {
        return stat == EArtifactGameModelFloatStat.SuccessProbability ||
               stat == EArtifactGameModelFloatStat.GreatSuccessProbability ||
               stat == EArtifactGameModelFloatStat.UltraSuccessProbability ||
               stat == EArtifactGameModelFloatStat.FailureProbability;
    }
}

[Serializable]
public class ArtifactBehavior_ModifyGameModelInt : ArtifactBehavior
{
    [SerializeField] private EArtifactGameModelIntStat _stat;
    [SerializeField] private int _delta;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        Apply(_delta);
    }

    public override void OnRemove(CharacterView target)
    {
        Apply(-_delta);
    }

    private void Apply(int delta)
    {
        if (DataManager.Instance == null || DataManager.Instance.GameModel == null)
        {
            return;
        }

        switch (_stat)
        {
            case EArtifactGameModelIntStat.KeywordUpgradeOptionCount:
                DataManager.Instance.GameModel.KeywordUpgradeOptionCount =
                    Mathf.Max(1, DataManager.Instance.GameModel.KeywordUpgradeOptionCount + delta);
                break;
        }
    }
}
