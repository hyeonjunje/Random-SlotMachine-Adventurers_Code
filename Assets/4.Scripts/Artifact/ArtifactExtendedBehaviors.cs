using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class ArtifactBehavior_ModifyDamagePercent : ArtifactBehavior
{
    [SerializeField] private float _percent;

    public override void ModifyAction(GameAction action)
    {
        if (action is DealDamageGA damageGA)
        {
            ArtifactActionMath.MultiplyDamageFormula(damageGA.DamageFormula, 1f + (_percent / 100f));
        }
    }
}

[Serializable]
public class ArtifactBehavior_IgnoreShield : ArtifactBehavior
{
    [SerializeField] private bool _ignoreShield = true;

    public override void ModifyAction(GameAction action)
    {
        if (_ignoreShield && action is DealDamageGA damageGA && damageGA.DamageFormula != null)
        {
            damageGA.DamageFormula.IsIgnoresDefense = true;
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyGoldRewardPercent : ArtifactBehavior
{
    [SerializeField] private float _percent;

    public override void ModifyAction(GameAction action)
    {
        if (action is GrantPostBattleGoldGA goldGA)
        {
            goldGA.reward = Mathf.RoundToInt(goldGA.reward * (1f + (_percent / 100f)));
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyEffectValue : ArtifactBehavior
{
    [SerializeField] private float _multiplier = 1f;

    public override void ModifyAction(GameAction action)
    {
        switch (action)
        {
            case DealDamageGA damageGA:
                ArtifactActionMath.MultiplyDamageFormula(damageGA.DamageFormula, _multiplier);
                break;
            case ApplyHealingGA healingGA:
                ArtifactActionMath.MultiplyHealingFormula(healingGA.HealingFormula, _multiplier);
                break;
            case AddShieldGA shieldGA:
                ArtifactActionMath.MultiplyShieldFormula(shieldGA.ShieldFormula, _multiplier);
                break;
            case ChangeEnemyActCountGA actCountGA:
                actCountGA.MultiplyActCountDiff(_multiplier);
                break;
            case ChangeStatValueGA statGA:
                statGA.MultiplyValue(_multiplier);
                break;
            case AddStatusGA addStatusGA:
                addStatusGA.MultiplyTurn(_multiplier);
                break;
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifySlotTierWeight : ArtifactBehavior
{
    [SerializeField] private float _multiplier = 1f;

    public override void ModifyAction(GameAction action)
    {
        if (action is SpinSlotMachineGA spinSlotMachineGA)
        {
            spinSlotMachineGA.SetHigherTierWeightMultiplier(_multiplier);
        }
    }
}

[Serializable]
public class ArtifactBehavior_ChanceWrapper : ArtifactBehavior
{
    [SerializeField, Range(0f, 100f)] private float _chancePercent = 0f;
    [SerializeReference] private List<ArtifactBehavior> _behaviors = new List<ArtifactBehavior>();

    [NonSerialized] private bool _passed;

    public override void ModifyAction(GameAction action)
    {
        _passed = UnityEngine.Random.Range(0f, 100f) < _chancePercent;
        if (!_passed)
        {
            return;
        }

        foreach (ArtifactBehavior behavior in _behaviors)
        {
            behavior.ModifyAction(action);
        }
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (!_passed)
        {
            return;
        }

        foreach (ArtifactBehavior behavior in _behaviors)
        {
            behavior.Execute(owner, caster, targets);
        }

        _passed = false;
    }
}

[Serializable]
public class ArtifactBehavior_RerollAllSlots : ArtifactBehavior
{
    [SerializeField] private int _rerollCount = 1;
    [SerializeField] private float _higherTierWeightMultiplier = 1f;

    public override void ModifyAction(GameAction action)
    {
        if (action is not StartAutoBattleGA startAutoBattleGA || SlotMachineSystem.Instance == null)
        {
            return;
        }

        List<BattleAct> nonPlayerActs = startAutoBattleGA.BattleActs.Where(act => act != null && !act.IsPlayer).ToList();
        List<BattleAct> playerActs = SlotMachineSystem.Instance.RerollAllSlotsAndBuildPlayerBattleActs(_rerollCount, _higherTierWeightMultiplier);

        startAutoBattleGA.BattleActs.Clear();
        startAutoBattleGA.BattleActs.AddRange(nonPlayerActs);
        startAutoBattleGA.BattleActs.AddRange(playerActs);
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (SlotMachineSystem.Instance == null ||
            BattleSystem.Instance == null ||
            BattleSystem.Instance.BattleState != EBattleState.InAutoBattle)
        {
            return;
        }

        List<BattleAct> playerActs = SlotMachineSystem.Instance.RerollAllSlotsAndBuildPlayerBattleActs(_rerollCount, _higherTierWeightMultiplier);
        if (playerActs == null || playerActs.Count == 0)
        {
            return;
        }

        BattleSystem.Instance.ReplaceRemainingPlayerActs(playerActs);
    }
}

[Serializable]
public class ArtifactBehavior_AdditionalAttack : ArtifactBehavior
{
    [SerializeField] private int _targetCount = 1;
    [SerializeField] private int _repeatCount = 1;

    [NonSerialized] private DealDamageGA _capturedDamageGA;

    public override void ModifyAction(GameAction action)
    {
        _capturedDamageGA = action as DealDamageGA;
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (_capturedDamageGA == null || _capturedDamageGA.IsArtifactGenerated || caster == null)
        {
            _capturedDamageGA = null;
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        for (int i = 0; i < Mathf.Max(1, _repeatCount); i++)
        {
            List<CharacterView> selectedTargets = CharacterSystem.Instance.Enemies
                .Where(enemy => enemy != null && !enemy.Character.IsDead)
                .OrderBy(_ => Guid.NewGuid())
                .Take(Mathf.Max(1, _targetCount))
                .Cast<CharacterView>()
                .ToList();

            if (selectedTargets.Count == 0)
            {
                break;
            }

            DealDamageGA extraDamageGA = new DealDamageGA(caster, selectedTargets, ArtifactActionMath.CloneDamageFormula(_capturedDamageGA.DamageFormula));
            extraDamageGA.MarkArtifactGenerated();
            triggerGA.AddEffect(extraDamageGA);
        }

        ActionSystem.Instance.AddReaction(triggerGA);
        _capturedDamageGA = null;
    }
}

[Serializable]
public class ArtifactBehavior_SetEnemyActCount : ArtifactBehavior
{
    [SerializeField] private int _targetActCount = 0;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        foreach (CharacterView target in targets)
        {
            if (target is not EnemyView enemyView)
            {
                continue;
            }

            int diff = _targetActCount - enemyView.Enemy.EnemyAI.ActCount;
            if (diff != 0)
            {
                triggerGA.AddEffect(new ChangeEnemyActCountGA(diff, new List<CharacterView> { enemyView }));
            }
        }

        ActionSystem.Instance.AddReaction(triggerGA);
    }
}

[Serializable]
public class ArtifactBehavior_ManaToHeal : ArtifactBehavior
{
    [SerializeField] private float _ratio = 1f;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (caster == null || ManaSystem.Instance == null)
        {
            return;
        }

        int manaToSpend = Mathf.FloorToInt(ManaSystem.Instance.CurrentMana);
        if (manaToSpend <= 0)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
        triggerGA.AddEffect(new SpendManaGA(manaToSpend));
        triggerGA.AddEffect(new ApplyHealingGA(
            caster,
            new List<CharacterView> { caster },
            new HealingFormula(EHealingFormulaType.Flat, manaToSpend * _ratio)));

        ActionSystem.Instance.AddReaction(triggerGA);
    }
}

[Serializable]
public class ArtifactBehavior_PoisonSpreadWatcher : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _spreadRatio = 0.5f;
    [SerializeField] private int _targetCount = 1;

    [NonSerialized] private readonly HashSet<EnemyView> _watchedTargets = new HashSet<EnemyView>();
    [NonSerialized] private Action<EnemyDeadGA> _handler;
    [NonSerialized] private Artifact _ownerArtifact;

    public void OnRegister(Artifact owner)
    {
        _ownerArtifact = owner;
        _handler = OnEnemyDead;
        ActionSystem.SubscribeReaction<EnemyDeadGA>(_handler, EReactionTiming.Pre);
    }

    public void OnUnregister(Artifact owner)
    {
        if (_handler != null)
        {
            ActionSystem.UnSubscribeReaction<EnemyDeadGA>(_handler, EReactionTiming.Pre);
            _handler = null;
        }

        _watchedTargets.Clear();
        _ownerArtifact = null;
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (targets == null)
        {
            return;
        }

        foreach (CharacterView target in targets)
        {
            if (target is EnemyView enemyView)
            {
                _watchedTargets.Add(enemyView);
            }
        }
    }

    private void OnEnemyDead(EnemyDeadGA enemyDeadGA)
    {
        if (enemyDeadGA?.Killed == null || !_watchedTargets.Remove(enemyDeadGA.Killed))
        {
            return;
        }

        int poisonStack = enemyDeadGA.Killed.Character.GetStatus(EStatusType.Poison);
        int spreadAmount = Mathf.FloorToInt(poisonStack * _spreadRatio);
        if (spreadAmount <= 0)
        {
            return;
        }

        List<CharacterView> spreadTargets = CharacterSystem.Instance.Enemies
            .Where(enemy => enemy != null && enemy != enemyDeadGA.Killed && !enemy.Character.IsDead)
            .OrderBy(_ => Guid.NewGuid())
            .Take(Mathf.Max(1, _targetCount))
            .Cast<CharacterView>()
            .ToList();

        if (spreadTargets.Count == 0)
        {
            return;
        }

        SO_StatusData poisonStatus = DataManager.Instance.GetStatus(EStatusType.Poison);
        if (poisonStatus == null)
        {
            return;
        }

        TriggerArtifactGA triggerGA = new TriggerArtifactGA(_ownerArtifact);
        triggerGA.AddEffect(new AddStatusGA(poisonStatus, spreadAmount, spreadTargets, enemyDeadGA.Killer));
        ActionSystem.Instance.AddReaction(triggerGA);
    }
}

[Serializable]
public class ArtifactBehavior_ModifyDamageByGoldThreshold : ArtifactBehavior
{
    [SerializeField] private int _goldThreshold = 100;
    [SerializeField] private float _percentPerThreshold = 0f;

    public override void ModifyAction(GameAction action)
    {
        if (action is not DealDamageGA damageGA || UIHudSystem.Instance == null || _goldThreshold <= 0)
        {
            return;
        }

        int stack = UIHudSystem.Instance.CurrentGold / _goldThreshold;
        if (stack <= 0)
        {
            return;
        }

        float multiplier = 1f + ((_percentPerThreshold / 100f) * stack);
        ArtifactActionMath.MultiplyDamageFormula(damageGA.DamageFormula, multiplier);
    }
}

[Serializable]
public class ArtifactBehavior_AttackPowerByGoldThreshold : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _goldThreshold = 100;
    [SerializeField] private float _valuePerThreshold = 1f;
    [SerializeField] private EStatType _statType = EStatType.AttackPower;
    [SerializeField] private EStatModType _statModType = EStatModType.Add;

    [NonSerialized] private int _currentAppliedStack = 0;
    [NonSerialized] private Action<ApplyGoldDeltaGA> _goldHandler;
    [NonSerialized] private Action<SpawnPlayerGA> _spawnHandler;

    public void OnRegister(Artifact owner)
    {
        _goldHandler = _ => RefreshBonus();
        _spawnHandler = _ => ApplyCurrentBonusToNewestPlayer();
        ActionSystem.SubscribeReaction<ApplyGoldDeltaGA>(_goldHandler, EReactionTiming.Post);
        ActionSystem.SubscribeReaction<SpawnPlayerGA>(_spawnHandler, EReactionTiming.Post);
        RefreshBonus();
    }

    public void OnUnregister(Artifact owner)
    {
        RemoveAppliedBonus();

        if (_goldHandler != null)
        {
            ActionSystem.UnSubscribeReaction<ApplyGoldDeltaGA>(_goldHandler, EReactionTiming.Post);
            _goldHandler = null;
        }

        if (_spawnHandler != null)
        {
            ActionSystem.UnSubscribeReaction<SpawnPlayerGA>(_spawnHandler, EReactionTiming.Post);
            _spawnHandler = null;
        }
    }

    private void RefreshBonus()
    {
        if (UIHudSystem.Instance == null || CharacterSystem.Instance == null || _goldThreshold <= 0)
        {
            return;
        }

        int desiredStack = UIHudSystem.Instance.CurrentGold / _goldThreshold;
        int deltaStack = desiredStack - _currentAppliedStack;
        if (deltaStack == 0)
        {
            return;
        }

        ApplyDeltaToAllPlayers(deltaStack * _valuePerThreshold);
        _currentAppliedStack = desiredStack;
    }

    private void RemoveAppliedBonus()
    {
        if (_currentAppliedStack == 0)
        {
            return;
        }

        ApplyDeltaToAllPlayers(-_currentAppliedStack * _valuePerThreshold);
        _currentAppliedStack = 0;
    }

    private void ApplyCurrentBonusToNewestPlayer()
    {
        if (_currentAppliedStack == 0 || CharacterSystem.Instance == null || CharacterSystem.Instance.Players.Count == 0)
        {
            return;
        }

        PlayerView latestPlayer = CharacterSystem.Instance.Players.LastOrDefault();
        if (latestPlayer == null)
        {
            return;
        }

        latestPlayer.Character.GetStat(_statType)?.AddModifier(_statModType, _currentAppliedStack * _valuePerThreshold);
        latestPlayer.Character.OnDataChanged?.Invoke();
    }

    private void ApplyDeltaToAllPlayers(float delta)
    {
        if (CharacterSystem.Instance == null)
        {
            return;
        }

        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            playerView.Character.GetStat(_statType)?.AddModifier(_statModType, delta);
            playerView.Character.OnDataChanged?.Invoke();
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyDamageTaken : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _flatDelta = 0;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.PlayerDamageTakenFlatModifier += _flatDelta;
    }

    public void OnUnregister(Artifact owner)
    {
        ArtifactRuntimeState.PlayerDamageTakenFlatModifier -= _flatDelta;
    }
}

[Serializable]
public class ArtifactBehavior_ModifyEliteMaxHpPercent : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _percent = 0f;

    [NonSerialized] private readonly HashSet<EnemyView> _appliedEnemies = new HashSet<EnemyView>();
    [NonSerialized] private Action<SpawnEnemyGA> _spawnHandler;

    public void OnRegister(Artifact owner)
    {
        _spawnHandler = _ => ApplyToLatestEnemy();
        ActionSystem.SubscribeReaction<SpawnEnemyGA>(_spawnHandler, EReactionTiming.Post);

        if (BattleSystem.Instance != null &&
            CharacterSystem.Instance != null &&
            BattleSystem.Instance.CurrentBattleType == EMapNodeType.Elite)
        {
            foreach (EnemyView enemyView in CharacterSystem.Instance.Enemies)
            {
                ApplyToEnemy(enemyView, _percent / 100f);
                _appliedEnemies.Add(enemyView);
            }
        }
    }

    public void OnUnregister(Artifact owner)
    {
        if (_spawnHandler != null)
        {
            ActionSystem.UnSubscribeReaction<SpawnEnemyGA>(_spawnHandler, EReactionTiming.Post);
            _spawnHandler = null;
        }

        foreach (EnemyView enemyView in _appliedEnemies.ToList())
        {
            ApplyToEnemy(enemyView, -(_percent / 100f));
        }

        _appliedEnemies.Clear();
    }

    private void ApplyToLatestEnemy()
    {
        if (BattleSystem.Instance == null ||
            CharacterSystem.Instance == null ||
            BattleSystem.Instance.CurrentBattleType != EMapNodeType.Elite)
        {
            return;
        }

        EnemyView latestEnemy = CharacterSystem.Instance.Enemies.LastOrDefault();
        ApplyToEnemy(latestEnemy, _percent / 100f);
        if (latestEnemy != null)
        {
            _appliedEnemies.Add(latestEnemy);
        }
    }

    private void ApplyToEnemy(EnemyView enemyView, float multiplierDelta)
    {
        if (enemyView == null)
        {
            return;
        }

        Stat stat = enemyView.Character.GetStat(EStatType.MaxHp);
        if (stat == null)
        {
            return;
        }

        float previousValue = stat.Value;
        stat.AddModifier(EStatModType.Mul, multiplierDelta);
        float currentValue = stat.Value;
        enemyView.Character.HealthController.ChangeMaxHp(Mathf.RoundToInt(currentValue - previousValue));
    }
}

[Serializable]
public class ArtifactBehavior_ModifyDamageTakenPercent : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _percent = 0f;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.PlayerDamageTakenMultiplier *= 1f + (_percent / 100f);
    }

    public void OnUnregister(Artifact owner)
    {
        float multiplier = 1f + (_percent / 100f);
        if (!Mathf.Approximately(multiplier, 0f))
        {
            ArtifactRuntimeState.PlayerDamageTakenMultiplier /= multiplier;
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyElectricValueMultiplier : ArtifactBehavior
{
    [SerializeField] private float _multiplier = 1.5f;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (DataManager.Instance?.GameModel == null || Mathf.Approximately(_multiplier, 0f))
        {
            return;
        }

        DataManager.Instance.GameModel.EletricValue *= _multiplier;
    }

    public override void OnRemove(CharacterView target)
    {
        if (DataManager.Instance?.GameModel == null || Mathf.Approximately(_multiplier, 0f))
        {
            return;
        }

        DataManager.Instance.GameModel.EletricValue /= _multiplier;
    }
}

[Serializable]
public class ArtifactBehavior_HealPartyPercentOfMaxHp : ArtifactBehavior
{
    [SerializeField] private float _ratio = 0.1f;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        HealthController partyHealth = CharacterSystem.Instance?.PartyHealth;
        if (partyHealth == null || _ratio <= 0f)
        {
            return;
        }

        int amount = Mathf.Max(1, Mathf.RoundToInt(partyHealth.MaxHp * _ratio));
        partyHealth.RestoreHealth(amount);
    }
}

[Serializable]
public class ArtifactBehavior_DestroyOwnerArtifact : ArtifactBehavior
{
    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (owner != null && ArtifactSystem.Instance != null)
        {
            ArtifactSystem.Instance.RemoveArtifact(owner);
        }
    }
}

[Serializable]
public class ArtifactBehavior_BlockStatus : ArtifactBehavior
{
    [SerializeField] private int _count = 1;

    public override void ModifyAction(GameAction action)
    {
        if (_count > 0 && action is AddStatusGA addStatusGA)
        {
            addStatusGA.Block();
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifyShopPrice : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _percent = 0f;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.ShopPriceMultiplier *= 1f + (_percent / 100f);
    }

    public void OnUnregister(Artifact owner)
    {
        float multiplier = 1f + (_percent / 100f);
        if (!Mathf.Approximately(multiplier, 0f))
        {
            ArtifactRuntimeState.ShopPriceMultiplier /= multiplier;
        }
    }
}

[Serializable]
public class ArtifactBehavior_DoublePlayerTokensThisTurn : ArtifactBehavior
{
    [SerializeField] private int _multiplier = 2;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        ArtifactRuntimeState.MultiplyCurrentTurnPlayerTokens(_multiplier);
    }
}

[Serializable]
public class ArtifactBehavior_SkipNextTurn : ArtifactBehavior
{
    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        ArtifactRuntimeState.ScheduleSkipNextPlayerTurn();
    }
}

[Serializable]
public class ArtifactBehavior_ShieldByAttackTokenCount : ArtifactBehavior
{
    [SerializeField] private float _shieldPerAttackToken = 1f;

    [NonSerialized] private int _capturedAttackTokenCount;

    public override void ModifyAction(GameAction action)
    {
        _capturedAttackTokenCount = 0;
        if (action is not StartAutoBattleGA startAutoBattleGA || startAutoBattleGA.BattleActs == null)
        {
            return;
        }

        _capturedAttackTokenCount = startAutoBattleGA.BattleActs.Count(IsAttackToken);
    }

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        if (_capturedAttackTokenCount <= 0 || CharacterSystem.Instance == null)
        {
            return;
        }

        int shield = Mathf.RoundToInt(_capturedAttackTokenCount * _shieldPerAttackToken);
        if (shield <= 0)
        {
            return;
        }

        var playerTargets = CharacterSystem.Instance.Players
            .Where(player => player != null && !player.Character.IsDead)
            .Cast<CharacterView>()
            .ToList();

        if (playerTargets.Count > 0)
        {
            TriggerArtifactGA triggerGA = new TriggerArtifactGA(owner);
            triggerGA.AddEffect(new AddShieldGA(
                caster, 
                playerTargets, 
                new ShieldFormula(EShieldFormulaType.Flat, shield)));
            ActionSystem.Instance.AddReaction(triggerGA);
        }

        _capturedAttackTokenCount = 0;
    }

    private static bool IsAttackToken(BattleAct battleAct)
    {
        return battleAct != null &&
               battleAct.IsPlayer &&
               battleAct.Skill != null &&
               battleAct.Skill.TotalEffect.Any(effect => effect is DealDamageEffect);
    }
}

[Serializable]
public class ArtifactBehavior_ModifyMapWeight : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [Serializable]
    public struct MapWeightModifierData
    {
        public EMapNodeType NodeType;
        public float Delta;
    }

