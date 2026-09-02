using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OnPostBattleRewardCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is GrantPostBattleGoldGA;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<GrantPostBattleGoldGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<GrantPostBattleGoldGA>(reaction, _reactionTiming);
    }
}

public class OnKillEnemyCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not EnemyDeadGA enemyDeadGA || enemyDeadGA.Killer == null || _owner == null)
        {
            return false;
        }

        return enemyDeadGA.Killer == _owner ||
               (enemyDeadGA.Killer is PlayerView && _owner is PlayerView);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<EnemyDeadGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<EnemyDeadGA>(reaction, _reactionTiming);
    }
}

public class OnUseKeywordCondition : Condition
{
    [SerializeField] private EPlayerJob _job = EPlayerJob.None;
    [SerializeField] private bool _matchAnyJob = false;
    [SerializeField] private string _keywordText = string.Empty;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is DealDamageGA damageGA && damageGA.IsArtifactGenerated)
        {
            return false;
        }

        if (!TryResolvePlayerView(gameAction, out PlayerView playerView))
        {
            return false;
        }

        if (!_matchAnyJob && playerView.Player.PlayerData.PlayerJob != _job)
        {
            return false;
        }

        Skill currentSkill = BattleSystem.Instance?.CurrentExecutingBattleAct?.Skill;
        return currentSkill != null && currentSkill.UsesKeywordText(_keywordText);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<ApplyHealingGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<AddShieldGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<ChangeStatValueGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<ApplyHealingGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<AddShieldGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<ChangeStatValueGA>(reaction, _reactionTiming);
    }

    private static bool TryResolvePlayerView(GameAction gameAction, out PlayerView playerView)
    {
        playerView = null;

        switch (gameAction)
        {
            case DealDamageGA damageGA when damageGA.Caster is PlayerView damageCaster:
                playerView = damageCaster;
                return true;
            case ApplyHealingGA healingGA when healingGA.Caster is PlayerView healingCaster:
                playerView = healingCaster;
                return true;
            case AddShieldGA shieldGA when shieldGA.Caster is PlayerView shieldCaster:
                playerView = shieldCaster;
                return true;
            case AddStatusGA addStatusGA when addStatusGA.Caster is PlayerView statusCaster:
                playerView = statusCaster;
                return true;
            case ChangeStatValueGA statGA when statGA.Caster is PlayerView statCaster:
                playerView = statCaster;
                return true;
            default:
                return false;
        }
    }
}

public class OnOurSideDealDamageCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is DealDamageGA damageGA && damageGA.Caster is PlayerView;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class ActionTargetHasStatusCondition : Condition
{
    [SerializeField] private EStatusType _statusType = EStatusType.Poison;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not DealDamageGA damageGA || damageGA.Targets == null)
        {
            return false;
        }

        if (_owner != null &&
            damageGA.Caster != _owner &&
            !(damageGA.Caster is PlayerView && _owner is PlayerView))
        {
            return false;
        }

        return damageGA.Targets.Any(target => target != null && target.Character.IsStatus(_statusType));
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class ActionActorHasStatusCondition : Condition
{
    [SerializeField] private EStatusType _statusType = EStatusType.Poison;
    [SerializeField] private bool _checkAttacker = true;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not DealDamageGA damageGA)
        {
            return false;
        }

        CharacterView actor = _checkAttacker ? damageGA.Caster : _owner;
        if (actor == null || !actor.Character.IsStatus(_statusType))
        {
            return false;
        }

        return damageGA.Targets != null &&
               damageGA.Targets.Any(target => target != null && target.Character.BattleSideType == EBattleSideType.OurSide);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class ActionTargetHpPercentCondition : Condition
{
    [SerializeField, Range(0f, 1f)] private float _targetPercent = 1f;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not DealDamageGA damageGA || damageGA.Targets == null || damageGA.Caster is not PlayerView)
        {
            return false;
        }

        foreach (CharacterView target in damageGA.Targets)
        {
            HealthController health = target?.Character?.HealthController;
            if (health == null || health.MaxHp <= 0)
            {
                continue;
            }

            if (_targetPercent >= 1f)
            {
                if (health.CurrentHp >= health.MaxHp)
                {
                    return true;
                }

                continue;
            }

            if ((health.CurrentHp / (float)health.MaxHp) <= _targetPercent)
            {
                return true;
            }
        }

        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class GoldAmountCondition : Condition
{
    [SerializeField] private int _amount = 0;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is ApplyGoldDeltaGA &&
               UIHudSystem.Instance != null &&
               UIHudSystem.Instance.CurrentGold == _amount;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<ApplyGoldDeltaGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<ApplyGoldDeltaGA>(reaction, _reactionTiming);
    }
}

