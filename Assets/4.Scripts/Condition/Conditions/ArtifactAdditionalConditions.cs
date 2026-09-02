using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AndCondition : Condition
{
    [SerializeReference] private List<Condition> _conditions = new List<Condition>();

    public override void SetOwner(CharacterView owner)
    {
        base.SetOwner(owner);

        foreach (Condition condition in _conditions)
        {
            condition?.SetOwner(owner);
        }
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _conditions.FirstOrDefault()?.SubscribeCondition(reaction);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        _conditions.FirstOrDefault()?.UnsubscribeCondition(reaction);
    }

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (_conditions == null || _conditions.Count == 0)
        {
            return false;
        }

        foreach (Condition condition in _conditions)
        {
            if (condition == null || !condition.SubConditionIsMet(gameAction))
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public class ManaAmountCondition : Condition
{
    [SerializeField] private float _amount = 0f;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return ManaSystem.Instance != null &&
               Mathf.Approximately(ManaSystem.Instance.CurrentMana, _amount);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}

[Serializable]
public class ShieldAmountCondition : Condition
{
    [SerializeField] private int _amount = 1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        HealthController healthController = _owner?.Character?.HealthController ?? CharacterSystem.Instance?.PartyHealth;
        return healthController != null && healthController.Shield >= _amount;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}

[Serializable]
public class BattleTypeCondition : Condition
{
    [SerializeField] private EMapNodeType _battleType = EMapNodeType.Monster;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return BattleSystem.Instance != null &&
               BattleSystem.Instance.CurrentBattleType == _battleType;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}

[Serializable]
public class OnShopPurchaseCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is PurchaseArtifactGA || gameAction is PurchaseKeywordGA || gameAction is PurchaseCharacterGA;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<PurchaseArtifactGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<PurchaseKeywordGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<PurchaseCharacterGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<PurchaseArtifactGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<PurchaseKeywordGA>(reaction, _reactionTiming);
        ActionSystem.UnSubscribeReaction<PurchaseCharacterGA>(reaction, _reactionTiming);
    }
}

[Serializable]
public class CumulativeGoldSpentCondition : Condition
{
    [SerializeField] private int _threshold = 1;
    [NonSerialized] private int _accumulatedSpent;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not ApplyGoldDeltaGA goldGA || goldGA.delta >= 0)
        {
            return false;
        }

        _accumulatedSpent += Mathf.Abs(goldGA.delta);
        if (_accumulatedSpent < Mathf.Max(1, _threshold))
        {
            return false;
        }

        _accumulatedSpent %= Mathf.Max(1, _threshold);
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<ApplyGoldDeltaGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<ApplyGoldDeltaGA>(reaction, _reactionTiming);
        _accumulatedSpent = 0;
    }
}

[Serializable]
public class CumulativeNewKeywordCondition : Condition
{
    [SerializeField] private int _threshold = 1;
    [NonSerialized] private int _count;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not AddSlotMachineKeywordGA)
        {
            return false;
        }

        _count++;
        if (_count < Mathf.Max(1, _threshold))
        {
            return false;
        }

        _count %= Mathf.Max(1, _threshold);
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<AddSlotMachineKeywordGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<AddSlotMachineKeywordGA>(reaction, _reactionTiming);
        _count = 0;
    }
}

[Serializable]
public class CumulativeKillEnemyCondition : Condition
{
    [SerializeField] private int _threshold = 1;
    [NonSerialized] private int _count;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not EnemyDeadGA)
        {
            return false;
        }

        _count++;
        if (_count < Mathf.Max(1, _threshold))
        {
            return false;
        }

        _count %= Mathf.Max(1, _threshold);
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<EnemyDeadGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<EnemyDeadGA>(reaction, _reactionTiming);
        _count = 0;
    }
}