    [SerializeField] private List<MapWeightModifierData> _modifiers = new List<MapWeightModifierData>();

    public void OnRegister(Artifact owner)
    {
        Apply(1f);
    }

    public void OnUnregister(Artifact owner)
    {
        Apply(-1f);
    }

    private void Apply(float sign)
    {
        foreach (MapWeightModifierData modifier in _modifiers)
        {
            ArtifactRuntimeState.AddMapNodeWeightDelta(modifier.NodeType, modifier.Delta * sign);
        }
    }
}

[Serializable]
public class ArtifactBehavior_SetFirstTurnFreeRerolls : ArtifactBehavior
{
    [SerializeField] private int _count = 1;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        ArtifactRuntimeState.FirstTurnTemporaryFreeRerolls += Mathf.Max(0, _count);
    }
}

[Serializable]
public class ArtifactBehavior_RerollRandomSlots : ArtifactBehavior
{
    [SerializeField] private int _slotCount = 1;

    public override void ModifyAction(GameAction action)
    {
        if (action is not StartAutoBattleGA startAutoBattleGA || SlotMachineSystem.Instance == null)
        {
            return;
        }

        List<BattleAct> nonPlayerActs = startAutoBattleGA.BattleActs
            .Where(act => act != null && !act.IsPlayer)
            .ToList();
        List<BattleAct> playerActs = SlotMachineSystem.Instance.RerollRandomSlotsAndBuildPlayerBattleActs(_slotCount);

        startAutoBattleGA.BattleActs.Clear();
        startAutoBattleGA.BattleActs.AddRange(nonPlayerActs);
        startAutoBattleGA.BattleActs.AddRange(playerActs);
    }
}

