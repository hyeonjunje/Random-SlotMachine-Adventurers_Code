#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 범용 게임 DSL 파서.
/// "GoldDelta(-50)|NextPage(1)" 같은 DSL 문자열을 Effect/Condition 객체로 변환합니다.
/// 이벤트, 스킬, 유물 등 여러 시스템에서 재사용 가능합니다.
/// </summary>
public static class GameDSLParser
{
    #region ── Public API ──

    /// <summary>
    /// DSL 문자열을 파싱하여 Effect 배열을 반환합니다.
    /// 여러 Effect는 | 로 구분합니다. 예: "GoldDelta(-50)|NextPage(1)"
    /// </summary>
    public static Effect[] ParseEffects(string dsl)
    {
        if (string.IsNullOrWhiteSpace(dsl)) return Array.Empty<Effect>();

        string[] parts = SplitDSL(dsl);
        var effects = new List<Effect>();

        foreach (string part in parts)
        {
            Effect effect = ParseSingleEffect(part.Trim());
            if (effect != null) effects.Add(effect);
        }

        return effects.ToArray();
    }

    /// <summary>
    /// DSL 문자열을 파싱하여 Condition 객체를 반환합니다.
    /// 빈 문자열이면 null을 반환합니다.
    /// </summary>
    public static Condition ParseCondition(string dsl)
    {
        if (string.IsNullOrWhiteSpace(dsl)) return null;
        return ParseSingleCondition(dsl.Trim());
    }

    /// <summary>
    /// DSL 검사 결과 구조체.
    /// </summary>
    public struct DslValidationIssue
    {
        public string FuncName;
        public string[] Args;
        public bool IsUnknown;        // 이름 없음
        public bool IsArgMismatch;    // 인자 개수 불일치
        public bool IsInvalidFormat;  // 괄호() 없는 잘못된 포맷
    }

    /// <summary>
    /// Effect DSL 문자열을 검증합니다. 실제 객체는 생성하지 않습니다.
    /// </summary>
    public static List<DslValidationIssue> ValidateEffects(string dsl)
    {
        var issues = new List<DslValidationIssue>();
        if (string.IsNullOrWhiteSpace(dsl)) return issues;

        foreach (string part in SplitDSL(dsl))
        {
            string trimmed = part.Trim();
            // 괄호()가 없으면 잘못된 포맷
            if (!trimmed.Contains("("))
            {
                issues.Add(new DslValidationIssue { FuncName = trimmed, IsInvalidFormat = true });
                continue;
            }
            var (func, args) = ParseDSLFunction(trimmed);
            var issue = ValidateSingleEffect(func, args);
            if (issue.HasValue) issues.Add(issue.Value);
        }
        return issues;
    }

    /// <summary>
    /// Condition DSL 문자열을 검증합니다. 실제 객체는 생성하지 않습니다.
    /// </summary>
    public static List<DslValidationIssue> ValidateCondition(string dsl)
    {
        var issues = new List<DslValidationIssue>();
        if (string.IsNullOrWhiteSpace(dsl)) return issues;

        string trimmed = dsl.Trim();
        // 괄호()가 없으면 잘못된 포맷
        if (!trimmed.Contains("("))
        {
            issues.Add(new DslValidationIssue { FuncName = trimmed, IsInvalidFormat = true });
            return issues;
        }
        var (func, args) = ParseDSLFunction(trimmed);
        var issue = ValidateSingleCondition(func, args);
        if (issue.HasValue) issues.Add(issue.Value);
        return issues;
    }

