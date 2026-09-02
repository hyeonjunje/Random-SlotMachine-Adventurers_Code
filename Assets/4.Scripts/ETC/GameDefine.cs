using UnityEngine;

public static class GameDefine
{
    public const int MAX = 100000000; // 정수형 최대값
    public const float EPSILON = 0.0001f; // 적당히 작은 값

    public const int MAXPLAYERCOUNT = 3; // 최대 플레이어 개수
    public const int MAXCHANGEPLAYERCOUNT = 3; // 휴식방에서 최대로 교체할 수 있는 캐릭터풀의 개수

    public const int MAX_MANA = 3; // 최대 마나
    public const int MAX_LEVEL = 10; // 캐릭터 최대 레벨

    // Status Value
    public const float WEAKENING_VALUE = 0.25f;  // 약화 수치
    public const float MARKING_VALUE = 0.25f;  // 표식 수치
    public const float ELETRIC_VALUE = 0.25f;  // 감전 수치
    public const float COUNTERATTACK_VALUE = 0.5f;   // 반격 수치
    public const float PUNISHMENTATTACK_VALUE = 1f;   // 응징 수치
    public const float GUARDIAN_VALUE = 0.25f;   // 수호 수치
    public const float PRESERVATION_VALUE = 0.25f;   // 보존 수치

    // Layer
    public static int DefaultLayerIndex = LayerMask.NameToLayer("Default"); // 블러보다 위에 렌더링될 레이어
    public static int BlurAfterLayerIndex = LayerMask.NameToLayer("BlurAfter"); // 블러보다 위에 렌더링될 레이어
}