[Serializable]
public class CumulativeRerollCondition : Condition
{
    [SerializeField] private int _threshold = 1;
    [NonSerialized] private int _count;
    [NonSerialized] private Action<RerollSlotMachineKeywordGA> _keywordHandler;
    [NonSerialized] private Action<RerollSlotMachineKeywordAddTokenGA> _addTokenHandler;
    [NonSerialized] private Action<RerollSlotMachineKeywordAddTokenInBattlePhaseGA> _battlePhaseHandler;
    [NonSerialized] private Action<RerollSlotMachineLineGA> _lineHandler;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is SpinSlotMachineGA &&
            (SlotMachineSystem.Instance == null || SlotMachineSystem.Instance.CurrentTurnRerollCount <= 0))
        {
            return false;
        }

        _count++;
        if (_count < Mathf.Max(1, _threshold))
        {
            return false;
        }

        _count %= Mathf.Max(1, _threshold);
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _keywordHandler = ga => reaction?.Invoke(ga);
        _addTokenHandler = ga => reaction?.Invoke(ga);
        _battlePhaseHandler = ga => reaction?.Invoke(ga);
        _lineHandler = ga => reaction?.Invoke(ga);

        ActionSystem.SubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordGA>(_keywordHandler, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(_addTokenHandler, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(_battlePhaseHandler, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineLineGA>(_lineHandler, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);
        if (_keywordHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordGA>(_keywordHandler, _reactionTiming);
            _keywordHandler = null;
        }

        if (_addTokenHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(_addTokenHandler, _reactionTiming);
            _addTokenHandler = null;
        }

        if (_battlePhaseHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(_battlePhaseHandler, _reactionTiming);
            _battlePhaseHandler = null;
        }

        if (_lineHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineLineGA>(_lineHandler, _reactionTiming);
            _lineHandler = null;
        }

        _count = 0;
    }
}

[Serializable]
public class KillerJobCondition : Condition
{
    [SerializeField] private EPlayerJob _job = EPlayerJob.None;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is EnemyDeadGA enemyDeadGA &&
               enemyDeadGA.Killer is PlayerView playerView &&
               playerView.Player.PlayerData.PlayerJob == _job;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}

[Serializable]
public class SlotMachineSuccessTypeCondition : Condition
{
    [SerializeField] private ESlotMachineSuccessType _successType = ESlotMachineSuccessType.GreatSuccess;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is SpinSlotMachineGA spinSlotMachineGA &&
               spinSlotMachineGA.SuccessType == _successType;
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

[Serializable]
public class BattleManaSpentCondition : Condition
{
    [SerializeField] private float _threshold = 1f;
    [NonSerialized] private float _spent;
    [NonSerialized] private Action<StartBattleGA> _resetHandler;
    [NonSerialized] private Action<SpendManaGA> _spendHandler;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is GrantPostBattleGoldGA && _spent >= Mathf.Max(0f, _threshold);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _resetHandler = _ => _spent = 0f;
        _spendHandler = spendManaGA =>
        {
            if (spendManaGA.Cost > 0f)
            {
                _spent += spendManaGA.Cost;
            }
        };

        ActionSystem.SubscribeReaction<StartBattleGA>(_resetHandler, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<SpendManaGA>(_spendHandler, EReactionTiming.Post);
        ActionSystem.SubscribeReaction<GrantPostBattleGoldGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        if (_resetHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartBattleGA>(_resetHandler, EReactionTiming.Pre);
            _resetHandler = null;
        }

        if (_spendHandler != null)
        {
            ActionSystem.UnSubscribeReaction<SpendManaGA>(_spendHandler, EReactionTiming.Post);
            _spendHandler = null;
        }

        ActionSystem.UnSubscribeReaction<GrantPostBattleGoldGA>(reaction, _reactionTiming);
        _spent = 0f;
    }
}

[Serializable]
public class PerfectBattleClearCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is GrantPostBattleGoldGA &&
               ArtifactRuntimeState.CurrentBattlePartyDamageTaken <= 0;
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

[Serializable]
public class FullHpBattleClearCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is GrantPostBattleGoldGA &&
               CharacterSystem.Instance?.PartyHealth != null &&
               CharacterSystem.Instance.PartyHealth.CurrentHp >= CharacterSystem.Instance.PartyHealth.MaxHp;
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

[Serializable]
public class ClearNodeTypeCountCondition : Condition
{
    [SerializeField] private EMapNodeType _nodeType = EMapNodeType.Event;
    [SerializeField] private int _threshold = 1;

    [NonSerialized] private int _count;
    [NonSerialized] private IDisposable _subscription;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _subscription = EventBus.Subscribe<StClearNodeEvent>(clearNodeEvent =>
        {
            if (clearNodeEvent.MapNodeType != _nodeType)
            {
                return;
            }

            _count++;
            if (_count < Mathf.Max(1, _threshold))
            {
                return;
            }

            _count %= Mathf.Max(1, _threshold);
            reaction?.Invoke(new ClearNodeGA());
        });
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        _subscription?.Dispose();
        _subscription = null;
        _count = 0;
    }
}

[Serializable]
public class ArtifactCountMultipleCondition : Condition
{
    [SerializeField] private int _threshold = 5;

    [NonSerialized] private IDisposable _subscription;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        int threshold = Mathf.Max(1, _threshold);
        int count = ArtifactSystem.Instance?.OwnedArtifacts.Count ?? 0;
        return count > 0 && count % threshold == 0;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _subscription = EventBus.Subscribe<StArtifactChangedEvent>(artifactChangedEvent =>
        {
            if (artifactChangedEvent.ChangeType == EArtifactChangeType.Added)
            {
                reaction?.Invoke(new ClearNodeGA());
            }
        });
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}

[Serializable]
public class OnUseKeywordAfterRerollCondition : Condition
{
    [SerializeField] private string _keywordText = string.Empty;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (!TryResolvePlayerView(gameAction, out PlayerView playerView))
        {
            return false;
        }

        if (_owner is PlayerView ownerView && playerView != ownerView)
        {
            return false;
        }

        Skill currentSkill = BattleSystem.Instance?.CurrentExecutingBattleAct?.Skill;
        if (currentSkill == null || !currentSkill.UsesKeywordText(_keywordText))
        {
            return false;
        }

        return SlotMachineSystem.Instance != null &&
               SlotMachineSystem.Instance.WasAnyCurrentSkillKeywordRerolledThisTurn(currentSkill);
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
            case DealDamageGA damageGA when !damageGA.IsArtifactGenerated && damageGA.Caster is PlayerView damageCaster:
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

[Serializable]
public class OnGainShieldCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not AddShieldGA addShieldGA || addShieldGA.Targets == null)
        {
            return false;
        }

        if (_owner == null)
        {
            return addShieldGA.Targets.Any(target => target is PlayerView);
        }

        return addShieldGA.Targets.Any(target => target == _owner || (target is PlayerView && _owner is PlayerView));
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<AddShieldGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<AddShieldGA>(reaction, _reactionTiming);
    }
}