    private static DslValidationIssue? ValidateSingleEffect(string func, string[] args)
    {
        // 1. 함수 이름 존재 여부 확인
        if (!EffectOwnArgCount.TryGetValue(func, out int ownCount))
            return new DslValidationIssue { FuncName = func, Args = args, IsUnknown = true };

        // 2. 인자 개수 확인
        // 가변 인자(-1)인 경우 (현재 AddKeyword만 해당)
        if (ownCount == -1)
        {
            // AddKeyword는 최소 1개 필요
            if (args.Length < 1)
                return new DslValidationIssue { FuncName = func, Args = args, IsArgMismatch = true };
            return null;
        }

        // 고정 인자 함수인 경우: [전용 인자] + [공통 후미 인자 0~3개]
        // 최소: 전용 인자 개수
        // 최대: 전용 인자 개수 + 3 (TargetSelector, DelayTime, TargetEffect)
        int min = ownCount;
        int max = ownCount + 3;

        if (args.Length < min || args.Length > max)
            return new DslValidationIssue { FuncName = func, Args = args, IsArgMismatch = true };

        // 3. 공통 후미 인자 중 TargetSelector(첫 번째 후미 인자) 검증
        if (args.Length > ownCount)
        {
            string selectorArg = args[ownCount];
            if (!string.IsNullOrWhiteSpace(selectorArg))
            {
                // TargetSelector는 반드시 괄호()를 포함해야 함
                if (!selectorArg.Contains("("))
                {
                    return new DslValidationIssue { FuncName = selectorArg, IsInvalidFormat = true };
                }

                // TargetSelector 이름 및 인자 검증
                var (sName, sArgs) = ParseDSLFunction(selectorArg);
                if (!IsValidTargetSelectorName(sName))
                {
                    return new DslValidationIssue { FuncName = sName, IsUnknown = true };
                }
            }
        }

        return null;
    }

    private static bool IsValidTargetSelectorName(string name)
    {
        return name switch
        {
            "PlayerPartySelector" => true,
            "SelfTargetSelector"  => true,
            "Self"                => true,
            "AllEnemyTargetSelector"  => true,
            "AllEnemies"              => true,
            "AllEnemy"                => true,
            "AllPlayerTargetSelector" => true,
            "AllPlayers"              => true,
            "EnemyRandomTargetSelector" => true,
            "EnemyRandom"               => true,
            "EnemyLowestHpSelector" => true,
            "EnemyBothSidesSelector" => true,
            "TargetInBattleSelector" => true,
            "RecentlyCasterSelector" => true,
            "ExplicitTargetsSelector" => true,
            "AdverbTargetSelector"    => true,
            "PlayerRandomTargetSelector" => true,
            "PlayerRandom" => true,
            _ => false
        };
    }

    private static DslValidationIssue? ValidateSingleCondition(string func, string[] args)
    {
        int expected = func switch
        {
            "HaveGold"   => 1,
            "HpLower"    => 2,
            "HpUpper"    => 2,
            "MaxHp"      => 1,
            "EnemyAlone" => 0,
            _            => -2
        };

        if (expected == -2)
            return new DslValidationIssue { FuncName = func, Args = args, IsUnknown = true };

        if (expected >= 0 && args.Length != expected)
            return new DslValidationIssue { FuncName = func, Args = args, IsArgMismatch = true };

        return null;
    }

    #endregion

    #region ── DSL 분리 ──

    /// <summary>
    /// | 로 DSL을 분리합니다. 단, 괄호 안의 | 는 무시합니다.
    /// </summary>
    private static string[] SplitDSL(string dsl)
    {
        var parts = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < dsl.Length; i++)
        {
            char c = dsl[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == '|' && depth == 0)
            {
                parts.Add(dsl.Substring(start, i - start));
                start = i + 1;
            }
        }

