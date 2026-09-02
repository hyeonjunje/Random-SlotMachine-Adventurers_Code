using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character
{
    // Static Data
    public SO_CharacterData CharacterData { get; private set; }

    // Runtime Data
    public int PosIndex { get; set; }
    public bool IsDead => HealthController.IsDead;
    public EBattleSideType BattleSideType { get; private set; }

    private Dictionary<EStatType, Stat> _stats = new Dictionary<EStatType, Stat>();

    public Action OnDataChanged;

    public Ability Ability { get; private set; }
    public HealthController HealthController { get; private set; }
    public StatusController StatusController { get; private set; }

    public Character(SO_CharacterData characterData, EBattleSideType battleSideType)
    {
        CharacterData = characterData;

        Setup(characterData);

        BattleSideType = battleSideType;
    }

    public void Setup(SO_CharacterData characterData)
    {
        // 스텟
        _stats.Add(EStatType.MaxHp, new Stat(characterData.Stats.maxHp, LocalizationManager.Instance.Get("CS_CHARACTER_001")));
        _stats.Add(EStatType.AttackPower, new Stat(characterData.Stats.attackPower, LocalizationManager.Instance.Get("CS_CHARACTER_002")));

        OnDataChanged?.Invoke();
    }

    public void SetHealthController(HealthController healthController)
    {
        HealthController = healthController;
    }

    public void SetStatusController(StatusController statusController)
    {
        StatusController = statusController;
    }

    // CharacterView가 파괴되거나 없어질 시 Character 객체를 해제해주는 메소드
    public virtual void Release()
    {
        HealthController.Release();
        StatusController.Release();

        Ability?.Release();

        OnDataChanged = null;
    }

    public void SetAbilty(Ability ability)
    {
        Ability = ability;
        Ability.Add();

        StatusController.AddAbility(Ability);
    }

    public virtual void StartTurn()
    {
        // 턴 시작시 방어도 초기화 (보존 상태 있으면 보존)
        HealthController.ClearShield(IsStatus(EStatusType.Preservation));
    }

    public virtual void EndTurn()
    {

    }

    public Stat GetStat(EStatType statType)
    {
        if(_stats.TryGetValue(statType, out Stat stat))
        {
            return stat;
        }
        return null;
    }

    public void AddStatus(Status newStatus)
    {
        StatusController.AddStatus(newStatus);
    }

    public void ReleaseStatus(EStatusType statusType)
    {
        StatusController.ReleaseStatus(statusType);
    }

    public void UpdateStatus(Status status)
    {
        StatusController.UpdateStatus(status);
    }

    public bool IsStatus(EStatusType statusType)
    {
        return StatusController.IsStatus(statusType);
    }

    public int GetStatus(EStatusType statusType)
    {
        return StatusController.GetStack(statusType);
    }

    public List<Status> GetStatusesByCategory(EStatusCategory StatusCategory)
    {
        return StatusController.GetStatusesByCategory(StatusCategory);
    }

    // 정석적인 공격에 의한 딜데미지 처리 메소드
    public void DealDamage(CharacterView caster, DamageFormula damageFormula)
    {
        if (HealthController.IsDead)
        {
            return;
        }

        int damage = CalculateDamage(caster?.Character, this, damageFormula);
        HealthController.DealDamage(caster, this, damage, damageFormula.IsIgnoresDefense);
    }

    // 받을 예상 데미지 반환
    public int GetExpectedDamage(CharacterView caster, DamageFormula damageFormula)
    {
        int damage = CalculateDamage(caster?.Character, this, damageFormula);
        return HealthController.GetExpectedDamage(caster, damage, damageFormula.IsIgnoresDefense);
    }

    // 중독, 감전과 같은 방어도 무시하는 상태이상 딜데미지 처리 메소드
    public void DealDamage(int damage)
    {
        if (HealthController.IsDead)
        {
            return;
        }

        HealthController.DealDamage(null, this, damage, true);
    }

    public void RestoreHealth(CharacterView caster, HealingFormula healingFormula)
    {
        if (HealthController.IsDead)
        {
            return;
        }

        int amount = CalculateHealing(caster?.Character, this, healingFormula);

        HealthController.RestoreHealth(amount);
    }

    public void AddShield(CharacterView caster, ShieldFormula shieldFormula)
    {
        if (HealthController.IsDead)
        {
            return;
        }

        int amount = CalculateShield(caster?.Character, this, shieldFormula);

        HealthController.AddShield(amount);
    }

    private int CalculateDamage(Character caster, Character target, DamageFormula damageFormula)
    {
        int result = 0;
        float postValue = 0; // 부동소수로 인한 반올림계산이 정확하지 않아 계산한 값에 적당히 작은 값을 더한 수치
        float extraValue = 0;

        if(caster is Player)
        {
            extraValue = DataManager.Instance.GameModel.DealDamageExtraValue;
        }

        switch (damageFormula.DamageFormulaType)
        {
            case EDamageFormulaType.Flat: // 고정 데미지
                postValue = damageFormula.Value + GameDefine.EPSILON;
                break;
            case EDamageFormulaType.AddPercentForAttackPower: // 내 공격력 데미지에 %만큼 더함
                postValue = caster.GetStat(EStatType.AttackPower).Value * (1 + damageFormula.Value + extraValue) + GameDefine.EPSILON;
                break;
            case EDamageFormulaType.SetPercentForAttackPower:   // 내 공격력 데미지에 %만 적용
                postValue = caster.GetStat(EStatType.AttackPower).Value * (damageFormula.Value + extraValue) + GameDefine.EPSILON;
                break;
            case EDamageFormulaType.AddPercentForAttackPowerWhenTargetHpFull: // 타겟 full hp일 때 내 공격력 기반 곱하기 데미지 ( 1.5배, 0.5배 등..)
                if(target.HealthController.IsFull)
                {
                    postValue = caster.GetStat(EStatType.AttackPower).Value * (1 + damageFormula.Value + extraValue) + GameDefine.EPSILON;
                }
                else
                {
                    postValue = caster.GetStat(EStatType.AttackPower).Value * (1 + extraValue) + GameDefine.EPSILON;
                }
                break;
            case EDamageFormulaType.BeforeAttackDamage: // 직전에 공격한 데미지에 %만 적용
                postValue = BattleSystem.Instance.RecentlyOriginDealDamage * damageFormula.Value + GameDefine.EPSILON;
                break;
            case EDamageFormulaType.BeforeAttackedDamage: // 직전에 실제로 입힌 데미지에 %만 적용
                postValue = BattleSystem.Instance.RecentlyRealDealDamage * damageFormula.Value + GameDefine.EPSILON;
                break;
            case EDamageFormulaType.AddPercentForAttackPowerAddTargetActCount: // 내 공격력 + 적 행동카운트당 N% 추가 데미지
                if(target is Enemy enemy)
                {
                    postValue = caster.GetStat(EStatType.AttackPower).Value * (1 + damageFormula.Value * enemy.EnemyAI.CurrentAct.ActCount + extraValue) + GameDefine.EPSILON;
                }
                else
                {
                    postValue = caster.GetStat(EStatType.AttackPower).Value + GameDefine.EPSILON;
                }
                break;
            case EDamageFormulaType.PercentOfMaxHP: // 최대체력 퍼센트 데미지
                postValue = target.HealthController.MaxHp * damageFormula.Value + GameDefine.EPSILON;
                break;
            case EDamageFormulaType.PercentOfCurrentHP: // 현재체력 퍼센트 데미지
                postValue = target.HealthController.CurrentHp * damageFormula.Value + GameDefine.EPSILON;
                break;
            case EDamageFormulaType.PercentOfMissingHP: // 잃은체력 퍼센트 데미지
                postValue = (target.HealthController.MaxHp - target.HealthController.CurrentHp) * damageFormula.Value + GameDefine.EPSILON;
                break;
        }

        result = Mathf.RoundToInt(postValue);

        if (caster != null && caster.IsStatus(EStatusType.Weakening))
        {
            result = Mathf.RoundToInt(result * (1 - DataManager.Instance.GameModel.WeakeningValue)); // 약화 데미지 계산
        }

        if (target.IsStatus(EStatusType.Marking) || target.IsStatus(EStatusType.Prey))
        {
            result = Mathf.RoundToInt(result * (1 + DataManager.Instance.GameModel.MarkingValue)); // 표식, 사냥감 데미지 계산
        }

        if(caster != null && caster.IsStatus(EStatusType.Guardian))
        {
            result = Mathf.RoundToInt(result * (1 - DataManager.Instance.GameModel.GuardianValue)); // 수호 데미지 계산
        }

        return result;
    }

    private int CalculateHealing(Character caster, Character target, HealingFormula healingFormula)
    {
        int result = 0;
        float postValue = 0;
        float extraValue = 0;
        if (caster is Player)
        {
            extraValue = DataManager.Instance.GameModel.ApplyHealingExtraValue;
        }

        switch (healingFormula.HealingFormulaType)
        {
            case EHealingFormulaType.Flat: // 고정 힐링
                postValue = healingFormula.Value + GameDefine.EPSILON;
                result = Mathf.RoundToInt(healingFormula.Value);
                break;
            case EHealingFormulaType.AddPercentForAttackPower: // 내 공격력 데미지에 %만큼 더함
                postValue = caster.GetStat(EStatType.AttackPower).Value * (1 + healingFormula.Value + extraValue) + GameDefine.EPSILON;
                break;
            case EHealingFormulaType.SetPercentForAttackPower: // 내 공격력 데미지에 %만 적용
                postValue = caster.GetStat(EStatType.AttackPower).Value * (healingFormula.Value + extraValue) + GameDefine.EPSILON;
                break;
            case EHealingFormulaType.BeforeAttackDamage: // 직전에 공격한 데미지에 %만 적용
                postValue = BattleSystem.Instance.RecentlyOriginDealDamage * healingFormula.Value + GameDefine.EPSILON;
                break;
            case EHealingFormulaType.BeforeAttackedDamage: // 직전에 실제로 입힌 데미지에 %만 적용
                postValue = BattleSystem.Instance.RecentlyRealDealDamage * healingFormula.Value + GameDefine.EPSILON;
                break;
            case EHealingFormulaType.PercentOfMaxHP: // 최대체력 퍼센트 
                postValue = target.HealthController.MaxHp * healingFormula.Value + GameDefine.EPSILON;
                break;
        }

        result = Mathf.RoundToInt(postValue);
        return result;
    }

    private int CalculateShield(Character caster, Character target, ShieldFormula shieldFormula)
    {
        int result = 0;
        float postValue = 0;
        float extraValue = 0;
        if (caster is Player)
        {
            extraValue = DataManager.Instance.GameModel.AddShieldExtraValue;
        }

        switch (shieldFormula.ShieldFormulaType)
        {
            case EShieldFormulaType.Flat: // 고정 방어도
                postValue = shieldFormula.Value + GameDefine.EPSILON;
                break;
            case EShieldFormulaType.AddPercentForAttackPower: // 내 공격력 데미지에 %만큼 더함
                postValue = caster.GetStat(EStatType.AttackPower).Value * (1 + shieldFormula.Value + extraValue) + GameDefine.EPSILON;
                break;
            case EShieldFormulaType.SetPercentForAttackPower: // 내 공격력 데미지에 %만 적용
                postValue = caster.GetStat(EStatType.AttackPower).Value * (shieldFormula.Value + extraValue) + GameDefine.EPSILON;
                break;
            case EShieldFormulaType.BeforeAttackDamage: // 직전에 공격한 데미지에 %만 적용
                postValue = BattleSystem.Instance.RecentlyOriginDealDamage * shieldFormula.Value + GameDefine.EPSILON;
                break;
            case EShieldFormulaType.BeforeAttackedDamage: // 직전에 실제로 입힌 데미지에 %만 적용
                postValue = BattleSystem.Instance.RecentlyRealDealDamage * shieldFormula.Value + GameDefine.EPSILON;
                break;
        }

        result = Mathf.RoundToInt(postValue);
        return result;
    }

    #region Abstract Method
    public abstract string GetName();
    #endregion
}