public class RerollCountCondition : Condition
{
    [SerializeField] private int _count = 0;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is EndTurnGA &&
               SlotMachineSystem.Instance != null &&
               SlotMachineSystem.Instance.CurrentTurnRerollCount == _count;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<EndTurnGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<EndTurnGA>(reaction, _reactionTiming);
    }
}

public class ReceiveStatusCondition : Condition
{
    [SerializeField] private EStatusCategory _statusCategory = EStatusCategory.Debuff;
    [SerializeField] private bool _enemyCasterOnly = true;
    [SerializeField] private bool _oncePerBattle = true;

    private bool _triggered;
    private Action<StartBattleGA> _resetHandler;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (_oncePerBattle && _triggered)
        {
            return false;
        }

        if (gameAction is not AddStatusGA addStatusGA ||
            addStatusGA.Status == null ||
            addStatusGA.Status.StatusCategory != _statusCategory ||
            addStatusGA.Targets == null)
        {
            return false;
        }

        if (_enemyCasterOnly &&
            (addStatusGA.Caster == null || addStatusGA.Caster.Character.BattleSideType != EBattleSideType.EnemySide))
        {
            return false;
        }

        bool targetsOurSide = addStatusGA.Targets.Any(target =>
            target != null &&
            target.Character.BattleSideType == EBattleSideType.OurSide &&
            (_owner == null || target == _owner || target is PlayerView));

        if (targetsOurSide && _oncePerBattle)
        {
            _triggered = true;
        }

        return targetsOurSide;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _resetHandler = _ => _triggered = false;
        ActionSystem.SubscribeReaction<StartBattleGA>(_resetHandler, EReactionTiming.Post);
        ActionSystem.SubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        if (_resetHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartBattleGA>(_resetHandler, EReactionTiming.Post);
            _resetHandler = null;
        }

        ActionSystem.UnSubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }
}

public class OnAttackNoManaCostCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not DealDamageGA damageGA || damageGA.Caster is not PlayerView || damageGA.IsArtifactGenerated)
        {
            return false;
        }

        Skill skill = BattleSystem.Instance?.CurrentExecutingBattleAct?.Skill;
        return skill != null && skill.ManaCost <= 0;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class BattleAttackCountCondition : Condition
{
    [SerializeField] private int _targetCount = 1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is DealDamageGA damageGA &&
               damageGA.Caster is PlayerView &&
               !damageGA.IsArtifactGenerated &&
               BattleSystem.Instance != null &&
               BattleSystem.Instance.CurrentBattleAttackCount == _targetCount;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class TargetActCountCondition : Condition
{
    [SerializeField] private int _targetActCount = 1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not DealDamageGA damageGA || damageGA.Caster is not PlayerView || damageGA.Targets == null)
        {
            return false;
        }

        foreach (CharacterView target in damageGA.Targets)
        {
            if (target is EnemyView enemyView && enemyView.Enemy.EnemyAI.ActCount == _targetActCount)
            {
                return true;
            }
        }

        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class HpThresholdCrossedCondition : Condition
{
    [SerializeField] private int _flatHp = 0;
    [SerializeField, Range(0f, 1f)] private float _probability = 0f;
    [SerializeField] private bool _triggerOnce = true;

    private bool _triggered = false;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (_owner == null || (_triggerOnce && _triggered))
        {
            return false;
        }

        if (!IsRelevantAction(gameAction))
        {
            return false;
        }

        bool isBelow = IsBelowThreshold();
        if (isBelow && _triggerOnce)
        {
            _triggered = true;
        }

        return isBelow;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<ApplyHealingGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<ApplyHealingGA>(reaction, _reactionTiming);
    }

    private bool IsRelevantAction(GameAction gameAction)
    {
        if (gameAction is DealDamageGA damageGA)
        {
            foreach (CharacterView target in damageGA.Targets)
            {
                if (target == _owner || (target is PlayerView && _owner is PlayerView))
                {
                    return true;
                }
            }
        }

        if (gameAction is ApplyHealingGA healingGA)
        {
            foreach (CharacterView target in healingGA.Targets)
            {
                if (target == _owner || (target is PlayerView && _owner is PlayerView))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsBelowThreshold()
    {
        HealthController healthController = _owner.Character.HealthController;
        return healthController.CurrentHp <= _flatHp ||
               (healthController.CurrentHp / (float)healthController.MaxHp) <= _probability;
    }
}

public class LowHpDamageCondition : Condition
{
    [SerializeField] private int _flatHp = 0;
    [SerializeField, Range(0f, 1f)] private float _probability = 0f;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (_owner == null || gameAction is not DealDamageGA damageGA || damageGA.Caster == null)
        {
            return false;
        }

        bool isOurPartyCaster = damageGA.Caster is PlayerView && _owner is PlayerView;
        if (damageGA.Caster != _owner && !isOurPartyCaster)
        {
            return false;
        }

        HealthController healthController = _owner.Character.HealthController;
        return healthController.CurrentHp <= _flatHp ||
               (healthController.CurrentHp / (float)healthController.MaxHp) <= _probability;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

public class LowHpSlotSpinCondition : Condition
{
    [SerializeField] private int _flatHp = 0;
    [SerializeField, Range(0f, 1f)] private float _probability = 0f;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (_owner == null || gameAction is not SpinSlotMachineGA)
        {
            return false;
        }

        HealthController healthController = _owner.Character.HealthController;
        return healthController.CurrentHp <= _flatHp ||
               (healthController.CurrentHp / (float)healthController.MaxHp) <= _probability;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);
    }
}

public class OnSlotConfirmCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is StartAutoBattleGA;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<StartAutoBattleGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<StartAutoBattleGA>(reaction, _reactionTiming);
    }
}

