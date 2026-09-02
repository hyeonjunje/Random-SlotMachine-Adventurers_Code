using SerializeReferenceEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class ArtifactTrigger
{
    [SerializeReference, SR] public List<ArtifactBehavior> Behaviors;
    public abstract void Register(Artifact owner);
    public abstract void Unregister(Artifact owner);

    protected void RegisterBehaviorLifecycle(Artifact owner)
    {
        if (Behaviors == null)
        {
            return;
        }

        foreach (ArtifactBehavior behavior in Behaviors)
        {
            if (behavior is IArtifactBehaviorLifecycle lifecycle)
            {
                lifecycle.OnRegister(owner);
            }
        }
    }

    protected void UnregisterBehaviorLifecycle(Artifact owner)
    {
        if (Behaviors == null)
        {
            return;
        }

        foreach (ArtifactBehavior behavior in Behaviors)
        {
            if (behavior is IArtifactBehaviorLifecycle lifecycle)
            {
                lifecycle.OnUnregister(owner);
            }
        }
    }
}

// 얻자마자 발동하는 유물
[Serializable]
public class ArtifactTrigger_OnEquip : ArtifactTrigger
{
    public override void Register(Artifact owner)
    {
        RegisterBehaviorLifecycle(owner);
        CharacterView caster = ArtifactExecutionContext.GetDefaultCaster (owner);
        foreach (var behavior in Behaviors)
        {
            behavior.Execute (owner, caster, null);
        }
    }

    public override void Unregister(Artifact owner)
    {
        foreach (var behavior in Behaviors)
        {
            behavior.OnRemove (null);
        }

        UnregisterBehaviorLifecycle(owner);
    }
}

// 턴 시작시 발동하는 유물
[Serializable]
public class ArtifactTrigger_OnStartTurn : ArtifactTrigger
{
    [SerializeField] private int _interval = 3;
    private Action<StartTurnGA> _handler;

    public override void Register(Artifact owner)
    {
        RegisterBehaviorLifecycle(owner);
        _handler = (startTurnGA) =>
        {
            int turn = BattleSystem.Instance.CurrentTurn;
            if (turn > 0 && turn % _interval == 0)
            {
                CharacterView caster = ArtifactExecutionContext.GetDefaultCaster (owner);
                foreach (var behavior in Behaviors)
                {
                    behavior.Execute (owner, caster, null);
                }
            }
        };
        ActionSystem.SubscribeReaction<StartTurnGA> (_handler, EReactionTiming.Post);
    }

    public override void Unregister(Artifact owner)
    {
        if (_handler != null)
        {
            ActionSystem.UnSubscribeReaction<StartTurnGA> (_handler, EReactionTiming.Post);
            _handler = null;
        }

        UnregisterBehaviorLifecycle(owner);
    }
}

// 적 스폰시 적에게 바로 발동하는 유물
[Serializable]
public class ArtifactTrigger_OnEnemySpawn : ArtifactTrigger
{
    private Action<SpawnEnemyGA> _spawnHandler;

    public override void Register(Artifact owner)
    {
        RegisterBehaviorLifecycle(owner);
        foreach (CharacterView view in CharacterSystem.Instance.Enemies)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.OnApply (view);
            }
        }

        _spawnHandler = (spawnGA) =>
        {
            if (spawnGA.Enemy == null) return;

            EnemyView targetView = CharacterSystem.Instance.Enemies.FirstOrDefault (view => view.Enemy == spawnGA.Enemy);

            foreach (var behavior in Behaviors)
            {
                behavior.OnApply (targetView);
            }
        };

        ActionSystem.SubscribeReaction<SpawnEnemyGA> (_spawnHandler, EReactionTiming.Post);
    }

    public override void Unregister(Artifact owner)
    {
        if (Behaviors == null) return;

        foreach (var view in CharacterSystem.Instance.Enemies)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.OnRemove (view);
            }
        }

        if (_spawnHandler != null)
        {
            ActionSystem.UnSubscribeReaction<SpawnEnemyGA> (_spawnHandler, EReactionTiming.Post);
            _spawnHandler = null;
        }

        UnregisterBehaviorLifecycle(owner);
    }
}

// 조건에 따라 발동하는 유물
[Serializable]
public class ArtifactTrigger_ConditionEffect : ArtifactTrigger
{
    [SerializeReference, SR] private Condition _condition;
    private Action<GameAction> _handler;

    public override void Register(Artifact owner)
    {
        if (_condition == null)
        {
            return;
        }

        RegisterBehaviorLifecycle(owner);

        CharacterView defaultOwner = ArtifactExecutionContext.GetDefaultCaster (owner);
        if (defaultOwner != null)
        {
            _condition.SetOwner(defaultOwner);
        }

        _handler = (gameAction) =>
        {
            if (!_condition.SubConditionIsMet(gameAction))
            {
                return;
            }

            if (Behaviors == null)
            {
                return;
            }

            CharacterView caster = ArtifactExecutionContext.ResolveCaster(owner, gameAction);
            List<CharacterView> targets = ArtifactExecutionContext.ResolveTargets(gameAction);

            foreach (var behavior in Behaviors)
            {
                behavior.ModifyAction (gameAction);
                behavior.Execute (owner, caster, targets);
            }

            EventBus.Publish (new StArtifactTriggeredEvent (owner));
        };

        _condition.SubscribeCondition(_handler);
    }

    public override void Unregister(Artifact owner)
    {
        if (_condition == null || _handler == null)
        {
            UnregisterBehaviorLifecycle(owner);
            return;
        }

        _condition.UnsubscribeCondition(_handler);
        _handler = null;
        UnregisterBehaviorLifecycle(owner);
    }
}

[Serializable]
public class ArtifactTrigger_EventWeightModifier : ArtifactTrigger
{
    public override void Register(Artifact owner)
    {
        RegisterBehaviorLifecycle(owner);
        foreach (var behavior in Behaviors)
        {
            if (behavior is ArtifactBehavior_ModifyEventWeight weightBehavior)
            {
                if (MyEventSystem.Instance != null)
                {
                    MyEventSystem.Instance.AddModifier (weightBehavior.OnModifyWeight);
                }
            }
        }
    }

    public override void Unregister(Artifact owner)
    {
        foreach (var behavior in Behaviors)
        {
            if (behavior is ArtifactBehavior_ModifyEventWeight weightBehavior)
            {
                if (MyEventSystem.Instance != null)
                {
                    MyEventSystem.Instance.RemoveModifier (weightBehavior.OnModifyWeight);
                }
            }
        }

        UnregisterBehaviorLifecycle(owner);
    }
}
