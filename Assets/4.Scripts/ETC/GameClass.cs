using UnityEngine;

[System.Serializable]
public class DamageFormula
{
    [field: SerializeField] public EDamageFormulaType DamageFormulaType;
    [field: SerializeField] public float Value;
    [field: SerializeField] public bool IsIgnoresDefense = false; // 방어도 무시

    public DamageFormula(EDamageFormulaType damageFormulaType, float value)
    {
        DamageFormulaType = damageFormulaType;
        Value = value;
    }

    // 원본 데이터를 수정방지를 위한 복사 생성자
    public DamageFormula(DamageFormula copyDamageFormula)
    {
        DamageFormulaType = copyDamageFormula.DamageFormulaType;
        Value = copyDamageFormula.Value;
        IsIgnoresDefense = copyDamageFormula.IsIgnoresDefense;
    }
}

[System.Serializable]
public class HealingFormula
{
    [field: SerializeField] public EHealingFormulaType HealingFormulaType;
    [field: SerializeField] public float Value;

    public HealingFormula(EHealingFormulaType healingFormulaType, float value)
    {
        HealingFormulaType = healingFormulaType;
        Value = value;
    }

    // 원본 데이터를 수정방지를 위한 복사 생성자
    public HealingFormula(HealingFormula copyHealingFormula)
    {
        HealingFormulaType = copyHealingFormula.HealingFormulaType;
        Value = copyHealingFormula.Value;
    }
}

[System.Serializable]
public class ShieldFormula
{
    [field: SerializeField] public EShieldFormulaType ShieldFormulaType;
    [field: SerializeField] public float Value;

    public ShieldFormula(EShieldFormulaType shieldFormulaType, float value)
    {
        ShieldFormulaType = shieldFormulaType;
        Value = value;
    }

    // 원본 데이터를 수정방지를 위한 복사 생성자
    public ShieldFormula(ShieldFormula copyShieldFormula)
    {
        ShieldFormulaType = copyShieldFormula.ShieldFormulaType;
        Value = copyShieldFormula.Value;
    }
}

public class BattleAct
{
    public CharacterView CharacterView { get; private set; }
    public Skill Skill { get; private set; }
    public EBingo Bingo { get; private set; }
    public bool IsPlayer { get; private set; }

    public BattleAct(CharacterView characterView, Skill skill, bool isPlayer, EBingo bingo = EBingo.None)
    {
        CharacterView = characterView;
        Skill = skill;
        Bingo = bingo;
        IsPlayer = isPlayer;
    }
}