using SerializeReferenceEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_KeywordData", menuName = "Scriptable Objects/SO_KeywordData")]
public class SO_KeywordData : ScriptableObject
{
    [field: SerializeField] public int Id;
    [field: SerializeField] public int UpgradedId;
    [field: SerializeField] public string KeywordName;
    [field: SerializeField] public string KeywordExplain;
    [field: SerializeField] public string KeywordSpriteName;
    [field: SerializeField] public int Rank;
    [field: SerializeField] public ECharacterAnimationType CharacterAnimationType;
    [field: SerializeField] public EKeywordType KeywordType;
    [field: SerializeField] public EKeyword Keyword;
    [field: SerializeField, Header ("잠금을 걸려면 체크")] public bool IsLocked = false;
    [field: SerializeField, Header("타겟을 지정해야하면 체크")] public bool IsTargetRequired;
    [field: SerializeField, Header("적의 행동카운트를 감소하는 유무")] public bool IsDecreaseActCount = true;
    [field: SerializeField, Header("토큰을 클릭함으로서 발동되는 키워드")] public bool IsClickableKeyword = false;
    [field: SerializeReference, SR, Header("동사 효과")] public Effect[] VerbEffects = new Effect[0];
    [field: SerializeField, Header("부사 효과")] public AdverbSkillEffect AdverbSkill;
    [field: SerializeReference, SR, Header("클릭 효과")] public Effect[] ClickEffects = new Effect[0];
}