public class OnAppearKeywordTierCondition : Condition
{
    [SerializeField] private int _rank = 1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return SlotMachineSystem.Instance != null &&
               SlotMachineSystem.Instance.CurrentResultHasKeywordRank(_rank);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<ChangeSlotMachineKeywordGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineLineGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<ChangeSlotMachineKeywordGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<RerollSlotMachineLineGA>(reaction, _reactionTiming);
    }
}

public class SlotTurnCountCondition : Condition
{
    [SerializeField] private int _interval = 1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (_interval <= 0 || BattleSystem.Instance == null || BattleSystem.Instance.TotalSlotConfirmCount <= 0)
        {
            return false;
        }

        if (BattleSystem.Instance.TotalSlotConfirmCount % _interval != 0)
        {
            return false;
        }

        return IsPlayerDrivenAction(gameAction);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<AddShieldGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<ApplyHealingGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<ChangeStatValueGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<ChangeEnemyActCountGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<AddShieldGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<ApplyHealingGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<ChangeStatValueGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<ChangeEnemyActCountGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }

    private static bool IsPlayerDrivenAction(GameAction gameAction)
    {
        return gameAction switch
        {
            DealDamageGA damageGA => damageGA.Caster is PlayerView,
            AddShieldGA shieldGA => shieldGA.Caster is PlayerView,
            ApplyHealingGA healingGA => healingGA.Caster is PlayerView,
            ChangeStatValueGA statGA => statGA.Caster == null || statGA.Caster is PlayerView,
            AddStatusGA addStatusGA => addStatusGA.Caster == null || addStatusGA.Caster is PlayerView,
            ChangeEnemyActCountGA => true,
            _ => false
        };
    }
}

public class SameKeywordUsedCondition : Condition
{
    [SerializeField] private int _requiredMatchCount = 2;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not DealDamageGA damageGA || damageGA.Caster is not PlayerView || BattleSystem.Instance == null)
        {
            return false;
        }

        Dictionary<EKeyword, int> counts = new Dictionary<EKeyword, int>();
        foreach (BattleAct battleAct in BattleSystem.Instance.CurrentConfirmedBattleActs)
        {
            if (battleAct == null || !battleAct.IsPlayer || battleAct.Skill == null)
            {
                continue;
            }

            foreach (EKeyword keyword in battleAct.Skill.GetUsedKeywords())
            {
                counts.TryGetValue(keyword, out int current);
                counts[keyword] = current + 1;
            }
        }

        return counts.Values.Any(value => value >= _requiredMatchCount);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}
