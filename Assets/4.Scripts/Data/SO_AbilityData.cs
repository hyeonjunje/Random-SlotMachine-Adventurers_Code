using SerializeReferenceEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_AbilityData", menuName = "Scriptable Objects/SO_AbilityData")]
public class SO_AbilityData : ScriptableObject
{
    [field:SerializeField] public string AbilityName { get; private set; }
    [field:SerializeField] public Sprite AbilitySprite { get; private set; }
    [field:SerializeField, TextArea] public string AbilityExplain { get; private set; }
    [field: SerializeReference, SR] public Effect[] Effects { get; private set; }
    [field: SerializeReference, SR] public Condition Condition { get; private set; }
}