[Serializable]
public class StatusStackCondition : Condition
{
    [SerializeField] private EStatusType _statusType = EStatusType.Poison;
    [SerializeField] private int _threshold = 1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not AddStatusGA addStatusGA || addStatusGA.Targets == null)
        {
            return false;
        }

        if (addStatusGA.Status == null || addStatusGA.Status.StatusType != _statusType)
        {
            return false;
        }

        return addStatusGA.Targets.Any(target =>
            target != null &&
            target.Character != null &&
            target.Character.GetStatus(_statusType) >= _threshold);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }
}

[Serializable]
public class KeywordUseCountCondition : Condition
{
    [SerializeField] private string _keywordText = string.Empty;
    [SerializeField] private int _count = 2;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (BattleSystem.Instance == null || BattleSystem.Instance.CurrentExecutingBattleAct?.Skill == null)
        {
            return false;
        }

        Skill currentSkill = BattleSystem.Instance.CurrentExecutingBattleAct.Skill;
        if (!currentSkill.UsesKeywordText(_keywordText))
        {
            return false;
        }

        int matchedCount = 0;
        foreach (BattleAct battleAct in BattleSystem.Instance.CurrentConfirmedBattleActs)
        {
            if (battleAct?.IsPlayer != true || battleAct.Skill == null)
            {
                continue;
            }

            if (battleAct.Skill.UsesKeywordText(_keywordText))
            {
                matchedCount++;
            }

            if (ReferenceEquals(battleAct, BattleSystem.Instance.CurrentExecutingBattleAct))
            {
                break;
            }
        }

        return matchedCount >= Mathf.Max(1, _count);
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}

[Serializable]
public class EnemyDamageOnTurnIntervalCondition : Condition
{
    [SerializeField] private int _interval = 5;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is DealDamageGA damageGA &&
               damageGA.Caster != null &&
               damageGA.Caster.Character.BattleSideType == EBattleSideType.EnemySide &&
               BattleSystem.Instance != null &&
               _interval > 0 &&
               BattleSystem.Instance.CurrentTurn > 0 &&
               BattleSystem.Instance.CurrentTurn % _interval == 0;
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

[Serializable]
public class FirstJobAttackInBattleCondition : Condition
{
    [SerializeField] private EPlayerJob _job = EPlayerJob.Warrior;

    private bool _triggered;
    private Action<StartBattleGA> _resetHandler;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (_triggered ||
            gameAction is not DealDamageGA damageGA ||
            damageGA.IsArtifactGenerated ||
            damageGA.Caster is not PlayerView playerView ||
            playerView.Player.PlayerData.PlayerJob != _job)
        {
            return false;
        }

