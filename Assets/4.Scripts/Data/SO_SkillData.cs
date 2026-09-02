using SerializeReferenceEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_SkillData", menuName = "Scriptable Objects/SO_SkillData")]
public class SO_SkillData : ScriptableObject
{
    [field: SerializeField] public string SkillName { get; private set; }
    [field: SerializeField] public ECharacterAnimationType CharacterAnimationType { get; private set; }
    [field: SerializeReference, SR] public Effect Effect { get; private set; }
    [field: SerializeField] public int ManaCost { get; private set; }
    [field: SerializeField] public string SkillIconName { get; private set; }
    [field: SerializeField] [TextArea] public string SkillDescription { get; private set; }
}