[Serializable]
public class ArtifactBehavior_SetTargetHp : ArtifactBehavior
{
    [SerializeField] private int _targetHp = 1;
    [SerializeReference] private TargetSelector _targetSelector;

    public override void Execute(Artifact owner, CharacterView caster, List<CharacterView> targets)
    {
        List<CharacterView> resolvedTargets = targets;
        if ((resolvedTargets == null || resolvedTargets.Count == 0) && _targetSelector != null)
        {
            resolvedTargets = _targetSelector.SelectTarget(caster);
        }

        if (resolvedTargets == null)
        {
            return;
        }

        foreach (CharacterView target in resolvedTargets)
        {
            if (target?.Character?.HealthController == null || target.Character.IsDead)
            {
                continue;
            }

            target.Character.HealthController.SetCurrentHp(_targetHp);
        }
    }
}

[Serializable]
public class ArtifactBehavior_ModifySlotClickRerollManaCost : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _delta = 1;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.SlotClickRerollManaCostDelta += _delta;
    }

    public void OnUnregister(Artifact owner)
    {
        ArtifactRuntimeState.SlotClickRerollManaCostDelta -= _delta;
    }
}

[Serializable]
public class ArtifactBehavior_ReintroduceHighestRankKeywordOnClickReroll : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField, Range(0f, 100f)] private float _chancePercent = 50f;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.ClickRerollReintroduceChancePercent =
            Mathf.Max(ArtifactRuntimeState.ClickRerollReintroduceChancePercent, _chancePercent);
    }

    public void OnUnregister(Artifact owner)
    {
        if (Mathf.Approximately(ArtifactRuntimeState.ClickRerollReintroduceChancePercent, _chancePercent))
        {
            ArtifactRuntimeState.ClickRerollReintroduceChancePercent = 0f;
        }
    }
}

