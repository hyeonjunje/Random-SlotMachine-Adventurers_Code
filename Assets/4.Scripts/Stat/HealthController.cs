using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class HealthController
{
    public int CurrentHp { get; private set; }
    public int MaxHp { get; private set; }
    public int Shield { get; private set; }
    public bool IsFull => CurrentHp == MaxHp;
    public bool IsDead => CurrentHp <= 0;

    private bool _isShieldClear = false;

    public event Action<CharacterView> OnDead; // killer
    public event Action<int, int> OnChangeHp; // CurrentHp, MaxHp
    public event Action<int, int> OnChangeShield; // Prev, Current
    public event Action<int, int> OnDealDamage; // Prev, Current
    public event Action<int, int> OnRestoreHealth; // Prev, Current

    public HealthController(int maxHp)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Shield = 0;
    }

    public void Release()
    {
        OnDead = null;
        OnChangeHp = null;
        OnChangeShield = null;
        OnDealDamage = null;
        OnRestoreHealth = null;
    }

    public void Init()
    {
        CurrentHp = MaxHp;

        OnChangeHp?.Invoke(CurrentHp, MaxHp);
        OnChangeShield?.Invoke(0, Shield);
    }

    public void ChangeMaxHp(int amount)
    {
        int prevMaxHp = MaxHp;
        MaxHp += amount;

        if (amount > 0)
        {
            CurrentHp += amount;
        }

        CurrentHp = Mathf.Clamp (CurrentHp, 0, MaxHp);

        OnChangeHp?.Invoke (CurrentHp, MaxHp);
    }

    public void SetCurrentHp(int currentHp)
    {
        int prevHp = CurrentHp;
        CurrentHp = Mathf.Clamp(currentHp, 0, MaxHp);

        OnChangeHp?.Invoke(CurrentHp, MaxHp);

        if (prevHp > 0 && CurrentHp <= 0)
        {
            OnDead?.Invoke(null);
        }
    }

    public void DealDamage(CharacterView caster, Character target, int damage, bool isIgnoresDefense)
    {
        int prevHp = CurrentHp;
        int prevShield = Shield;

        BattleSystem.Instance.RecentlyOriginDealDamage = damage;

        if (target.BattleSideType == EBattleSideType.OurSide && ArtifactRuntimeState.PlayerDamageTakenFlatModifier != 0)
        {
            damage = Mathf.Max(0, damage + ArtifactRuntimeState.PlayerDamageTakenFlatModifier);
        }

        if (target.BattleSideType == EBattleSideType.OurSide &&
            !Mathf.Approximately(ArtifactRuntimeState.PlayerDamageTakenMultiplier, 1f))
        {
            damage = Mathf.Max(0, Mathf.RoundToInt(damage * ArtifactRuntimeState.PlayerDamageTakenMultiplier));
        }

        if (target.BattleSideType == EBattleSideType.OurSide &&
            caster != null &&
            caster.Character != null &&
            caster.Character.BattleSideType == EBattleSideType.EnemySide &&
            caster.Character.IsStatus(EStatusType.Weakening) &&
            ArtifactRuntimeState.RollChance(ArtifactRuntimeState.NullifyWeakenedEnemyDamageChancePercent))
        {
            damage = 0;
        }

        if(Shield > 0 && isIgnoresDefense == false)
        {
            int prevDamage = damage;
            damage = Mathf.Clamp(damage - Shield, 0, GameDefine.MAX);
            Shield = Mathf.Clamp(Shield - prevDamage, 0, GameDefine.MAX);

            if(Shield > 0)
            {
                AudioManager.Instance.PlaySFX(ESfxId.Guard);
            }

            OnChangeShield?.Invoke(prevShield, Shield);
        }

        // 회피상태면 데미지 0으로 취급
        if(target.IsStatus(EStatusType.Evasion))
        {
            damage = 0;
        }

        BattleSystem.Instance.RecentlyRealDealDamage = damage;

        CurrentHp = Mathf.Clamp(CurrentHp - damage, 0, MaxHp);

        if(damage > 0)
        {
            if (target.BattleSideType == EBattleSideType.OurSide)
            {
                ArtifactRuntimeState.AddPartyDamageTaken(damage);
            }

            AudioManager.Instance.PlaySFX(ESfxId.Hit);
        }

        OnDealDamage?.Invoke(prevHp, CurrentHp);
        OnChangeHp?.Invoke(CurrentHp, MaxHp);

        if (CurrentHp <= 0)
        {
            OnDead?.Invoke(caster);
        }
    }

    // 예상 데미지 반환
    public int GetExpectedDamage(CharacterView caster, int damage, bool isIgnoresDefense)
    {
        int prevHp = CurrentHp;
        int prevShield = Shield;

        if (Shield > 0 && isIgnoresDefense == false)
        {
            damage = Mathf.Clamp(damage - Shield, 0, GameDefine.MAX);
        }

        return damage;
    }

    public void RestoreHealth(int amount)
    {
        int prevHp = CurrentHp;
        CurrentHp = Mathf.Clamp(CurrentHp + amount, 0, MaxHp);

        OnRestoreHealth?.Invoke(prevHp, CurrentHp);
        OnChangeHp?.Invoke(CurrentHp, MaxHp);
    }

    public void AddShield(int amount)
    {
        int prev = Shield;
        Shield += amount;

        OnChangeShield?.Invoke(prev, Shield);

        _isShieldClear = false;
    }

    public void ClearShield(bool isPreservation)
    {
        if(_isShieldClear)
        {
            return;
        }
        _isShieldClear = true;

        int prev = Shield;

        if(isPreservation)
        {
            Shield = Mathf.RoundToInt(Shield * DataManager.Instance.GameModel.PreservationValue);
        }
        else
        {
            Shield = 0;
        }

        OnChangeShield?.Invoke(prev, Shield);
    }

    public void ResetForLoad(int maxHp, int currentHp, int shield = 0)
    {
        MaxHp = Mathf.Max (0, maxHp);
        CurrentHp = Mathf.Clamp (currentHp, 0, MaxHp);
        Shield = Mathf.Max (0, shield);

        OnChangeHp?.Invoke (CurrentHp, MaxHp);
        OnChangeShield?.Invoke (0, Shield);
    }

}