        parts.Add(dsl.Substring(start));
        return parts.ToArray();
    }

    /// <summary>
    /// DSL 구문에서 함수명과 인자들을 추출합니다.
    /// 예: "GoldDelta(-50)" → ("GoldDelta", ["-50"])
    /// </summary>
    private static (string funcName, string[] args) ParseDSLFunction(string dsl)
    {
        int parenStart = dsl.IndexOf('(');
        if (parenStart < 0) return (dsl, Array.Empty<string>());

        string funcName = dsl.Substring(0, parenStart);
        string argsStr = dsl.Substring(parenStart + 1, dsl.Length - parenStart - 2);

        if (string.IsNullOrWhiteSpace(argsStr)) return (funcName, Array.Empty<string>());

        // 괄호 안에 콤마가 있을 수 있으므로 depth 기반 분리
        var args = new List<string>();
        int depth = 0;
        int argStart = 0;
        for (int i = 0; i < argsStr.Length; i++)
        {
            if (argsStr[i] == '(') depth++;
            else if (argsStr[i] == ')') depth--;
            else if (argsStr[i] == ',' && depth == 0)
            {
                args.Add(argsStr.Substring(argStart, i - argStart).Trim());
                argStart = i + 1;
            }
        }
        args.Add(argsStr.Substring(argStart).Trim());
        return (funcName, args.ToArray());
    }

    #endregion

    #region ── Effect 파싱 ──

    // ── 각 Effect 전용 인자 수 ─────────────────────────────────────────
    // 공통 후미 인자: (TargetSelector DSL?, float DelayTime=0, string TargetEffect="")
    // 예: Heal(10, 0.2, PlayerPartySelector(), 0.5, "hitEffect")
    //          ↑전용(2개)  ↑TargetSelector     ↑DelayTime ↑TargetEffect
    private static readonly Dictionary<string, int> EffectOwnArgCount = new()
    {
        { "GoldDelta",   1 }, // amount
        { "SetGold",     1 }, // amount
        { "NextPage",    1 }, // id
        { "ClearNode",   0 },
        { "Heal",        2 }, // formulaType, value
        { "Damage",      2 }, // formulaType, value
        { "ChangeStat",  3 }, // statType, modType, value
        { "LevelUp",     1 }, // diff
        { "AddKeyword",  -1 }, // 가변 (특수 처리)
        { "StartBattle", 2 }, // matchupName, battleType
        { "Shield",      2 }, // formulaType, value
        { "EarnMoney",   2 }, // amount, changeType
        { "ChangePlayerLevel",   1 }, // levelDiff
        { "RemoveRandomSlotMachineKeyword",   0 },
        { "LevelUpRandomKeyword",   0 },
        { "AddRandomArtifact",   0 },
        { "RemoveRandomArtifact",   0 },
        { "AddDelayedStatus",   3 }, // 확률, EStatusType, value
    };

    private static Effect ParseSingleEffect(string dsl)
    {
        var (func, args) = ParseDSLFunction(dsl);

        Effect effect = func switch
        {
            "GoldDelta"   => CreateGoldDeltaEffect(args),
            "SetGold"     => CreateSetGoldEffect(args),
            "NextPage"    => CreateNextPageEffect(args),
            "ClearNode"   => CreateClearNodeEffect(),
            "Heal"        => CreateHealEffect(args),
            "Damage"      => CreateDamageEffect(args),
            "ChangeStat"  => CreateChangeStatEffect(args),
            "LevelUp"     => CreateLevelUpEffect(args),
            "AddKeyword"  => CreateAddKeywordEffect(args),
            "StartBattle" => CreateStartBattleEffect(args),
            "Shield"      => CreateShieldEffect(args),
            "EarnMoney"   => CreateEarnMoneyEffect(args),
            "ChangePlayerLevel" => CreateChangePlayerLevelEffect(args),
            "RemoveRandomSlotMachineKeyword" => CreateRemoveRandomSlotMachineKeywordEffect(args),
            "LevelUpRandomKeyword" => CreateLevelUpRandomKeywordEffect(args),
            "AddRandomArtifact" => CreateAddRandomArtifactEffect(args),
            "RemoveRandomArtifact" => CreateRemoveRandomArtifactEffect(args),
            "AddDelayedStatus" => CreateAddDelayedStatusEffect(args),
            _ => null
        };

        if (effect == null)
        {
            Debug.LogWarning($"[GameDSLParser] 알 수 없는 Effect DSL: {dsl}");
            return null;
        }

        // 공통 후미 인자 적용 (TargetSelector, DelayTime, TargetEffect)
        ApplyCommonEffectArgs(effect, func, args);
        return effect;
    }

    /// <summary>
    /// 공통 후미 인자를 파싱하여 effect에 적용합니다.
    /// 전용 인자 개수 이후부터: (TargetSelectorDSL?, float DelayTime=0, string TargetEffect="")
    /// </summary>
    private static void ApplyCommonEffectArgs(Effect effect, string func, string[] args)
    {
        if (!EffectOwnArgCount.TryGetValue(func, out int ownCount) || ownCount < 0)
            return; // 가변 개수 함수는 공통 처리 생략

        int idx = ownCount;
        if (idx >= args.Length) return;

        // TargetSelector (공통 후미 첫 번째 인자 - 괄호를 포함한 DSL 형태)
        string selectorArg = args[idx];
        if (!string.IsNullOrWhiteSpace(selectorArg) && selectorArg.Contains("("))
        {
            TargetSelector selector = ParseTargetSelector(selectorArg);
            if (selector != null)
                SetField(effect, "<TargetSelector>k__BackingField", selector);
        }
        idx++;

        // DelayTime (기본값 0)
        if (idx < args.Length && float.TryParse(args[idx], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float delay))
        {
            SetField(effect, "<DelayTime>k__BackingField", delay);
        }
        idx++;

        // TargetEffect (기본값 "")
        if (idx < args.Length && !string.IsNullOrWhiteSpace(args[idx]))
        {
            SetField(effect, "<TargetEffect>k__BackingField", args[idx].Trim('"', '\''));
        }
    }

    // ── TargetSelector 파싱 ───────────────────────────────────────────
    private static TargetSelector ParseTargetSelector(string dsl)
    {
        var (name, args) = ParseDSLFunction(dsl.Trim());
        switch (name)
        {
            case "PlayerPartySelector":
                return new PlayerPartySelector();
            case "SelfTargetSelector":
            case "Self":
                return new SelfTargetSelector();
            case "AllEnemyTargetSelector":
            case "AllEnemies":
            case "AllEnemy":
                return new AllEnemyTargetSelector();
            case "AllPlayerTargetSelector":
            case "AllPlayers":
                return new AllPlayerTargetSelector();
            case "EnemyRandomTargetSelector":
            case "EnemyRandom":
            {
                var sel = new EnemyRandomTargetSelector();
                if (args.Length >= 1 && int.TryParse(args[0], out int count))
                    SetField(sel, "_randomTargetCount", count);
                return sel;
            }
            case "EnemyLowestHpSelector":
                return new EnemyLowestHpSelector();
            case "EnemyBothSidesSelector":
                return new EnemyBothSidesSelector();
            case "TargetInBattleSelector":
                return new TargetInBattleSelector();
            case "RecentlyCasterSelector":
                return new RecentlyCasterSelector();
            case "PlayerRandomTargetSelector":
            case "PlayerRandom":
                {
                    var sel = new PlayerRandomTargetSelector();
                    if (args.Length >= 1 && int.TryParse(args[0], out int count))
                        SetField(sel, "_randomPlayerCount", count);
                    return sel;
                }
            default:
                Debug.LogWarning($"[GameDSLParser] 알 수 없는 TargetSelector DSL: {dsl}");
                return null;
        }
    }

    // ── GoldDelta(amount) ──
    private static Effect CreateGoldDeltaEffect(string[] args)
    {
        var effect = new ApplyGoldDeltaEffect();
        SetField(effect, "_amount", int.Parse(args[0]));
        return effect;
    }

    private static Effect CreateSetGoldEffect(string[] args)
    {
        var effect = new SetGoldEffect();
        SetField(effect, "_amount", int.Parse(args[0]));
        return effect;
    }

    // ── NextPage(id) ──
    private static Effect CreateNextPageEffect(string[] args)
    {
        var effect = new ChangeNextEventPageEffect();
        SetField(effect, "NextId", int.Parse(args[0]));
        return effect;
    }

    // ── ClearNode() ──
    private static Effect CreateClearNodeEffect()
    {
        return new ClearNodeEffect();
    }

    // ── Heal(formulaType, value, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateHealEffect(string[] args)
    {
        var effect = new ApplyHealingEffect();
        var formula = new HealingFormula((EHealingFormulaType)int.Parse(args[0]), float.Parse(args[1]));
        SetField(effect, "_healingFormula", formula);
        return effect;
    }

    // ── Damage(formulaType, value, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateDamageEffect(string[] args)
    {
        var effect = new DealDamageEffect();
        var formula = new DamageFormula((EDamageFormulaType)int.Parse(args[0]), float.Parse(args[1]));
        SetField(effect, "_damageFormula", formula);
        return effect;
    }

    // ── ChangeStat(statType, modType, value, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateChangeStatEffect(string[] args)
    {
        var effect = new ChangeStatValueEffect();
        SetField(effect, "_statType", (EStatType)int.Parse(args[0]));
        SetField(effect, "_statModType", (EStatModType)int.Parse(args[1]));
        SetField(effect, "_value", float.Parse(args[2]));
        return effect;
    }

    // ── LevelUp(diff, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateLevelUpEffect(string[] args)
    {
        var effect = new ChangePlayerLevelEffect();
        SetField(effect, "_levelDiff", int.Parse(args[0]));
        return effect;
    }

    // ── AddKeyword(keyword[,Random,typeFlag[,probability]]) ─ 가변 인수, 공통 후미 미지원 ──
    private static Effect CreateAddKeywordEffect(string[] args)
    {
        var effect = new AddSlotMachineKeywordEffect();

        if (args.Length >= 1 && args[0] == "Random")
        {
            SetField(effect, "_isRandom", true);
            if (args.Length >= 2)
                SetField(effect, "_slotMachineKeywordTypeFlag", (EKeywordType)int.Parse(args[1]));
            if (args.Length >= 3)
                SetField(effect, "_probability", float.Parse(args[2]));
        }
        else if (args.Length >= 1)
        {
            SetField(effect, "_keyword", (EKeyword)int.Parse(args[0]));
        }

        return effect;
    }

    // ── StartBattle(matchupName, battleType, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateStartBattleEffect(string[] args)
    {
        var effect = new StartBattleEffect();

        string matchupName = args[0];
        SO_MatchupData matchup = FindAsset<SO_MatchupData>(matchupName);
        if (matchup == null) Debug.LogWarning($"[GameDSLParser] SO_MatchupData를 찾을 수 없습니다: {matchupName}");

        SetField(effect, "_matchData", matchup);
        if (args.Length >= 2)
            SetField(effect, "_battleType", (EMapNodeType)int.Parse(args[1]));

        return effect;
    }

    // ── Shield(formulaType, value, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateShieldEffect(string[] args)
    {
        var effect = new AddShieldEffect();
        var formula = new ShieldFormula((EShieldFormulaType)int.Parse(args[0]), float.Parse(args[1]));
        SetField(effect, "_shieldFormula", formula);
        return effect;
    }

    // ── EarnMoney(amount, changeType, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateEarnMoneyEffect(string[] args)
    {
        var effect = new ChangeEarnedMoneyAmountEffect();
        SetField(effect, "_amount", float.Parse(args[0]));
        SetField(effect, "_changeType", (EChangeType)int.Parse(args[1]));
        return effect;
    }

    // ── EarnMoney(levelDiff, [TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateChangePlayerLevelEffect(string[] args)
    {
        var effect = new ChangePlayerLevelEffect();
        SetField(effect, "_levelDiff", float.Parse(args[0]));
        return effect;
    }

    // ── EarnMoney([TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateRemoveRandomSlotMachineKeywordEffect(string[] args)
    {
        var effect = new RemoveRandomSlotMachineKeywordEffect();
        return effect;
    }

    // ── EarnMoney([TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateLevelUpRandomKeywordEffect(string[] args)
    {
        var effect = new LevelUpRandomKeywordEffect();
        return effect;
    }

    // ── EarnMoney([TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateAddRandomArtifactEffect(string[] args)
    {
        var effect = new AddRandomArtifactEffect();
        return effect;
    }

    // ── EarnMoney([TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateRemoveRandomArtifactEffect(string[] args)
    {
        var effect = new RemoveRandomArtifactEffect();
        return effect;
    }

    // ── EarnMoney([TargetSelector], [DelayTime=0], [TargetEffect=""]) ──
    private static Effect CreateAddDelayedStatusEffect(string[] args)
    {
        var effect = new AddDelayedStatusEffect();
        SetField(effect, "_probability", float.Parse(args[0]));
        SetField(effect, "_statusType", (EStatusType)int.Parse(args[1]));
        SetField(effect, "_value", int.Parse(args[2]));
        return effect;
    }
    #endregion

    #region ── Condition 파싱 ──

    private static Condition ParseSingleCondition(string dsl)
    {
        var (func, args) = ParseDSLFunction(dsl);

        switch (func)
        {
            case "HaveGold": return CreateHaveGoldCondition(args);
            case "HpLower": return CreateHpLowerCondition(args);
            case "HpUpper": return CreateHpUpperCondition(args);
            case "MaxHp": return CreateMaxHpCondition(args);
            case "EnemyAlone": return new IsEnemyAloneCondition();
            default:
                Debug.LogWarning($"[GameDSLParser] 알 수 없는 Condition DSL: {dsl}");
                return null;
        }
    }

    // ── HaveGold(amount) ──
    private static Condition CreateHaveGoldCondition(string[] args)
    {
        var cond = new IsHaveGoldCondition();
        SetField(cond, "_needGold", int.Parse(args[0]));
        return cond;
    }

    // ── HpLower(flatHp, probability) ──
    private static Condition CreateHpLowerCondition(string[] args)
    {
        var cond = new IsCheckHpLowerCondition();
        SetField(cond, "_flatHp", int.Parse(args[0]));
        SetField(cond, "_probability", float.Parse(args[1]));
        return cond;
    }

    // ── HpUpper(flatHp, probability) ──
    private static Condition CreateHpUpperCondition(string[] args)
    {
        var cond = new IsCheckHpUpperCondition();
        SetField(cond, "_flatHp", int.Parse(args[0]));
        SetField(cond, "_probability", float.Parse(args[1]));
        return cond;
    }

    // ── MaxHp(flatHp) ──
    private static Condition CreateMaxHpCondition(string[] args)
    {
        var cond = new IsCheckMaxHpCondition();
        SetField(cond, "_flatHp", int.Parse(args[0]));
        return cond;
    }

    #endregion

    #region ── 유틸리티 ──

    /// <summary>
    /// 리플렉션으로 private/public 필드에 값을 설정합니다.
    /// 상속된 클래스의 필드도 탐색합니다.
    /// </summary>
    private static void SetField(object obj, string fieldName, object value)
    {
        Type type = obj.GetType();

        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }

            type = type.BaseType;
        }

        Debug.LogWarning($"[GameDSLParser] 필드를 찾을 수 없습니다: {obj.GetType().Name}.{fieldName}");
    }

    /// <summary>
    /// AssetDatabase에서 이름으로 에셋을 찾습니다.
    /// </summary>
    public static T FindAsset<T>(string assetName) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null && asset.name == assetName) return asset;
        }
        return null;
    }

    #endregion
}
#endif