[Serializable]
public class ArtifactBehavior_LevelUpRandomPlayerOnPermanentKeyword : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField, Range(0f, 100f)] private float _chancePercent = 50f;
    [SerializeField] private int _levelDiff = 1;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.GrowthPotionChancePercent =
            Mathf.Max(ArtifactRuntimeState.GrowthPotionChancePercent, _chancePercent);
        ArtifactRuntimeState.GrowthPotionLevelDiff =
            Mathf.Max(ArtifactRuntimeState.GrowthPotionLevelDiff, _levelDiff);
    }

    public void OnUnregister(Artifact owner)
    {
        if (Mathf.Approximately(ArtifactRuntimeState.GrowthPotionChancePercent, _chancePercent))
        {
            ArtifactRuntimeState.GrowthPotionChancePercent = 0f;
        }

        if (ArtifactRuntimeState.GrowthPotionLevelDiff == _levelDiff)
        {
            ArtifactRuntimeState.GrowthPotionLevelDiff = 0;
        }
    }
}

[Serializable]
public class ArtifactBehavior_MarkEnemiesOnTurnEndIfUniqueSlot : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _markStacks = 1;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.UniqueSlotEndTurnMarkStacks =
            Mathf.Max(ArtifactRuntimeState.UniqueSlotEndTurnMarkStacks, _markStacks);
    }

    public void OnUnregister(Artifact owner)
    {
        if (ArtifactRuntimeState.UniqueSlotEndTurnMarkStacks == _markStacks)
        {
            ArtifactRuntimeState.UniqueSlotEndTurnMarkStacks = 0;
        }
    }
}

