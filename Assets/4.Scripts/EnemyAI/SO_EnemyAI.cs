using JetBrains.Annotations;
using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyActTransition
{
    [field: SerializeField] public int NextId { get; private set; } = 0;  // 다음 인덱스
    [field: SerializeReference, SR] public Condition Condition { get; private set; } // 조건
}

[System.Serializable]
public class EnemyActGroup
{
    [field: SerializeField, Min(1)] public int Id { get; private set; } = 1;
    [field: SerializeField] public bool IsStart { get; private set; } = false;
    [field: SerializeField] public int NextId { get; private set; } = 0;
    [field: SerializeField] public List<EnemyAct> Acts { get; private set; } = new List<EnemyAct>();
    [field: SerializeField] public List<EnemyActTransition> EnemyActTransitions { get; private set;} = new List<EnemyActTransition>();

    // 에디터 전용: 노드 에디터에서의 위치
    [field: SerializeField, HideInInspector] public Vector2 NodePosition { get; private set; } = Vector2.zero;
    [field: SerializeField, HideInInspector] public float NodeWidth { get; private set; } = 280f; 
}

[System.Serializable]
public class EnemyAct
{
    [field: SerializeField, Range(0, 1)] public float Probability { get; private set; } = 0;
    [field: SerializeField, Header("-1은 무한대")] public int ActCount { get; private set; } = -1;
    [field: SerializeField] public int RepeatLimit { get; private set; } = -1;

    [field: SerializeField] public EEnemyActType EnemyActType { get; private set; }
    [field: SerializeField] public int Value1 { get; private set; } // EnemyActType에 따른 수치1
    [field: SerializeField] public int Value2 { get; private set; } // EnemyActType에 따른 수치2

    [field: SerializeField] public ECharacterAnimationType CharacterAnimationType { get; private set; }
    [field: SerializeReference, SR] public Effect[] Effects { get; private set; }

    public string GetActName()
    {
        return LocalizationManager.Instance.Get("EnemyAct_Name_" + EnemyActType.ToString());
    }

    public string GetActIconName()
    {
        string result = "";
        switch (EnemyActType)
        {
            case EEnemyActType.None:
                result = "부사_그냥";
                break;
            case EEnemyActType.Attack:
                result = "동사_공격해라";
                break;
            case EEnemyActType.Defense:
                result = "동사_깨뜨려라";
                break;
            case EEnemyActType.Special:
                result = "부사_그냥";
                break;
            case EEnemyActType.Buff:
                result = "부사_강하게";
                break;
            case EEnemyActType.Debuff:
                result = "부사_약화로";
                break;
            case EEnemyActType.AttackAndBuff:
                result = "부사_강하게";
                break;
            case EEnemyActType.AttackAndDeBuff:
                result = "부사_강하게";
                break;
            case EEnemyActType.DefenseAndBuff:
                result = "부사_강하게";
                break;
            case EEnemyActType.DefenseAndDeBuff:
                result = "부사_강하게";
                break;
            case EEnemyActType.AttackAndDefense:
                result = "부사_강하게";
                break;
            case EEnemyActType.SpecialAndBuff:
                result = "부사_강하게";
                break;
            case EEnemyActType.SpecialAndDeBuff:
                result = "부사_강하게";
                break;
        }
        return result;
    }

    public string GetActExplain(CharacterView caster)
    {
        string result = "";

        string key = "EnemyAct_Explain_" + EnemyActType.ToString();

        switch (EnemyActType)
        {
            case EEnemyActType.Attack:
                {
                    int expectedValue = Mathf.RoundToInt(caster.Character.GetStat(EStatType.AttackPower).Value * Value1 / 100f);
                    result = string.Format(LocalizationManager.Instance.Get(key), Value1, expectedValue, Value2);
                }
                break;
            case EEnemyActType.Defense:
                {
                    int expectedValue = Mathf.RoundToInt(caster.Character.GetStat(EStatType.AttackPower).Value * Value1 / 100f);
                    result = string.Format(LocalizationManager.Instance.Get(key), Value1, expectedValue);
                }
                break;
            case EEnemyActType.AttackAndBuff:
                {
                    int expectedValue = Mathf.RoundToInt(caster.Character.GetStat(EStatType.AttackPower).Value * Value1 / 100f);
                    result = string.Format(LocalizationManager.Instance.Get(key), Value1, expectedValue, Value2);
                }
                break;
            case EEnemyActType.AttackAndDeBuff:
                {
                    int expectedValue = Mathf.RoundToInt(caster.Character.GetStat(EStatType.AttackPower).Value * Value1 / 100f);
                    result = string.Format(LocalizationManager.Instance.Get(key), Value1, expectedValue, Value2);
                }
                break;
            case EEnemyActType.DefenseAndBuff:
                {
                    int expectedValue = Mathf.RoundToInt(caster.Character.GetStat(EStatType.AttackPower).Value * Value1 / 100f);
                    result = string.Format(LocalizationManager.Instance.Get(key), Value1, expectedValue);
                }
                break;
            case EEnemyActType.DefenseAndDeBuff:
                {
                    int expectedValue = Mathf.RoundToInt(caster.Character.GetStat(EStatType.AttackPower).Value * Value1 / 100f);
                    result = string.Format(LocalizationManager.Instance.Get(key), Value1, expectedValue);
                }
                break;
            case EEnemyActType.AttackAndDefense:
                {
                    int expectedValue = Mathf.RoundToInt(caster.Character.GetStat(EStatType.AttackPower).Value * Value1 / 100f);
                    result = string.Format(LocalizationManager.Instance.Get(key), Value1, expectedValue);
                }
                break;
            case EEnemyActType.None:
            case EEnemyActType.Special:
            case EEnemyActType.Buff:
            case EEnemyActType.Debuff:
            case EEnemyActType.SpecialAndBuff:
            case EEnemyActType.SpecialAndDeBuff:
                result = LocalizationManager.Instance.Get(key);
                break;
        }

        return result;
    }
}

[CreateAssetMenu(fileName = "SO_EnemyAI", menuName = "Scriptable Objects/SO_EnemyAI")]
public class SO_EnemyAI : ScriptableObject
{
    [field: SerializeField] public List<EnemyActGroup> EnemyActGroup { get; private set; } = new List<EnemyActGroup>();
}