        _triggered = true;
        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _resetHandler = _ => _triggered = false;
        ActionSystem.SubscribeReaction<StartBattleGA>(_resetHandler, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        if (_resetHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartBattleGA>(_resetHandler, EReactionTiming.Pre);
            _resetHandler = null;
        }

        ActionSystem.UnSubscribeReaction<DealDamageGA>(reaction, _reactionTiming);
    }
}

[Serializable]
public class ManaSpentRerollCondition : Condition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is SpinSlotMachineGA && ArtifactRuntimeState.ConsumeManaSpentRerollFlag();
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

[Serializable]
public class AnyRerollCondition : Condition
{
    private Action<RerollSlotMachineKeywordGA> _keywordHandler;
    private Action<RerollSlotMachineKeywordAddTokenGA> _addTokenHandler;
    private Action<RerollSlotMachineKeywordAddTokenInBattlePhaseGA> _battlePhaseHandler;
    private Action<RerollSlotMachineLineGA> _lineHandler;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is SpinSlotMachineGA)
        {
            return SlotMachineSystem.Instance != null && SlotMachineSystem.Instance.CurrentTurnRerollCount > 0;
        }

        return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _keywordHandler = ga => reaction?.Invoke(ga);
        _addTokenHandler = ga => reaction?.Invoke(ga);
        _battlePhaseHandler = ga => reaction?.Invoke(ga);
        _lineHandler = ga => reaction?.Invoke(ga);

        ActionSystem.SubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordGA>(_keywordHandler, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(_addTokenHandler, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(_battlePhaseHandler, _reactionTiming);
        ActionSystem.SubscribeReaction<RerollSlotMachineLineGA>(_lineHandler, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<SpinSlotMachineGA>(reaction, _reactionTiming);

        if (_keywordHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordGA>(_keywordHandler, _reactionTiming);
            _keywordHandler = null;
        }

        if (_addTokenHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenGA>(_addTokenHandler, _reactionTiming);
            _addTokenHandler = null;
        }

        if (_battlePhaseHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineKeywordAddTokenInBattlePhaseGA>(_battlePhaseHandler, _reactionTiming);
            _battlePhaseHandler = null;
        }

        if (_lineHandler != null)
        {
            ActionSystem.UnSubscribeReaction<RerollSlotMachineLineGA>(_lineHandler, _reactionTiming);
            _lineHandler = null;
        }
    }
}

[Serializable]
public class BattleRerollCountCondition : Condition
{
    [SerializeField] private int _count = 0;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return SlotMachineSystem.Instance != null && SlotMachineSystem.Instance.BattleRerollCount == _count;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
    }
}

[Serializable]
public class NoEnemyDamageThisTurnCondition : Condition
{
    private bool _enemyDamaged;
    private Action<DealDamageGA> _damageTracker;
    private Action<StartTurnGA> _resetHandler;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is EndTurnGA && !_enemyDamaged;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        _damageTracker = TrackDamage;
        _resetHandler = _ => _enemyDamaged = false;

        ActionSystem.SubscribeReaction<DealDamageGA>(_damageTracker, EReactionTiming.Post);
        ActionSystem.SubscribeReaction<StartTurnGA>(_resetHandler, EReactionTiming.Pre);
        ActionSystem.SubscribeReaction<EndTurnGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        if (_damageTracker != null)
        {
            ActionSystem.UnSubscribeReaction<DealDamageGA>(_damageTracker, EReactionTiming.Post);
            _damageTracker = null;
        }

        if (_resetHandler != null)
        {
            ActionSystem.UnSubscribeReaction<StartTurnGA>(_resetHandler, EReactionTiming.Pre);
            _resetHandler = null;
        }

        ActionSystem.UnSubscribeReaction<EndTurnGA>(reaction, _reactionTiming);
    }

    private void TrackDamage(DealDamageGA damageGA)
    {
        if (damageGA?.Caster is not PlayerView || damageGA.Targets == null)
        {
            return;
        }

        foreach (CharacterView target in damageGA.Targets)
        {
            if (target != null && target.Character.BattleSideType == EBattleSideType.EnemySide)
            {
                _enemyDamaged = true;
                return;
            }
        }
    }
}

[Serializable]
public class OnApplyStatusByJobCondition : Condition
{
    [SerializeField] private EPlayerJob _job = EPlayerJob.Archer;
    [SerializeField] private EStatusType _statusType = EStatusType.Marking;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is AddStatusGA addStatusGA &&
               addStatusGA.Status != null &&
               addStatusGA.Status.StatusType == _statusType &&
               addStatusGA.Caster is PlayerView playerView &&
               playerView.Player.PlayerData.PlayerJob == _job;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<AddStatusGA>(reaction, _reactionTiming);
    }
}