[Serializable]
public class ArtifactBehavior_RevivePartyOnce : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _reviveRatio = 0.3f;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.ArmPartyRevive(_reviveRatio);
    }

    public void OnUnregister(Artifact owner)
    {
        ArtifactRuntimeState.DisarmPartyRevive(_reviveRatio);
    }
}

[Serializable]
public class ArtifactBehavior_UpgradeAllSlotsToHighestTierOnNthReroll : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _interval = 10;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.UpgradeAllSlotsOnNthRerollInterval =
            ArtifactRuntimeState.UpgradeAllSlotsOnNthRerollInterval <= 0
                ? _interval
                : Mathf.Min(ArtifactRuntimeState.UpgradeAllSlotsOnNthRerollInterval, _interval);
    }

    public void OnUnregister(Artifact owner)
    {
        if (ArtifactRuntimeState.UpgradeAllSlotsOnNthRerollInterval == _interval)
        {
            ArtifactRuntimeState.UpgradeAllSlotsOnNthRerollInterval = 0;
        }
    }
}

[Serializable]
public class ArtifactBehavior_NullifyWeakenedEnemyDamageChance : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField, Range(0f, 100f)] private float _chancePercent = 50f;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.NullifyWeakenedEnemyDamageChancePercent =
            Mathf.Max(ArtifactRuntimeState.NullifyWeakenedEnemyDamageChancePercent, _chancePercent);
    }

    public void OnUnregister(Artifact owner)
    {
        if (Mathf.Approximately(ArtifactRuntimeState.NullifyWeakenedEnemyDamageChancePercent, _chancePercent))
        {
            ArtifactRuntimeState.NullifyWeakenedEnemyDamageChancePercent = 0f;
        }
    }
}

