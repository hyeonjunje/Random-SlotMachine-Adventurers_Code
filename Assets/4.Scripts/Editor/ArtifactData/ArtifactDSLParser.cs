#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal static class ArtifactDSLParser
{
    public static ArtifactTrigger ParseTrigger(string triggerType, string triggerArg, string conditionDsl, string effectsDsl)
    {
        triggerType = NormalizeDslText(triggerType);
        conditionDsl = NormalizeConditionText(conditionDsl);
        effectsDsl = NormalizeDslText(effectsDsl);

        if (TryCreateSpecialTrigger(triggerType, triggerArg, conditionDsl, effectsDsl, out ArtifactTrigger specialTrigger))
        {
            return specialTrigger;
        }

        string resolvedConditionDsl = conditionDsl;
        if (TryMapTriggerTypeToCondition(triggerType, triggerArg, conditionDsl, effectsDsl, out string mappedConditionDsl))
        {
            triggerType = "ConditionEffect";
            resolvedConditionDsl = mappedConditionDsl;
        }
        else if (triggerType != "ConditionEffect" && string.IsNullOrWhiteSpace(conditionDsl) == false)
        {
            // Legacy CSV rows sometimes put the real event trigger into the Condition column.
            triggerType = "ConditionEffect";
            resolvedConditionDsl = conditionDsl;
        }

        ArtifactTrigger trigger = CreateTrigger(triggerType);
        if (trigger == null)
        {
            Debug.LogWarning($"[ArtifactDataImporter] Unsupported TriggerType: {triggerType}");
            return null;
        }

        if (trigger is ArtifactTrigger_OnStartTurn startTurnTrigger && int.TryParse(triggerArg, out int interval))
        {
            SetField(startTurnTrigger, "_interval", interval);
        }

        if (trigger is ArtifactTrigger_ConditionEffect conditionTrigger)
        {
            Condition condition = ParseCondition(resolvedConditionDsl);
            if (condition == null)
            {
                Debug.LogWarning($"[ArtifactDataImporter] Failed to parse Condition: {resolvedConditionDsl}");
                return null;
            }

            SetField(conditionTrigger, "_condition", condition);
        }

        List<ArtifactBehavior> behaviors = ParseBehaviors(effectsDsl);
        if (behaviors.Count == 0)
        {
            Debug.LogWarning($"[ArtifactDataImporter] Failed to parse Effects: {effectsDsl}");
            return null;
        }

        trigger.Behaviors = behaviors;
        return trigger;
    }

    private static ArtifactTrigger CreateTrigger(string triggerType)
    {
        switch (triggerType)
        {
            case "OnEquip":
                return new ArtifactTrigger_OnEquip();
            case "OnStartTurn":
                return new ArtifactTrigger_OnStartTurn();
            case "ConditionEffect":
                return new ArtifactTrigger_ConditionEffect();
            case "EventWeightModifier":
                return new ArtifactTrigger_EventWeightModifier();
            case "OnEnemySpawn":
                return new ArtifactTrigger_OnEnemySpawn();
            default:
                return null;
        }
    }

    public static Condition ParseCondition(string dsl)
    {
        if (string.IsNullOrWhiteSpace(dsl))
        {
            return null;
        }

        if (!TryParseFunction(dsl, out string name, out string[] args))
        {
            return null;
        }

        switch (name)
        {
            case "And":
            {
                var condition = new AndCondition();
                var conditions = new List<Condition>();
                foreach (string arg in args)
                {
                    Condition parsedCondition = ParseCondition(arg);
                    if (parsedCondition != null)
                    {
                        conditions.Add(parsedCondition);
                    }
                }

                if (conditions.Count == 0)
                {
                    return null;
                }

                SetField(condition, "_conditions", conditions);
                return condition;
            }
            case "OnStartTurn":
            {
                var condition = new OnStartTurnCondition();
                SetField(condition, "_interval", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "TurnAction":
            {
                var condition = new TurnActionCondition();
                SetField(condition, "_targetTurn", ParseInt(args, 0, 0));
                SetField(condition, "_targetActionIndexes", ParseIntList(args, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Pre));
                return condition;
            }
            case "OnStartBattle":
            {
                var condition = new OnStartBattleCondition();
                SetField(condition, "_reactionTiming", EReactionTiming.Post);
                return condition;
            }
            case "OnFinishBattle":
            {
                var condition = new OnPostBattleRewardCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Pre));
                return condition;
            }
            case "OnDealDamage":
            {
                var condition = new OnDealDamageCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Pre));
                return condition;
            }
            case "OnKillEnemy":
            {
                var condition = new OnKillEnemyCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Post));
                return condition;
            }
            case "OnFinishTurn":
            {
                var condition = new OnTurnEndCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Post));
                return condition;
            }
            case "OnSlotConfirm":
            {
                var condition = new OnSlotConfirmCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Pre));
                return condition;
            }
            case "OnSlotMachineReroll":
            {
                var condition = new OnRerollSlotMachineCondition();
                SetField(condition, "_reactionTiming", EReactionTiming.Post);
                return condition;
            }
            case "OnCounterAttack":
            {
                var condition = new OnCounterAttackCondition();
                SetField(condition, "_reactionTiming", EReactionTiming.Post);
                return condition;
            }
            case "OnIncreaseEnemyActCount":
            {
                var condition = new OnIncreaseEnemyActCountCondition();
                SetField(condition, "_reactionTiming", EReactionTiming.Post);
                return condition;
            }
            case "AccumulatedStatusAppliedInTurn":
            {
                var condition = new AccumulatedStatusAppliedInTurnCondition();
                SetField(condition, "_statusType", ParseStatusType(args, 0, EStatusType.Poison));
                SetField(condition, "_threshold", ParseInt(args, 1, 1));
                SetField(condition, "_targetSide", ParseEnumOrInt(args, 2, EBattleSideType.EnemySide));
                SetField(condition, "_oncePerTurn", ParseBool(args, 3, true));
                SetField(condition, "_reactionTiming", EReactionTiming.Post);
                return condition;
            }
            case "AccumulatedEnemyActCountIncreaseInTurn":
            {
                var condition = new AccumulatedEnemyActCountIncreaseInTurnCondition();
                SetField(condition, "_threshold", ParseInt(args, 0, 1));
                SetField(condition, "_oncePerTurn", ParseBool(args, 1, true));
                SetField(condition, "_reactionTiming", EReactionTiming.Post);
                return condition;
            }
            case "OnUseKeyword":
            {
                var condition = new OnUseKeywordCondition();
                if (args.Length > 0)
                {
                    EPlayerJob parsedJob = ParsePlayerJob(args[0], EPlayerJob.None);
                    bool matchAnyJob = parsedJob == EPlayerJob.Any;
                    SetField(condition, "_matchAnyJob", matchAnyJob);
                    if (!matchAnyJob)
                    {
                        SetField(condition, "_job", parsedJob);
                    }
                }

                SetField(condition, "_keywordText", args.Length > 1 ? StripQuotes(args[1]) : string.Empty);
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Pre));
                return condition;
            }
            case "HasStatus":
            {
                string targetRole = args.Length > 0 ? StripQuotes(args[0]) : "Target";
                if (string.Equals(targetRole, "Attacker", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(targetRole, "Caster", StringComparison.OrdinalIgnoreCase))
                {
                    var condition = new ActionActorHasStatusCondition();
                    SetField(condition, "_checkAttacker", true);
                    SetField(condition, "_statusType", ParseStatusType(args, 1, EStatusType.Poison));
                    SetField(condition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Pre));
                    return condition;
                }

                var targetCondition = new ActionTargetHasStatusCondition();
                SetField(targetCondition, "_statusType", ParseStatusType(args, 1, EStatusType.Poison));
                SetField(targetCondition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Pre));
                return targetCondition;
            }
            case "GoldAmount":
            {
                var condition = new GoldAmountCondition();
                SetField(condition, "_amount", ParseInt(args, 0, 0));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "TargetHpPercent":
            {
                var condition = new ActionTargetHpPercentCondition();
                SetField(condition, "_targetPercent", ParsePercentFloat(args, 0, 1f));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "RerollCount":
            {
                var condition = new RerollCountCondition();
                SetField(condition, "_count", ParseInt(args, 0, 0));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "ManaAmount":
            {
                var condition = new ManaAmountCondition();
                SetField(condition, "_amount", ParseFloat(args, 0, 0f));
                return condition;
            }
            case "ShieldAmount":
            {
                var condition = new ShieldAmountCondition();
                SetField(condition, "_amount", ParseInt(args, 0, 1));
                return condition;
            }
            case "BattleType":
            {
                var condition = new BattleTypeCondition();
                SetField(condition, "_battleType", ParseEnumOrInt(args, 0, EMapNodeType.Monster));
                return condition;
            }
            case "OnUseKeywordAfterReroll":
            {
                var condition = new OnUseKeywordAfterRerollCondition();
                SetField(condition, "_keywordText", args.Length > 0 ? StripQuotes(args[^1]) : string.Empty);
                SetField(condition, "_reactionTiming", EReactionTiming.Pre);
                return condition;
            }
            case "OnGainShield":
            {
                var condition = new OnGainShieldCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Post));
                return condition;
            }
            case "StatusStack":
            {
                var condition = new StatusStackCondition();
                int statusArgIndex = args.Length >= 3 ? 1 : 0;
                int thresholdArgIndex = args.Length >= 3 ? 2 : 1;
                SetField(condition, "_statusType", ParseStatusType(args, statusArgIndex, EStatusType.Poison));
                SetField(condition, "_threshold", ParseInt(args, thresholdArgIndex, 1));
                SetField(condition, "_reactionTiming", EReactionTiming.Post);
                return condition;
            }
            case "KeywordUseCount":
            {
                var condition = new KeywordUseCountCondition();
                SetField(condition, "_keywordText", args.Length > 0 ? StripQuotes(args[0]) : string.Empty);
                SetField(condition, "_count", ParseInt(args, 1, 2));
                return condition;
            }
            case "EnemyDamageOnTurnInterval":
            {
                var condition = new EnemyDamageOnTurnIntervalCondition();
                SetField(condition, "_interval", ParseInt(args, 0, 5));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "FirstJobAttackInBattle":
            {
                var condition = new FirstJobAttackInBattleCondition();
                SetField(condition, "_job", ParsePlayerJob(args.Length > 0 ? args[0] : string.Empty, EPlayerJob.Warrior));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "ManaSpentReroll":
            {
                var condition = new ManaSpentRerollCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Post));
                return condition;
            }
            case "OnAnyReroll":
            {
                var condition = new AnyRerollCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Post));
                return condition;
            }
            case "BattleRerollCount":
            {
                var condition = new BattleRerollCountCondition();
                SetField(condition, "_count", ParseInt(args, 0, 0));
                return condition;
            }
            case "NoEnemyDamageThisTurn":
            {
                var condition = new NoEnemyDamageThisTurnCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Post));
                return condition;
            }
            case "OnApplyStatusByJob":
            {
                var condition = new OnApplyStatusByJobCondition();
                SetField(condition, "_job", ParsePlayerJob(args.Length > 0 ? args[0] : string.Empty, EPlayerJob.Archer));
                SetField(condition, "_statusType", ParseStatusType(args, 1, EStatusType.Marking));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Post));
                return condition;
            }
            case "OnReceiveStatus":
            {
                var condition = new ReceiveStatusCondition();
                SetField(condition, "_statusCategory", ParseStatusCategory(args, 0, EStatusCategory.Debuff));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "OnAttackNoManaCost":
            {
                var condition = new OnAttackNoManaCostCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Pre));
                return condition;
            }
            case "OnAppearKeywordTier":
            {
                var condition = new OnAppearKeywordTierCondition();
                SetField(condition, "_rank", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "HitCount":
            {
                var condition = new BattleAttackCountCondition();
                SetField(condition, "_targetCount", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "TargetActCount":
            {
                var condition = new TargetActCountCondition();
                SetField(condition, "_targetActCount", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "HpLower":
            {
                var condition = new HpThresholdCrossedCondition();
                SetField(condition, "_flatHp", ParseInt(args, 0, 0));
                SetField(condition, "_probability", ParsePercentFloat(args, 1, 0f));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Post));
                return condition;
            }
            case "LowHp":
            {
                var condition = new LowHpDamageCondition();
                SetField(condition, "_flatHp", ParseInt(args, 0, 0));
                SetField(condition, "_probability", ParsePercentFloat(args, 1, 0f));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Pre));
                return condition;
            }
            case "LowHpSpin":
            {
                var condition = new LowHpSlotSpinCondition();
                SetField(condition, "_flatHp", ParseInt(args, 0, 0));
                SetField(condition, "_probability", ParsePercentFloat(args, 1, 0f));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 2, EReactionTiming.Pre));
                return condition;
            }
            case "SlotTurnCount":
            {
                var condition = new SlotTurnCountCondition();
                SetField(condition, "_interval", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "OnSameKeywordUsed":
            {
                var condition = new SameKeywordUsedCondition();
                SetField(condition, "_requiredMatchCount", ParseInt(args, 0, 2));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Pre));
                return condition;
            }
            case "OnShopPurchase":
            case "OnPurchase":
            {
                var condition = new OnShopPurchaseCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Post));
                return condition;
            }
            case "GoldSpent":
            {
                var condition = new CumulativeGoldSpentCondition();
                SetField(condition, "_threshold", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "NewKeywordCount":
            {
                var condition = new CumulativeNewKeywordCondition();
                SetField(condition, "_threshold", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "KillEnemyCount":
            {
                var condition = new CumulativeKillEnemyCondition();
                SetField(condition, "_threshold", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "RerollCountCumulative":
            case "CumulativeRerollCount":
            {
                var condition = new CumulativeRerollCondition();
                SetField(condition, "_threshold", ParseInt(args, 0, 1));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "KillerJob":
            {
                var condition = new KillerJobCondition();
                SetField(condition, "_job", ParsePlayerJob(args.Length > 0 ? args[0] : string.Empty, EPlayerJob.None));
                return condition;
            }
            case "OnSlotSuccess":
            case "SlotMachineSuccess":
            {
                var condition = new SlotMachineSuccessTypeCondition();
                SetField(condition, "_successType", ParseEnumOrInt(args, 0, ESlotMachineSuccessType.GreatSuccess));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "BattleManaSpent":
            {
                var condition = new BattleManaSpentCondition();
                SetField(condition, "_threshold", ParseFloat(args, 0, 1f));
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 1, EReactionTiming.Post));
                return condition;
            }
            case "PerfectBattleClear":
            {
                var condition = new PerfectBattleClearCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Pre));
                return condition;
            }
            case "FullHpBattleClear":
            {
                var condition = new FullHpBattleClearCondition();
                SetField(condition, "_reactionTiming", ParseReactionTiming(args, 0, EReactionTiming.Pre));
                return condition;
            }
            case "ClearNodeTypeCount":
            case "MapNodeVisit":
            {
                var condition = new ClearNodeTypeCountCondition();
                SetField(condition, "_nodeType", ParseEnumOrInt(args, 0, EMapNodeType.Event));
                SetField(condition, "_threshold", ParseInt(args, 1, 1));
                return condition;
            }
            case "ArtifactCountMultiple":
            {
                var condition = new ArtifactCountMultipleCondition();
                SetField(condition, "_threshold", ParseInt(args, 0, 5));
                return condition;
            }
            default:
                return GameDSLParser.ParseCondition(dsl);
        }
    }

    public static List<ArtifactBehavior> ParseBehaviors(string dsl)
    {
        var behaviors = new List<ArtifactBehavior>();
        if (string.IsNullOrWhiteSpace(dsl))
        {
            return behaviors;
        }

        foreach (string part in SplitDsl(dsl))
        {
            ArtifactBehavior behavior = ParseBehavior(part.Trim());
            if (behavior != null)
            {
                behaviors.Add(behavior);
            }
        }

        return behaviors;
    }

    private static ArtifactBehavior ParseBehavior(string dsl)
    {
        if (string.IsNullOrWhiteSpace(dsl))
        {
            return null;
        }

        if (!TryParseFunction(dsl, out string name, out string[] args))
        {
            return null;
        }

        switch (name)
        {
            case "Chance":
                return CreateChanceBehavior(args);
            case "OnKill":
                return ParseBehavior(args.Length > 0 ? args[0] : string.Empty);
            case "PoisonSpread":
                return CreatePoisonSpreadBehavior(args);
            case "UnlockDiagonal":
                return new ArtifactBehavior_UnlockDiagonal();
            case "ModifyDamage":
                return CreateModifyDamageBehavior(args);
            case "ModifyDamagePercent":
                return CreateModifyDamagePercentBehavior(args);
            case "ModifyEventWeight":
                return CreateModifyEventWeightBehavior(args);
            case "ModifyCounterAttackValue":
                return CreateModifyGameModelFloatBehavior(EArtifactGameModelFloatStat.CounterAttackValue, ParseFloat(args, 0, 0f));
            case "ModifySlotProbability":
                return CreateModifySlotProbabilityBehavior(args);
            case "IncreaseSuccessProbability":
                return CreateAddGameModelFloatBehavior(EArtifactGameModelFloatStat.SuccessProbability, ParseProbabilityDelta(args, 0, 0f));
            case "IncreaseGreatSuccessProbability":
                return CreateAddGameModelFloatBehavior(EArtifactGameModelFloatStat.GreatSuccessProbability, ParseProbabilityDelta(args, 0, 0f));
            case "IncreaseUltraSuccessProbability":
                return CreateAddGameModelFloatBehavior(EArtifactGameModelFloatStat.UltraSuccessProbability, ParseProbabilityDelta(args, 0, 0f));
            case "IncreaseFailureProbability":
                return CreateAddGameModelFloatBehavior(EArtifactGameModelFloatStat.FailureProbability, ParseProbabilityDelta(args, 0, 0f));
            case "ModifyElectricValueMultiplier":
                return CreateModifyElectricValueMultiplierBehavior(args);
            case "ModifyGoldRewardPercent":
                return CreateModifyGoldRewardPercentBehavior(args);
            case "IgnoreShield":
                return CreateIgnoreShieldBehavior(args);
            case "ModifyDamageTaken":
                return CreateModifyDamageTakenBehavior(args);
            case "ModifyShopPrice":
                return CreateModifyShopPriceBehavior(args);
            case "DestroyArtifact":
                return new ArtifactBehavior_DestroyOwnerArtifact();
            case "BlockStatus":
                return CreateBlockStatusBehavior(args);
            case "DoubleTokens":
                return CreateDoubleTokensBehavior(args);
            case "SkipNextTurn":
                return new ArtifactBehavior_SkipNextTurn();
            case "ShieldByAttackTokenCount":
                return CreateShieldByAttackTokenCountBehavior(args);
            case "ModifyMapWeight":
                return CreateModifyMapWeightBehavior(args);
            case "HealPartyPercentOfMaxHp":
                return CreateHealPartyPercentOfMaxHpBehavior(args);
            case "SetFirstTurnFreeRerolls":
                return CreateSetFirstTurnFreeRerollsBehavior(args);
            case "RerollRandomSlots":
                return CreateRerollRandomSlotsBehavior(args);
            case "SetHp":
                return CreateSetHpBehavior(args);
            case "ModifySlotClickRerollManaCost":
                return CreateModifySlotClickRerollManaCostBehavior(args);
            case "ReintroduceHighestRankKeywordOnClickReroll":
                return CreateReintroduceHighestRankKeywordOnClickRerollBehavior(args);
            case "LevelUpRandomPlayerOnPermanentKeyword":
                return CreateLevelUpRandomPlayerOnPermanentKeywordBehavior(args);
            case "MarkEnemiesOnTurnEndIfUniqueSlot":
                return CreateMarkEnemiesOnTurnEndIfUniqueSlotBehavior(args);
            case "RevivePartyOnce":
                return CreateRevivePartyOnceBehavior(args);
            case "UpgradeAllSlotsToHighestTierOnNthReroll":
                return CreateUpgradeAllSlotsToHighestTierOnNthRerollBehavior(args);
            case "NullifyWeakenedEnemyDamageChance":
                return CreateNullifyWeakenedEnemyDamageChanceBehavior(args);
            case "DealPartyAttackDamageOnReroll":
                return CreateDealPartyAttackDamageOnRerollBehavior(args);
            case "DisableRerollAndDoublePlayerTokens":
                return CreateDisableRerollAndDoublePlayerTokensBehavior(args);
            case "ModifyKeywordUpgradeChoiceCount":
                return CreateModifyGameModelIntBehavior(EArtifactGameModelIntStat.KeywordUpgradeOptionCount, ParseInt(args, 0, 0));
            case "AdditionalAttack":
                return CreateAdditionalAttackBehavior(args);
            case "RerollAllSlots":
                return CreateRerollAllSlotsBehavior(args);
            case "ModifyEffectValue":
                return CreateModifyEffectValueBehavior(args);
            case "ManaToHeal":
                return CreateManaToHealBehavior(args);
            case "FillMana":
                return CreateFillManaBehavior(args);
            case "SetEnemyActCount":
                return CreateSetEnemyActCountBehavior(args);
            case "ModifySlotTierWeight":
                return CreateModifySlotTierWeightBehavior(args);
            case "ModifyEliteMaxHpPercent":
                return CreateModifyEliteMaxHpPercentBehavior(args);
            case "HealByStatusCount":
                return CreateHealByStatusCountBehavior(args);
            case "DamageByCurrentShield":
                return new ArtifactBehavior_DamageByCurrentShield();
            case "GainRandomArtifact":
                return CreateGainRandomArtifactBehavior(args);
            case "AddBuffNextTurn":
                return CreateAddBuffNextTurnBehavior(args);
            case "IncreaseStatWithCap":
                return CreateIncreaseStatWithCapBehavior(args);
            case "DamageByStatusStack":
                return CreateDamageByStatusStackBehavior(args);
            case "GoldRewardPercentPerStack":
                return CreateGoldRewardPercentPerStackBehavior(args);
            case "RandomHealOrDamageSelf":
                return CreateRandomHealOrDamageSelfBehavior(args);
            case "ScheduleNextTurnDamageWithHpCost":
                return CreateScheduleNextTurnDamageWithHpCostBehavior(args);
            case "DestroyAfterBattleClears":
                return CreateDestroyAfterBattleClearsBehavior(args);
            case "ModifyGreatSuccessProbabilityMultiplier":
                return CreateModifyGreatSuccessProbabilityMultiplierBehavior(args);
            case "UpgradeGreatSuccessToUltraChance":
                return CreateUpgradeGreatSuccessToUltraChanceBehavior(args);
            case "SetGold":
                return CreateSetGoldBehavior(args);
            case "AddFreeReroll":
            case "ChangeEnemyActCount":
            case "GoldDelta":
            case "NextPage":
            case "ClearNode":
            case "Heal":
            case "Damage":
            case "ChangeStat":
            case "LevelUp":
            case "LevelUpParty":
            case "LevelUpRandomKeyword":
            case "AddKeyword":
            case "AddStatus":
            case "ApplyMark":
            case "RemoveStatus":
            case "StartBattle":
            case "Shield":
            case "EarnMoney":
            case "RerollLine":
            case "RepeatLastBattleAct":
                return CreateUseEffectBehavior(dsl);
            default:
                Debug.LogWarning($"[ArtifactDataImporter] Unsupported Effect DSL: {dsl}");
                return null;
        }
    }

    private static ArtifactBehavior CreateModifyDamageBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyDamage();
        SetField(behavior, "_multiplier", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyDamagePercentBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyDamagePercent();
        SetField(behavior, "_percent", ParseFloat(args, 0, 0f));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyEventWeightBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyEventWeight();
        var modifiers = new List<ArtifactBehavior_ModifyEventWeight.WeightModifierData>();

        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            if (!int.TryParse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int typeIndex))
            {
                continue;
            }

            if (!int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int addAmount))
            {
                continue;
            }

            modifiers.Add(new ArtifactBehavior_ModifyEventWeight.WeightModifierData
            {
                TargetType = (EEventRiskRewardType)typeIndex,
                AddAmount = addAmount,
            });
        }

        SetField(behavior, "_modifiers", modifiers);
        return behavior;
    }

    private static ArtifactBehavior CreateModifyGameModelFloatBehavior(EArtifactGameModelFloatStat stat, float delta)
    {
        var behavior = new ArtifactBehavior_ModifyGameModelFloat();
        SetField(behavior, "_stat", stat);
        SetField(behavior, "_delta", delta);
        return behavior;
    }

    private static ArtifactBehavior CreateAddGameModelFloatBehavior(EArtifactGameModelFloatStat stat, float delta)
    {
        var behavior = new ArtifactBehavior_AddGameModelFloat();
        SetField(behavior, "_stat", stat);
        SetField(behavior, "_delta", delta);
        return behavior;
    }

    private static ArtifactBehavior CreateModifySlotProbabilityBehavior(string[] args)
    {
        EArtifactGameModelFloatStat stat = ParseSlotProbabilityStat(args, 0, EArtifactGameModelFloatStat.GreatSuccessProbability);
        return CreateModifyGameModelFloatBehavior(stat, ParseProbabilityDelta(args, 1, 0f));
    }

    private static ArtifactBehavior CreateModifyGameModelIntBehavior(EArtifactGameModelIntStat stat, int delta)
    {
        var behavior = new ArtifactBehavior_ModifyGameModelInt();
        SetField(behavior, "_stat", stat);
        SetField(behavior, "_delta", delta);
        return behavior;
    }

    private static ArtifactBehavior CreateSetGoldBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_SetGold();
        SetField(behavior, "_amount", ParseInt(args, 0, 0));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyGoldRewardPercentBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyGoldRewardPercent();
        SetField(behavior, "_percent", ParseFloat(args, 0, 0f));
        return behavior;
    }

    private static ArtifactBehavior CreateIgnoreShieldBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_IgnoreShield();
        SetField(behavior, "_ignoreShield", ParseBool(args, 0, true));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyDamageTakenBehavior(string[] args)
    {
        float raw = ParseFloat(args, 0, 0f);
        if (Mathf.Abs(raw) > 0f && Mathf.Abs(raw) < 1f)
        {
            var percentBehavior = new ArtifactBehavior_ModifyDamageTakenPercent();
            SetField(percentBehavior, "_percent", raw * 100f);
            return percentBehavior;
        }

        var behavior = new ArtifactBehavior_ModifyDamageTaken();
        SetField(behavior, "_flatDelta", Mathf.RoundToInt(raw));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyShopPriceBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyShopPrice();
        float raw = ParseFloat(args, 0, 0f);
        SetField(behavior, "_percent", Mathf.Abs(raw) < 1f ? raw * 100f : raw);
        return behavior;
    }

    private static ArtifactBehavior CreateBlockStatusBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_BlockStatus();
        SetField(behavior, "_count", ParseInt(args, 0, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateDoubleTokensBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_DoublePlayerTokensThisTurn();
        SetField(behavior, "_multiplier", ParseInt(args, 1, 2));
        return behavior;
    }

    private static ArtifactBehavior CreateShieldByAttackTokenCountBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ShieldByAttackTokenCount();
        SetField(behavior, "_shieldPerAttackToken", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyMapWeightBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyMapWeight();
        var modifiers = new List<ArtifactBehavior_ModifyMapWeight.MapWeightModifierData>();

        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            modifiers.Add(new ArtifactBehavior_ModifyMapWeight.MapWeightModifierData
            {
                NodeType = ParseEnumOrInt(args, i, EMapNodeType.None),
                Delta = ParseFloat(args, i + 1, 0f),
            });
        }

        SetField(behavior, "_modifiers", modifiers);
        return behavior;
    }

    private static ArtifactBehavior CreateAdditionalAttackBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_AdditionalAttack();
        SetField(behavior, "_targetCount", ParseEnemyTargetCount(args, 0, 1));
        SetField(behavior, "_repeatCount", ParseInt(args, 1, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateRerollAllSlotsBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_RerollAllSlots();
        SetField(behavior, "_rerollCount", ParseInt(args, 0, 1));
        if (args.Length > 1)
        {
            SetField(behavior, "_higherTierWeightMultiplier", ParseFloat(args, 1, 1f));
        }

        return behavior;
    }

    private static ArtifactBehavior CreateModifyEffectValueBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyEffectValue();
        SetField(behavior, "_multiplier", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateManaToHealBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ManaToHeal();
        SetField(behavior, "_ratio", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateFillManaBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_FillMana();
        SetField(behavior, "_amount", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateSetEnemyActCountBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_SetEnemyActCount();
        SetField(behavior, "_targetActCount", ParseInt(args, 0, 0));
        return behavior;
    }

    private static ArtifactBehavior CreateModifySlotTierWeightBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifySlotTierWeight();
        SetField(behavior, "_multiplier", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateHealByStatusCountBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_HealByStatusCount();
        int categoryArgIndex = args.Length >= 3 ? 1 : 0;
        int ratioArgIndex = args.Length >= 3 ? 2 : 1;
        SetField(behavior, "_statusCategory", ParseStatusCategory(args, categoryArgIndex, EStatusCategory.Debuff));
        SetField(behavior, "_ratio", ParseFloat(args, ratioArgIndex, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateGainRandomArtifactBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_GainRandomArtifact();
        int countArgIndex = args.Length >= 2 ? 1 : 0;
        SetField(behavior, "_count", ParseInt(args, countArgIndex, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateAddBuffNextTurnBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_AddBuffNextTurn();
        SetField(behavior, "_statType", ParseEnumOrInt(args, 0, EStatType.AttackPower));
        SetField(behavior, "_value", ParseFloat(args, 1, 0f));
        SetField(behavior, "_modType", ParseEnumOrInt(args, 2, EStatModType.Add));
        return behavior;
    }

    private static ArtifactBehavior CreateIncreaseStatWithCapBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_IncreaseStatWithCap();
        SetField(behavior, "_statType", ParseEnumOrInt(args, 0, EStatType.AttackPower));
        SetField(behavior, "_value", ParseFloat(args, 1, 1f));
        SetField(behavior, "_cap", ParseInt(args, 2, 10));
        SetField(behavior, "_modType", EStatModType.Add);
        return behavior;
    }

    private static ArtifactBehavior CreateDamageByStatusStackBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_DamageByStatusStack();
        SetField(behavior, "_statusType", ParseStatusType(args, 0, EStatusType.Poison));
        SetField(behavior, "_multiplier", ParseFloat(args, 1, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateGoldRewardPercentPerStackBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_GoldRewardPercentPerStack();
        SetField(behavior, "_percentPerStack", ParseFloat(args, 0, 1f));
        SetField(behavior, "_maxStack", ParseInt(args, 1, 30));
        return behavior;
    }

    private static ArtifactBehavior CreateRandomHealOrDamageSelfBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_RandomHealOrDamageSelf();
        SetField(behavior, "_healChancePercent", ParseFloat(args, 0, 50f));
        SetField(behavior, "_healAmount", ParseInt(args, 1, 5));
        SetField(behavior, "_damageAmount", ParseInt(args, 2, 3));
        return behavior;
    }

    private static ArtifactBehavior CreateScheduleNextTurnDamageWithHpCostBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ScheduleNextTurnDamageWithHpCost();
        SetField(behavior, "_hpCost", ParseInt(args, 0, 3));
        SetField(behavior, "_damage", ParseInt(args, 1, 10));
        return behavior;
    }

    private static ArtifactBehavior CreateDestroyAfterBattleClearsBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_DestroyAfterBattleClears();
        SetField(behavior, "_battleCount", ParseInt(args, 0, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyGreatSuccessProbabilityMultiplierBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyGreatSuccessProbabilityMultiplier();
        SetField(behavior, "_multiplier", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateUpgradeGreatSuccessToUltraChanceBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_UpgradeGreatSuccessToUltraChance();
        SetField(behavior, "_chancePercent", ParsePercentValue(args, 0, 50f));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyEliteMaxHpPercentBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyEliteMaxHpPercent();
        SetField(behavior, "_percent", ParseFloat(args, 0, 0f));
        return behavior;
    }

    private static ArtifactBehavior CreateModifyElectricValueMultiplierBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifyElectricValueMultiplier();
        SetField(behavior, "_multiplier", ParseFloat(args, 0, 1f));
        return behavior;
    }

    private static ArtifactBehavior CreateHealPartyPercentOfMaxHpBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_HealPartyPercentOfMaxHp();
        SetField(behavior, "_ratio", ParseFloat(args, 0, 0f));
        return behavior;
    }

    private static ArtifactBehavior CreateSetFirstTurnFreeRerollsBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_SetFirstTurnFreeRerolls();
        SetField(behavior, "_count", ParseInt(args, 0, 0));
        return behavior;
    }

    private static ArtifactBehavior CreateRerollRandomSlotsBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_RerollRandomSlots();
        SetField(behavior, "_slotCount", ParseInt(args, 0, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateSetHpBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_SetTargetHp();
        SetField(behavior, "_targetHp", ParseInt(args, 0, 1));

        if (TryParseTargetSelector(args, 1, out TargetSelector selector))
        {
            SetField(behavior, "_targetSelector", selector);
        }

        return behavior;
    }

    private static ArtifactBehavior CreateModifySlotClickRerollManaCostBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ModifySlotClickRerollManaCost();
        SetField(behavior, "_delta", ParseInt(args, 0, 0));
        return behavior;
    }

    private static ArtifactBehavior CreateReintroduceHighestRankKeywordOnClickRerollBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_ReintroduceHighestRankKeywordOnClickReroll();
        SetField(behavior, "_chancePercent", ParsePercentValue(args, 0, 0f));
        return behavior;
    }

    private static ArtifactBehavior CreateLevelUpRandomPlayerOnPermanentKeywordBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_LevelUpRandomPlayerOnPermanentKeyword();
        SetField(behavior, "_chancePercent", ParsePercentValue(args, 0, 0f));
        SetField(behavior, "_levelDiff", ParseInt(args, 1, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateMarkEnemiesOnTurnEndIfUniqueSlotBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_MarkEnemiesOnTurnEndIfUniqueSlot();
        SetField(behavior, "_markStacks", ParseInt(args, 0, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateRevivePartyOnceBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_RevivePartyOnce();
        SetField(behavior, "_reviveRatio", ParseFloat(args, 0, 0f));
        return behavior;
    }

    private static ArtifactBehavior CreateUpgradeAllSlotsToHighestTierOnNthRerollBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_UpgradeAllSlotsToHighestTierOnNthReroll();
        SetField(behavior, "_interval", ParseInt(args, 0, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateNullifyWeakenedEnemyDamageChanceBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_NullifyWeakenedEnemyDamageChance();
        SetField(behavior, "_chancePercent", ParsePercentValue(args, 0, 0f));
        return behavior;
    }

    private static ArtifactBehavior CreateDealPartyAttackDamageOnRerollBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_DealPartyAttackDamageOnReroll();
        SetField(behavior, "_ratio", ParseFloat(args, 0, 1f));
        SetField(behavior, "_targetCount", ParseInt(args, 1, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateDisableRerollAndDoublePlayerTokensBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_DisableRerollAndDoublePlayerTokens();
        SetField(behavior, "_tokenMultiplier", ParseInt(args, 0, 2));
        return behavior;
    }

    private static ArtifactBehavior CreatePoisonSpreadBehavior(string[] args)
    {
        var behavior = new ArtifactBehavior_PoisonSpreadWatcher();
        SetField(behavior, "_spreadRatio", ParseFloat(args, 0, 0.5f));
        SetField(behavior, "_targetCount", ParseInt(args, 1, 1));
        return behavior;
    }

    private static ArtifactBehavior CreateChanceBehavior(string[] args)
    {
        if (args.Length < 2)
        {
            return null;
        }

        ArtifactBehavior nestedBehavior = ParseBehavior(args[1]);
        if (nestedBehavior == null)
        {
            return null;
        }

        var behavior = new ArtifactBehavior_ChanceWrapper();
        SetField(behavior, "_chancePercent", ParsePercentValue(args, 0, 0f));
        SetField(behavior, "_behaviors", new List<ArtifactBehavior> { nestedBehavior });
        return behavior;
    }

    private static ArtifactBehavior CreateUseEffectBehavior(string dsl)
    {
        Effect effect = ParseEffect(dsl);
        if (effect == null)
        {
            return null;
        }

        return new ArtifactBehavior_UseEffect
        {
            Effect = effect
        };
    }

    public static Effect ParseEffect(string dsl)
    {
        if (string.IsNullOrWhiteSpace(dsl))
        {
            return null;
        }

        if (TryParseFunction(dsl, out string name, out string[] args))
        {
            switch (name)
            {
                case "AddFreeReroll":
                    return CreateAddFreeRerollEffect(args);
                case "ChangeEnemyActCount":
                    return CreateChangeEnemyActCountEffect(args);
                case "AddStatus":
                    return CreateAddStatusEffect(args);
                case "ApplyMark":
                    return CreateApplyMarkEffect(args);
                case "Damage":
                    return CreateDamageEffect(args);
                case "Heal":
                    return CreateHealEffect(args);
                case "Shield":
                    return CreateShieldEffect(args);
                case "ChangeStat":
                    return CreateChangeStatEffect(args);
                case "LevelUpParty":
                    return CreateLevelUpPartyEffect(args);
                case "RemoveStatus":
                    return CreateRemoveStatusEffect(args);
                case "RerollLine":
                    return CreateRerollLineEffect(args);
                case "RepeatLastBattleAct":
                    return CreateRepeatLastBattleActEffect(args);
            }
        }

        Effect[] effects = GameDSLParser.ParseEffects(dsl);
        if (effects == null || effects.Length == 0)
        {
            return null;
        }

        return effects[0];
    }

    private static Effect CreateAddFreeRerollEffect(string[] args)
    {
        return new AddFreeRerollEffect
        {
            Amount = ParseInt(args, 0, 1)
        };
    }

    private static Effect CreateChangeEnemyActCountEffect(string[] args)
    {
        var effect = new ChnageEnemyActCountEffect();
        SetField(effect, "_diff", ParseInt(args, 0, 0));

        TargetSelector selector = null;
        if (args.Length >= 2)
        {
            if (TryParseTargetSelector(args[1], out TargetSelector parsedSelector))
            {
                selector = parsedSelector;
            }
            else
            {
                int targetCount = ParseInt(args, 1, 1);
                var randomSelector = new EnemyRandomTargetSelector();
                SetField(randomSelector, "_randomTargetCount", targetCount);
                selector = randomSelector;
            }
        }

        if (selector != null)
        {
            SetField(effect, "<TargetSelector>k__BackingField", selector);
        }

        return effect;
    }

    private static Effect CreateAddStatusEffect(string[] args)
    {
        EStatusType statusType = ParseStatusType(args, 0, EStatusType.Poison);
        SO_StatusData status = FindStatusData(statusType);
        if (status == null)
        {
            Debug.LogWarning($"[ArtifactDataImporter] Status asset not found for {statusType}");
            return null;
        }

        var effect = new AddStatusEffect();
        SetField(effect, "_status", status);
        SetField(effect, "_turn", ParseInt(args, 1, 1));

        if (TryParseTargetSelector(args, 2, out TargetSelector selector))
        {
            SetField(effect, "<TargetSelector>k__BackingField", selector);
        }

        return effect;
    }

    private static Effect CreateApplyMarkEffect(string[] args)
    {
        return CreateAddStatusEffect(new[]
        {
            nameof(EStatusType.Marking),
            "1",
            args.Length > 0 ? args[0] : "EnemyRandom(1)"
        });
    }

    private static Effect CreateDamageEffect(string[] args)
    {
        var effect = new DealDamageEffect();
        var formula = new DamageFormula(ParseEnumOrInt(args, 0, EDamageFormulaType.Flat), ParseFloat(args, 1, 0f));
        SetField(effect, "_damageFormula", formula);

        if (TryParseTargetSelector(args, 2, out TargetSelector selector))
        {
            SetField(effect, "<TargetSelector>k__BackingField", selector);
        }

        return effect;
    }

    private static Effect CreateHealEffect(string[] args)
    {
        var effect = new ApplyHealingEffect();
        var formula = new HealingFormula(ParseEnumOrInt(args, 0, EHealingFormulaType.Flat), ParseFloat(args, 1, 0f));
        SetField(effect, "_healingFormula", formula);

        if (TryParseTargetSelector(args, 2, out TargetSelector selector))
        {
            SetField(effect, "<TargetSelector>k__BackingField", selector);
        }

        return effect;
    }

    private static Effect CreateShieldEffect(string[] args)
    {
        var effect = new AddShieldEffect();
        var formula = new ShieldFormula(ParseEnumOrInt(args, 0, EShieldFormulaType.Flat), ParseFloat(args, 1, 0f));
        SetField(effect, "_shieldFormula", formula);

        if (TryParseTargetSelector(args, 2, out TargetSelector selector))
        {
            SetField(effect, "<TargetSelector>k__BackingField", selector);
        }

        return effect;
    }

    private static Effect CreateChangeStatEffect(string[] args)
    {
        var effect = new ChangeStatValueEffect();
        SetField(effect, "_statType", ParseEnumOrInt(args, 0, EStatType.MaxHp));
        SetField(effect, "_statModType", ParseEnumOrInt(args, 1, EStatModType.Add));
        SetField(effect, "_value", ParseFloat(args, 2, 0f));

        if (TryParseTargetSelector(args, 3, out TargetSelector selector))
        {
            SetField(effect, "<TargetSelector>k__BackingField", selector);
        }

        return effect;
    }

    private static Effect CreateLevelUpPartyEffect(string[] args)
    {
        var effect = new LevelUpPartyEffect();
        SetField(effect, "_levelDiff", ParseInt(args, 0, 1));
        return effect;
    }

    private static Effect CreateRemoveStatusEffect(string[] args)
    {
        var effect = new RemoveStatusByCategoryEffect();
        SetField(effect, "_statusCategory", ParseStatusCategory(args, 0, EStatusCategory.Debuff));
        SetField(effect, "_count", ParseInt(args, 1, 1));

        if (TryParseTargetSelector(args, 2, out TargetSelector selector))
        {
            SetField(effect, "<TargetSelector>k__BackingField", selector);
        }
        else
        {
            SetField(effect, "<TargetSelector>k__BackingField", new SelfTargetSelector());
        }

        return effect;
    }

    private static Effect CreateRerollLineEffect(string[] args)
    {
        var effect = new RerollSlotMachineLineEffect();
        SetField(effect, "_direction", ParseEnumOrInt(args, 0, ESlotMachineLineDirection.Horizontal));
        SetField(effect, "_lineCount", ParseInt(args, 1, 1));
        return effect;
    }

    private static Effect CreateRepeatLastBattleActEffect(string[] args)
    {
        var effect = new RepeatLastBattleActEffect();
        SetField(effect, "_repeatCount", ParseInt(args, 0, 1));
        return effect;
    }

    private static bool TryCreateSpecialTrigger(
        string triggerType,
        string triggerArg,
        string conditionDsl,
        string effectsDsl,
        out ArtifactTrigger trigger)
    {
        trigger = null;

        if (!TryParseFunction(conditionDsl, out string conditionName, out string[] conditionArgs))
        {
            return false;
        }

        if (string.Equals(conditionName, "GoldAmount", StringComparison.OrdinalIgnoreCase))
        {
            int threshold = ParseInt(conditionArgs, 0, 100);

            if (TryParseFunction(effectsDsl, out string effectName, out string[] effectArgs))
            {
                if (effectName == "ModifyDamagePercent")
                {
                    ArtifactTrigger_ConditionEffect conditionTrigger = new ArtifactTrigger_ConditionEffect();
                    var condition = new OnOurSideDealDamageCondition();
                    SetField(condition, "_reactionTiming", EReactionTiming.Pre);
                    SetField(conditionTrigger, "_condition", condition);
                    conditionTrigger.Behaviors = new List<ArtifactBehavior>
                    {
                        CreateGoldThresholdDamageBehavior(threshold, ParseFloat(effectArgs, 0, 0f))
                    };
                    trigger = conditionTrigger;
                    return true;
                }

                if (effectName == "ChangeStat" && IsAttackPowerStatChange(effectArgs))
                {
                    ArtifactTrigger_OnEquip onEquipTrigger = new ArtifactTrigger_OnEquip();
                    onEquipTrigger.Behaviors = new List<ArtifactBehavior>
                    {
                        CreateGoldThresholdAttackPowerBehavior(threshold, ParseFloat(effectArgs, 2, 0f))
                    };
                    trigger = onEquipTrigger;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryMapTriggerTypeToCondition(
        string triggerType,
        string triggerArg,
        string conditionDsl,
        string effectsDsl,
        out string mappedConditionDsl)
    {
        mappedConditionDsl = conditionDsl;

        switch (triggerType)
        {
            case "ConditionEffect":
                return !string.IsNullOrWhiteSpace(conditionDsl);
            case "OnStartBattle":
                mappedConditionDsl = "OnStartBattle()";
                return true;
            case "OnFinishBattle":
                mappedConditionDsl = "OnFinishBattle(Pre)";
                return true;
            case "OnHit":
                mappedConditionDsl = "OnDealDamage(Post)";
                return true;
            case "OnKill":
                mappedConditionDsl = "OnKillEnemy(Post)";
                return true;
            case "OnFinishTurn":
                mappedConditionDsl = "OnFinishTurn(Post)";
                return true;
            case "OnSlotConfirm":
                mappedConditionDsl = "OnSlotConfirm(Pre)";
                return true;
            case "OnShopPurchase":
            case "OnPurchase":
                mappedConditionDsl = "OnShopPurchase(Post)";
                return true;
            case "HpLower":
                mappedConditionDsl = IsSlotTierWeightEffect(effectsDsl)
                    ? $"LowHpSpin(0,{triggerArg},Pre)"
                    : $"LowHp(0,{triggerArg},Pre)";
                return true;
            default:
                return false;
        }
    }

    private static ArtifactBehavior CreateGoldThresholdDamageBehavior(int threshold, float percentPerThreshold)
    {
        var behavior = new ArtifactBehavior_ModifyDamageByGoldThreshold();
        SetField(behavior, "_goldThreshold", threshold);
        SetField(behavior, "_percentPerThreshold", percentPerThreshold);
        return behavior;
    }

    private static ArtifactBehavior CreateGoldThresholdAttackPowerBehavior(int threshold, float valuePerThreshold)
    {
        var behavior = new ArtifactBehavior_AttackPowerByGoldThreshold();
        SetField(behavior, "_goldThreshold", threshold);
        SetField(behavior, "_valuePerThreshold", valuePerThreshold);
        SetField(behavior, "_statType", EStatType.AttackPower);
        SetField(behavior, "_statModType", EStatModType.Add);
        return behavior;
    }

    private static bool IsAttackPowerStatChange(string[] args)
    {
        return ParseEnumOrInt(args, 0, EStatType.MaxHp) == EStatType.AttackPower;
    }

    private static bool IsSlotTierWeightEffect(string effectsDsl)
    {
        return TryParseFunction(effectsDsl, out string name, out _) &&
               string.Equals(name, "ModifySlotTierWeight", StringComparison.OrdinalIgnoreCase);
    }


    private static string[] SplitDsl(string dsl)
    {
        var parts = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < dsl.Length; i++)
        {
            char c = dsl[i];
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
            }
            else if (c == '|' && depth == 0)
            {
                parts.Add(dsl.Substring(start, i - start));
                start = i + 1;
            }
        }

        parts.Add(dsl.Substring(start));
        return parts.ToArray();
    }

    private static bool TryParseFunction(string dsl, out string name, out string[] args)
    {
        name = string.Empty;
        args = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(dsl))
        {
            return false;
        }

        int parenStart = dsl.IndexOf('(');
        if (parenStart < 0)
        {
            name = dsl.Trim();
            return true;
        }

        int parenEnd = dsl.LastIndexOf(')');
        if (parenEnd < parenStart)
        {
            return false;
        }

        name = dsl.Substring(0, parenStart).Trim();
        string argsText = dsl.Substring(parenStart + 1, parenEnd - parenStart - 1);
        args = SplitArguments(argsText);
        return true;
    }

    private static string[] SplitArguments(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var args = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                args.Add(text.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }

        args.Add(text.Substring(start).Trim());
        return args.ToArray();
    }

    private static string NormalizeDslText(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
    }

    private static string NormalizeConditionText(string text)
    {
        string normalized = NormalizeDslText(text);
        return normalized == "조건 없음" ? string.Empty : normalized;
    }

    private static string StripQuotes(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().Trim('"');
    }

    private static List<int> ParseIntList(string[] args, int index)
    {
        var values = new List<int>();
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            return values;
        }

        string[] tokens = args[index].Split(new[] { '&', '|', ';', '/' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (int.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static EReactionTiming ParseReactionTiming(string[] args, int index, EReactionTiming fallback)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            return fallback;
        }

        return Enum.TryParse(args[index], true, out EReactionTiming timing) ? timing : fallback;
    }

    private static bool ParseBool(string[] args, int index, bool fallback)
    {
        if (index >= args.Length)
        {
            return fallback;
        }

        if (bool.TryParse(args[index], out bool value))
        {
            return value;
        }

        if (int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric))
        {
            return numeric != 0;
        }

        return fallback;
    }

    private static int ParseInt(string[] args, int index, int fallback)
    {
        if (index >= args.Length)
        {
            return fallback;
        }

        return int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;
    }

    private static float ParsePercentFloat(string[] args, int index, float fallback)
    {
        float value = ParseFloat(args, index, fallback);
        return value > 1f ? value / 100f : value;
    }

    private static float ParseProbabilityDelta(string[] args, int index, float fallback)
    {
        float value = ParseFloat(args, index, fallback);
        return Mathf.Abs(value) >= 1f ? value / 100f : value;
    }

    private static float ParsePercentValue(string[] args, int index, float fallback)
    {
        if (index >= args.Length)
        {
            return fallback;
        }

        string raw = StripQuotes(args[index]).TrimEnd('%');
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : fallback;
    }

    private static float ParseFloat(string[] args, int index, float fallback)
    {
        if (index >= args.Length)
        {
            return fallback;
        }

        string raw = StripQuotes(args[index]).TrimEnd('%');
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : fallback;
    }

    private static T ParseEnumOrInt<T>(string[] args, int index, T fallback) where T : struct, Enum
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            return fallback;
        }

        string raw = args[index].Trim();
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric))
        {
            return (T)Enum.ToObject(typeof(T), numeric);
        }

        return Enum.TryParse(raw, true, out T value) ? value : fallback;
    }

    private static EPlayerJob ParsePlayerJob(string raw, EPlayerJob fallback)
    {
        string normalized = StripQuotes(raw);
        switch (normalized)
        {
            case "전사":
                return EPlayerJob.Warrior;
            case "드워프":
                return EPlayerJob.Dwarf;
            case "궁수":
                return EPlayerJob.Archer;
            case "사제":
                return EPlayerJob.Priest;
            case "도적":
                return EPlayerJob.Rogue;
            case "공용":
            case "전체":
                return EPlayerJob.None;
        }

        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric))
        {
            return (EPlayerJob)numeric;
        }

        return Enum.TryParse(normalized, true, out EPlayerJob job) ? job : fallback;
    }

    private static EArtifactGameModelFloatStat ParseSlotProbabilityStat(string[] args, int index, EArtifactGameModelFloatStat fallback)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            return fallback;
        }

        string raw = StripQuotes(args[index]);
        switch (raw)
        {
            case "Success":
            case "성공":
                return EArtifactGameModelFloatStat.SuccessProbability;
            case "GreatSuccess":
            case "대성공":
                return EArtifactGameModelFloatStat.GreatSuccessProbability;
            case "UltraSuccess":
            case "초대성공":
            case "대대성공":
                return EArtifactGameModelFloatStat.UltraSuccessProbability;
            case "Fail":
            case "Failure":
            case "실패":
            case "대실패":
                return EArtifactGameModelFloatStat.FailureProbability;
            default:
                return Enum.TryParse(raw, true, out EArtifactGameModelFloatStat stat) ? stat : fallback;
        }
    }

    private static EStatusType ParseStatusType(string[] args, int index, EStatusType fallback)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            return fallback;
        }

        string raw = StripQuotes(args[index]);
        switch (raw)
        {
            case "Weak":
                return EStatusType.Weakening;
            case "Mark":
                return EStatusType.Marking;
            case "Frost":
                return EStatusType.Frost;
        }

        return ParseEnumOrInt(args, index, fallback);
    }

    private static EStatusCategory ParseStatusCategory(string[] args, int index, EStatusCategory fallback)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            return fallback;
        }

        string raw = StripQuotes(args[index]);
        return Enum.TryParse(raw, true, out EStatusCategory value) ? value : fallback;
    }

    private static int ParseEnemyTargetCount(string[] args, int index, int fallback)
    {
        if (index >= args.Length)
        {
            return fallback;
        }

        string raw = args[index];
        if (TryParseFunction(raw, out string name, out string[] selectorArgs) &&
            string.Equals(name, "EnemyRandom", StringComparison.OrdinalIgnoreCase))
        {
            return ParseInt(selectorArgs, 0, fallback);
        }

        return ParseInt(args, index, fallback);
    }

    private static bool TryParseTargetSelector(string[] args, int index, out TargetSelector selector)
    {
        selector = null;
        if (index >= args.Length)
        {
            return false;
        }

        return TryParseTargetSelector(args[index], out selector);
    }

    private static bool TryParseTargetSelector(string text, out TargetSelector selector)
    {
        selector = null;

        if (string.IsNullOrWhiteSpace(text) || !TryParseFunction(text, out string name, out string[] args))
        {
            return false;
        }

        switch (name)
        {
            case "EnemyRandom":
            {
                var randomSelector = new EnemyRandomTargetSelector();
                SetField(randomSelector, "_randomTargetCount", ParseInt(args, 0, 1));
                selector = randomSelector;
                return true;
            }
            case "AllEnemies":
            case "AllEnemy":
                selector = new AllEnemyTargetSelector();
                return true;
            case "AllPlayers":
                selector = new AllPlayerTargetSelector();
                return true;
            case "Self":
                selector = new SelfTargetSelector();
                return true;
            case "RecentlyCaster":
                selector = new RecentlyCasterSelector();
                return true;
            default:
                return false;
        }
    }

    private static SO_StatusData FindStatusData(EStatusType statusType)
    {
        string[] guids = AssetDatabase.FindAssets("t:SO_StatusData");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SO_StatusData status = AssetDatabase.LoadAssetAtPath<SO_StatusData>(assetPath);
            if (status != null && status.StatusType == statusType)
            {
                return status;
            }
        }

        return null;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        Type currentType = target.GetType();
        while (currentType != null)
        {
            FieldInfo field = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            currentType = currentType.BaseType;
        }

        Debug.LogWarning($"[ArtifactDataImporter] Missing field: {target.GetType().Name}.{fieldName}");
    }
}
#endif
