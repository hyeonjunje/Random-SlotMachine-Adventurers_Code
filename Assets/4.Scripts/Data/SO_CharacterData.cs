using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/SO_CharacterData")]
public class SO_CharacterData : ScriptableObject
{
    [field: SerializeField] public int Id { get; private set; }

    [field: Header("----- Base -----")]
    [field: SerializeField] public Vector2 ColliderOffset { get; private set; }
    [field: SerializeField] public Vector2 ColliderSize { get; private set; }

    [field: Header ("----- 캐릭터 스탯 -----")]
    [field: SerializeField] public STStats Stats { get; private set; }

    [field: Header ("----- 캐릭터 레벨업 증가치(레벨 +1 당) -----")]
    [field: SerializeField] public STStats LevelUpIncrements { get; private set; }

    [field: SerializeField] public GameObject CharacterPrefab { get; private set; }
    [field: SerializeField] public string PortraitIconName { get; private set; }
    [field: SerializeField] public string SubjectIconName { get; private set; }

    [field: Header("----- 캐릭터 특수효과 -----")]
    [field: SerializeField] public SO_AbilityData AbilityData { get; private set; }

    // =========== 캐릭터 스탯 ==============
    [Serializable]
    public struct STStats
    {
        public int maxHp;
        public int attackPower;
    }
}