[Serializable]
public class ArtifactBehavior_DealPartyAttackDamageOnReroll : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private float _ratio = 1f;
    [SerializeField] private int _targetCount = 1;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.DamageOnRerollPartyAttackRatio =
            Mathf.Max(ArtifactRuntimeState.DamageOnRerollPartyAttackRatio, _ratio);
        ArtifactRuntimeState.DamageOnRerollTargetCount =
            Mathf.Max(ArtifactRuntimeState.DamageOnRerollTargetCount, _targetCount);
    }

    public void OnUnregister(Artifact owner)
    {
        if (Mathf.Approximately(ArtifactRuntimeState.DamageOnRerollPartyAttackRatio, _ratio))
        {
            ArtifactRuntimeState.DamageOnRerollPartyAttackRatio = 0f;
        }

        if (ArtifactRuntimeState.DamageOnRerollTargetCount == _targetCount)
        {
            ArtifactRuntimeState.DamageOnRerollTargetCount = 1;
        }
    }
}

[Serializable]
public class ArtifactBehavior_DisableRerollAndDoublePlayerTokens : ArtifactBehavior, IArtifactBehaviorLifecycle
{
    [SerializeField] private int _tokenMultiplier = 2;

    public void OnRegister(Artifact owner)
    {
        ArtifactRuntimeState.DisableRerollCount++;
        ArtifactRuntimeState.PlayerTokenMultiplier *= Mathf.Max(1, _tokenMultiplier);
    }

    public void OnUnregister(Artifact owner)
    {
        ArtifactRuntimeState.DisableRerollCount = Mathf.Max(0, ArtifactRuntimeState.DisableRerollCount - 1);

        int divisor = Mathf.Max(1, _tokenMultiplier);
        ArtifactRuntimeState.PlayerTokenMultiplier = Mathf.Max(1, ArtifactRuntimeState.PlayerTokenMultiplier / divisor);
    }
}
