using SerializeReferenceEditor;
using UnityEngine;

[System.Serializable]
public class StatusEffect
{
    [field: SerializeReference, SR] public Effect Effect { get; private set; }
    [field: SerializeReference, SR] public Effect ReleaseEffect { get; private set; }
}

[CreateAssetMenu(fileName = "SO_StatusData", menuName = "Scriptable Objects/SO_StatusData")]
public class SO_StatusData : ScriptableObject
{
    [field: SerializeField] public string StatusName { get; private set; }
    [field: SerializeField] public Sprite StatusSprite { get; private set; }
    [field: SerializeField, TextArea] public string StatusExplain { get; private set; }
    [field: SerializeField] public EStatusType StatusType { get; private set; }
    [field: SerializeField] public EStatusCategory StatusCategory { get; private set; }
    [field: SerializeField] public bool IsSingleTurn { get; private set; } = false; // 1턴짜리 Status
    [field: SerializeField] public bool IsStackable { get; private set; } = true; // 중첩가능 여부
    [field: SerializeField] public StatusEffect[] StatusEffects { get; private set; }
    [field: SerializeReference, SR] public Condition StatusTriggerCondition { get; private set; } // status 효과 발동 시점
    [field: SerializeReference, SR] public Condition StatusExpireCondition { get; private set; } // status 만료 조건
}
