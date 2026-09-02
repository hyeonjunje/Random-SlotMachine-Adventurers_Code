using System.Collections.Generic;
using System.Linq;

public static class ArtifactExecutionContext
{
    public static CharacterView GetOwnerView(Artifact artifact)
    {
        if (artifact?.OwnerPlayer == null || CharacterSystem.Instance == null)
        {
            return null;
        }

        return CharacterSystem.Instance.Players.FirstOrDefault (playerView => playerView?.Player == artifact.OwnerPlayer);
    }

    public static CharacterView GetDefaultCaster(Artifact artifact = null)
    {
        CharacterView ownerView = GetOwnerView (artifact);
        if (ownerView != null)
        {
            return ownerView;
        }

        if (CharacterSystem.Instance == null || CharacterSystem.Instance.Players == null)
        {
            return null;
        }

        PlayerView alivePlayer = CharacterSystem.Instance.Players.FirstOrDefault(player => player != null && !player.Character.IsDead);
        if (alivePlayer != null)
        {
            return alivePlayer;
        }

        return CharacterSystem.Instance.Players.FirstOrDefault();
    }

    public static CharacterView ResolveCaster(Artifact artifact, GameAction action)
    {
        CharacterView ownerView = GetOwnerView (artifact);
        if (ownerView != null)
        {
            return ownerView;
        }

        switch (action)
        {
            case DealDamageGA damageGA:
                return damageGA.Caster ?? GetDefaultCaster (artifact);
            case AddStatusGA addStatusGA:
                return addStatusGA.Caster ?? GetDefaultCaster (artifact);
            case ChangeStatValueGA changeStatGA:
                return changeStatGA.Caster ?? GetDefaultCaster (artifact);
            case ChangeEnemyActCountGA:
            case DealDamage_CounterAttackGA:
            case StartBattleGA:
                return GetDefaultCaster (artifact);
            case ClickUseSlotMachineTokenGA clickTokenGA:
                return clickTokenGA.BattleAct?.CharacterView ?? GetDefaultCaster (artifact);
            case ActAutoBattleGA autoBattleGA:
                return autoBattleGA.BattleAct?.CharacterView ?? GetDefaultCaster (artifact);
            case UseSkillGA useSkillGA:
                return useSkillGA.Caster ?? GetDefaultCaster (artifact);
            default:
                return GetDefaultCaster (artifact);
        }
    }

    public static List<CharacterView> ResolveTargets(GameAction action)
    {
        switch (action)
        {
            case DealDamageGA damageGA:
                return damageGA.Targets;
            case AddStatusGA addStatusGA:
                return addStatusGA.Targets;
            case ChangeStatValueGA changeStatGA:
                return changeStatGA.Targets;
            case ChangeEnemyActCountGA changeEnemyActCountGA:
                return changeEnemyActCountGA.Targets;
            case DealDamage_CounterAttackGA counterAttackGA:
                return counterAttackGA.Targets;
            default:
                return null;
        }
    }
}
