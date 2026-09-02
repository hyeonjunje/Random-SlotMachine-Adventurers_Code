using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_PlayerData", menuName = "Scriptable Objects/SO_PlayerData")]
public class SO_PlayerData : SO_CharacterData
{
    [field: Header("----- Player -----")]
    [field: Header("----- 캐릭터 Preview 이미지 & 생성할 캐릭터 프리펩 -----")]
    [field: SerializeField] public GameObject CharacterSkeletonGraphic { get; private set; }
    [field: SerializeField] public string JobIconName { get; private set; }

    [field: Header("----- 캐릭터 ID & 표시 이름 & 가격 -----")]
    [field: SerializeField] public EPlayerJob PlayerJob { get; private set; }
    [field: SerializeField] public EKeyword SubjectKeyword { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }

    [field: Header("----- 캐릭터 선택 화면용 -----")]
    [field: SerializeField] public string IllustrationIconName { get; private set; }
    [field: SerializeField] public string IllustrationName { get; private set; }
    [field: SerializeField] public Vector2 SelectionIllustrationOffset { get; private set; }
    [field: SerializeField] public float SelectionIllustrationScale { get; private set; } = 1f;
    [field: SerializeField] public Vector2 SelectionBackgroundIllustrationOffset { get; private set; } = Vector2.zero;
    [field: SerializeField] public Vector2 LevelUpBackgroundIllustrationOffset { get; private set; } = Vector2.zero;
    [field: SerializeField] public string CharacterLore { get; private set; }
}
