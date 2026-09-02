using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatEffectSystem : SingletonScene<CombatEffectSystem>
{
    protected override void OnAwakeSingleton()
    {
        ActionSystem.AttachPerformer<PerformEffectGA>(PerformEffectPerformer);

        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);

        ActionSystem.AttachPerformer<DealDamage_ElectricGA>(DealDamage_ElectricPerformer);
        ActionSystem.AttachPerformer<DealDamage_PoisionGA>(DealDamage_PoisionPerformer);
        ActionSystem.AttachPerformer<DealDamage_CounterAttackGA>(DealDamage_CounterAttackPerformer);
        ActionSystem.AttachPerformer<DealDamage_PunishmentGA>(DealDamage_PunishmentPerformer);

        ActionSystem.AttachPerformer<ApplyHealingGA>(ApplyHealingPerformer);
        ActionSystem.AttachPerformer<AddShieldGA>(AddShieldPerformer);

        ActionSystem.AttachPerformer<AddTempExtraValueGA>(AddTempExtraValuePerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEffectGA>();

        ActionSystem.DetachPerformer<DealDamageGA>();

        ActionSystem.DetachPerformer<DealDamage_ElectricGA>();
        ActionSystem.DetachPerformer<DealDamage_PoisionGA>();
        ActionSystem.DetachPerformer<DealDamage_CounterAttackGA>();
        ActionSystem.DetachPerformer<DealDamage_PunishmentGA>();

        ActionSystem.DetachPerformer<ApplyHealingGA>();
        ActionSystem.DetachPerformer<AddShieldGA>();

        ActionSystem.DetachPerformer<AddTempExtraValueGA>();
    }

    private IEnumerator PerformEffectPerformer(PerformEffectGA performEffectGA)
    {
        if (performEffectGA.Effect == null)
        {
            Debug.Log("왜 널인거야?");
            yield break;
        }

        float battleTimeScale = StyleManager.Instance.AnimationTimeData.SafeBattleTimeScale;
        yield return new WaitForSeconds(performEffectGA.Effect.DelayTime / battleTimeScale);

        // 이펙트 켜주기
        if (string.IsNullOrEmpty(performEffectGA.Effect.TargetEffect) == false)
        {
            foreach (CharacterView target in performEffectGA.Targets)
            {
                if (target == null)
                {
                    continue;
                }

                GameObject objEffect = Creator.Instance.CreatAsset<GameObject>(performEffectGA.Effect.TargetEffect);

                if(performEffectGA.Effect.TargetEffect == "Attack" && performEffectGA.Caster != null)
                {
                    if(performEffectGA.Caster.Character is Player player) // 플레이어 타입별 공격 이펙트 하드코딩
                    {
                        objEffect = Creator.Instance.GetPlayerEffect(player.PlayerData.PlayerJob);
                    }
                    else if(performEffectGA.Caster.Character is Enemy enemy) // 몬스터 타입별 공격 이펙트 하드코딩
                    {
                        objEffect = Creator.Instance.GetEnemyEffect(enemy.EnemyData.Id);
                    }
                }

                if (objEffect != null)
                {
                    objEffect.transform.position = target.GetPositionCenter();
                }
            }
        }

        GameAction effectAction = performEffectGA.Effect.GetGameAction(performEffectGA.Targets, performEffectGA.Caster);
        ActionSystem.Instance.AddReaction(effectAction);

        // 카메라 흔들림 기능
        if (effectAction is ICameraControllableGA cameraControllable)
        {
            StyleManager.Instance.PlayCameraAction(cameraControllable.CameraActionType);
        }
    }

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        yield return null;

        foreach (CharacterView target in dealDamageGA.Targets)
        {
            target.DealDamage(dealDamageGA.Caster, dealDamageGA.DamageFormula);
        }
    }

    private IEnumerator DealDamage_ElectricPerformer(DealDamage_ElectricGA dealDamage_ElectricGA)
    {
        yield return null;

        foreach (CharacterView target in dealDamage_ElectricGA.Targets)
        {
            // 제일 최근 데미지의 (DataManager.Instance.GameModel.EletricValue)% 로 공격
            target.DealDamage(Mathf.RoundToInt(BattleSystem.Instance.RecentlyRealDealDamage * DataManager.Instance.GameModel.EletricValue));
        }
    }

    private IEnumerator DealDamage_PoisionPerformer(DealDamage_PoisionGA dealDamage_PoisionGA)
    {
        yield return null;

        foreach (CharacterView target in dealDamage_PoisionGA.Targets)
        {
            target.DealDamage(target.Character.GetStatus(EStatusType.Poison));
        }
    }

    private IEnumerator DealDamage_CounterAttackPerformer(DealDamage_CounterAttackGA dealDamage_CounterAttackGA)
    {
        // 반격 데미지는 파티 공격력 평균으로 한다.
        int damage = 0;
        foreach(PlayerView playerView in CharacterSystem.Instance.Players)
        {
            playerView.SetAnimation(dealDamage_CounterAttackGA.CharacterAnimationType);
            damage += playerView.Player.GetStat(EStatType.AttackPower).Value;
        }
        damage = Mathf.RoundToInt(damage / CharacterSystem.Instance.Players.Count);

        PlayerView caster = CharacterSystem.Instance.Players[0];
        // 애니메이션 트리거까지의 시간
        yield return new WaitForSeconds(caster.AnimationController.GetTimeUntilEvent(dealDamage_CounterAttackGA.CharacterAnimationType));

        foreach (CharacterView target in dealDamage_CounterAttackGA.Targets)
        {
            target.DealDamage(Mathf.RoundToInt(damage * DataManager.Instance.GameModel.CounterAttackValue));
        }
    }

    private IEnumerator DealDamage_PunishmentPerformer(DealDamage_PunishmentGA dealDamage_PunishmentGA)
    {
        // 응징 데미지는 파티 공격력 평균으로 한다.
        int damage = 0;
        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            playerView.SetAnimation(dealDamage_PunishmentGA.CharacterAnimationType);
            damage += playerView.Player.GetStat(EStatType.AttackPower).Value;
        }
        damage = Mathf.RoundToInt(damage / CharacterSystem.Instance.Players.Count);

        PlayerView caster = CharacterSystem.Instance.Players[0];
        // 애니메이션 트리거까지의 시간
        yield return new WaitForSeconds(caster.AnimationController.GetTimeUntilEvent(dealDamage_PunishmentGA.CharacterAnimationType));

        foreach (CharacterView target in dealDamage_PunishmentGA.Targets)
        {
            target.DealDamage(Mathf.RoundToInt(damage * DataManager.Instance.GameModel.PunishmentAttackValue));
        }
    }

    private IEnumerator ApplyHealingPerformer(ApplyHealingGA applyHealingGA)
    {
        yield return null;

        HashSet<HealthController> healedTargets = new HashSet<HealthController>();
        foreach (CharacterView target in applyHealingGA.Targets)
        {
            if (target?.Character?.HealthController == null || !healedTargets.Add(target.Character.HealthController))
            {
                continue;
            }

            target.RestoreHealth(applyHealingGA.Caster, applyHealingGA.HealingFormula);
        }
    }

    private IEnumerator AddShieldPerformer(AddShieldGA addShieldGA)
    {
        yield return null;

        HashSet<HealthController> shieldedTargets = new HashSet<HealthController>();
        foreach (CharacterView target in addShieldGA.Targets)
        {
            if (target?.Character?.HealthController == null || !shieldedTargets.Add(target.Character.HealthController))
            {
                continue;
            }

            target.AddShield(addShieldGA.Caster, addShieldGA.ShieldFormula);
        }
    }

    private IEnumerator AddTempExtraValuePerformer(AddTempExtraValueGA addTempExtraValueGA)
    {
        yield return null;

        if((addTempExtraValueGA.TargetType & EAdverbEffectTargetType.DealDamage) != 0)
        {
            DataManager.Instance.GameModel.DealDamageExtraValue += addTempExtraValueGA.ExtraValue;
        }

        if ((addTempExtraValueGA.TargetType & EAdverbEffectTargetType.AddShield) != 0)
        {
            DataManager.Instance.GameModel.AddShieldExtraValue += addTempExtraValueGA.ExtraValue;
        }

        if ((addTempExtraValueGA.TargetType & EAdverbEffectTargetType.ApplyHealing) != 0)
        {
            DataManager.Instance.GameModel.ApplyHealingExtraValue += addTempExtraValueGA.ExtraValue;
        }
    }
}
