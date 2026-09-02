#region Audio
using System;

public enum EBgmId
{
    None = 0,
    Title = 1,
    CharacterSelect = 2,
    Tutorial = 3,
    Battle = 11,
    Elite = 12,
    Boss = 13,
    Rest = 14,
    Event = 15,
    Shop = 16,
    Treasure = 17,

    Stage1 = 21,
    Stage2 = 22,
    Stage3 = 23,
}

public enum ESfxId
{
    SlotMachineSpin = 1,   // 일반 슬롯머신이 돌아갈 때 틱 소리
    SlotMachineReroll = 2, // 일반 슬롯머신 리롤 누르면 나는 소리
    SlotMachineComplete = 3, // 일반 슬롯머신 문장 완성

    TitleSlotMachineSpin = 11,   // 타이틀 슬롯머신이 돌아갈 때 틱 소리
    TitleSlotMachineReroll = 12, // 타이틀 슬롯머신 리롤 누르면 나는 소리
    TitleSlotMachineComplete = 13, // 타이틀 슬롯머신 문장 완성

    EventSlotMachineSpin = 21,   // 이벤트 슬롯머신이 돌아갈 때 틱 소리
    EventSlotMachineReroll = 22, // 이벤트 슬롯머신 리롤 누르면 나는 소리
    EventSlotMachineComplete = 23, // 이벤트 슬롯머신 문장 완성

    Gain_Money = 31, // 돈획득

    Rest = 51, // 휴식섬_휴식

    // UI (101~)
    UI_Click1 = 101,
    UI_Click2 = 102,
    UI_Click3 = 103,

    // 전투 (201~)
    Warrior_DefaultAttack1 = 201,
    Warrior_DefaultAttack2,
    Dwarf_DefaultAttack1,
    Dwarf_DefaultAttack2,
    Archer_DefaultAttack1,
    Archer_DefaultAttack2,
    Priest_DefaultAttack1,
    Priest_DefaultAttack2,
    Rogue_DefaultAttack1,
    Rogue_DefaultAttack2,

    Buff1 = 231,
    Buff2,

    Hit = 241,

    Guard = 251,

    NormalBattle_Victory = 261,

    // 상점 (301~)
    Buy_Goods = 301,
    LackOfMoney = 302,
}
#endregion

#region ActionSystem
public enum EReactionTiming
{
    Pre,
    Post
}
#endregion

#region Character
public enum EPlayerJob
{
    Any = -2,
    None = -1,
    Warrior = 0,
    Dwarf,
    Archer,
    Priest,
    Rogue,
}

public enum EEnemyId
{
    Slime = 1,
    Slime_Blue,
    Flower,
    Flower_Pink,
    Golem,
    Mushroom,
    Mushroom_Posion,
    Wolf,
    Golem_Dark,
    KingSlime,
}

// ĳ������ �ִϸ��̼� Ÿ�� (�ִϸ������� �ִϸ��̼� �̸��� ���ƾ��մϴ�.)
public enum ECharacterAnimationType
{
    Idle,
    Attack,
    Hit,
    Buff,
}

public enum EEnemyActType
{
    None,
    Attack,
    Defense,
    Special,
    Buff,
    Debuff,
    AttackAndBuff,
    AttackAndDeBuff,
    DefenseAndBuff,
    DefenseAndDeBuff,
    AttackAndDefense,
    SpecialAndBuff,
    SpecialAndDeBuff,
}
#endregion

#region UI
public enum EUIType
{
    None,
    UI_SlotMachine,
    UI_CharacterStore,
    UI_SkillCard,
    UI_Battle,
    UI_Expedition,
    UI_Map,
    UI_Store,
    UI_MainHud,
    UI_Rest,
    UI_SelectCharacter,
    UI_Event,
    UI_MyKeywords,
    UI_Treasure,
    UI_Reward,
    UI_LevelUpArtifactSelect,
    UI_KeywordUpgrade,
    UI_Pause,
    UI_Title,
    UI_Settings,
    UI_Intro,
    UI_Ending,
    UI_Tutorial,
    UI_SelectionContext = 99, 
}

public enum EMessageType
{
    Notice,
    Warning,
}
#endregion

#region Stats
public enum EStatType
{
    MaxHp,
    AttackPower,
    AttackSpeed,
    MaxMana,
}

public enum EStatModType
{
    Add,     // ���ϱ�
    Mul,     // ���ϱ�
    FinalMul // ���� ���ϱ�
}
#endregion

#region Status
public enum EStatusType
{
    Poison,           // <중독: 턴 시작 시 정해진 수치만큼 데미지> [디버프]
    Weakening,        // <약화: 이번 1회 동안 가하는 피해량 25% 감소> [디버프]
    Marking,          // <표식: 이번 1회 동안 받는 피해량 25% 증가> [디버프]
    Electric,         // <감전: 피격 시, 받은 데미지의 25%를 공격자에게 반사> [디버프]
    CounterAttack,    // <반격: 피격 시, 내 공격력의 [50%]만큼 공격자에게 데미지> [버프]
    PunishmentAttack, // <응징: 피격 시, 내 공격력의 [100%]만큼 공격자에게 데미지> [버프]
    Evasion,          // <회피: 이번 턴에 받는 첫 피해를 [100%] 무시> [버프]
    Guardian,         // <수호: 받는 피해량 [25%] 감소> [버프]
    Preservation,     // <보존: 턴 종료 시 남은 쉴드의 [25%]가 다음 턴으로 유지됨> [버프]

    Prey,             // <사냥감: 받는 데미지 [25%] 증가, 해당 적 처치 시 체력 회복> [디버프]
    Paralysis,        // <마비: 행동 불가> [디버프]

    Exhaustion,       // <탈진: N턴간 최대마나 1감소> [디버프]
    Frost,
    Anger,            // <분노: 매 턴 시작 시 영구적인 공격력 N 상승> [버프]
    Max,
}

public enum EStatusCategory
{
    Buff,
    Debuff,
    Passive,
}
#endregion

#region Skill
public enum EAdverbAdjustTiming
{
    None,
    Start,
    End,
    Reroll,
    Clicked,
}

[Flags]
public enum EAdverbEffectTargetType
{
    None = 1 << 0,
    Skill = 1 << 1,
    DealDamage = 1 << 2,
    AddShield = 1 << 3,
    ApplyHealing = 1 << 4,
}

public enum ECardRank
{
    Bronze = 0,
    Silver,
    Gold,
    Platinum,
    Rainbow,
}
#endregion

#region Battle
public enum EBattleState
{
    NonBattle,    // 전투 중이 아닌 상태
    StartBattle,  // 전투 시작
    StartTurn,    // 턴 시작 (이전 애니메이션 완료 후 진행)
    SlotMachine,  // 슬롯머신 돌아가는 중
    SelectTarget, // 타겟 선택 중
    InAutoBattle, // 자동 전투 진행 중
    ClearBattle,  // 전투 클리어 (승리) 상태
}

public enum EBattleActType
{
    ProgressTurn, // 턴 진행
    EndTurn,      // 턴 종료
    EndBattle,    // 전투 종료
}

public enum EBattleSideType
{
    OurSide,    // 아군
    EnemySide,  // 적군
    Neutrality  // 중립
}

public enum EDamageFormulaType
{
    Flat = 0,                                     // 고정 수치 데미지
    AddPercentForAttackPower = 2,                 // 내 공격력의 N% 만큼 더해서 데미지
    SetPercentForAttackPower = 3,                 // 내 공격력의 N% 로 데미지 설정

    AddPercentForAttackPowerWhenTargetHpFull = 4, // 타겟 체력이 최대일 때 내 공격력 비례 추가 피해

    BeforeAttackDamage = 5,                       // 가하기 전 피해량 비례 데미지
    BeforeAttackedDamage = 6,                     // 받기 전 피해량 비례 데미지

    AddPercentForAttackPowerAddTargetActCount = 7,// 내 공격력 + 타겟 행동 카운트의 N% 추가 피해

    PercentOfMaxHP = 10,                          // 타겟 최대 체력의 % 데미지
    PercentOfCurrentHP = 11,                      // 타겟 현재 체력의 % 데미지
    PercentOfMissingHP = 12,                      // 타겟 잃은 체력의 % 데미지
}

public enum EHealingFormulaType
{
    Flat = 0,                                     // 고정 수치 회복
    AddPercentForAttackPower = 2,                 // 내 공격력의 N% 만큼 더해서 회복
    SetPercentForAttackPower = 3,                 // 내 공격력의 N% 만큼 회복

    BeforeAttackDamage = 4,                       // 가하기 전 피해량 비례 회복
    BeforeAttackedDamage = 5,                     // 받기 전 피해량 비례 회복
    PercentOfMaxHP = 6,
}

public enum EShieldFormulaType
{
    Flat = 0,                                     // 고정 수치 쉴드
    AddPercentForAttackPower = 2,                 // 내 공격력의 N% 만큼 더해서 쉴드
    SetPercentForAttackPower = 3,                 // 내 공격력의 N% 만큼 쉴드

    BeforeAttackDamage = 4,                       // 가하기 전 피해량 비례 쉴드
    BeforeAttackedDamage = 5,                     // 받기 전 피해량 비례 쉴드
}
#endregion

#region Event
public enum EEventRiskRewardType
{
    RiskHighRewardHigh, // 하이 리스크 & 하이 리턴
    RiskNoneRewardLow,  // 로우 리스크 & 로우 리턴
    RiskHighRewardNone, // 하이 리스크 & 노 리턴
}

public enum EMiniGameType
{
    None,
    StartingSlotMachine, // 시작 슬롯머신
}
#endregion

#region SlotMachine
public enum ESlotMachineRerollKeywordType
{
    Cross,  // 십자(가로/세로) 리롤
    Entire, // 전체 리롤
}
#endregion

#region Map
// 맵 노드의 종류
public enum EMapNodeType
{
    Monster,    // 일반 몬스터 전투
    Elite,      // 엘리트 몬스터 전투
    Rest,       // 휴식처 (모닥불 등)
    Shop,       // 상점
    Event,      // 물음표 이벤트
    Treasure,   // 보물 상자
    Boss,       // 보스 전투
    Start,      // 시작 지점
    None,       // 아무것도 아님 (빈 노드)
}
// 맵 노드의 현재 상태
public enum EMapNodeState
{
    Locked,     // 잠김 (아직 방문 불가)
    Available,  // 방문 가능 (현재 위치에서 이동 가능한 노드)
    Visited     // 방문 완료
}
#endregion

#region CreatorAsset
public enum ECreatorAsset
{
    PlayerView,
    EnemyView,
    GhostView,
    DamageTextUI,
}
#endregion

#region CameraAction
public enum ECameraActionType
{
    None,
    BlurZoomIn,
    BlurZoomOut,
    PlayerAttack,
    EnemyAttack,
    JustCameraShakeInBattle,
}
#endregion

#region Artifact
public enum EArtifactId
{
    \uB300\uAC01\uC120,
    \uACE0\uB798\uC758\uB208,
    \uADE4,
    \uB124\uC78E\uD074\uB85C\uBC84,
    \uC120\uBD09\uAE43\uBC1C,
    \uBC14\uB2E4\uADF8\uBB3C,
    \uB3D9\uC804\uB354\uBBF8,
    \uBA54\uBAA8\uC7A5,
    \uC804\uD22C\uAD50\uBCF8,
    \uD68C\uC804\uB3C5,
    \uB3C5\uC131\uC570\uD50C,
    \uAC00\uC2DC\uBC29\uD328,
    \uAC15\uCCA0\uBC29\uD328,
    \uC5BC\uC74C\uB098\uCE68\uBC18,
    \uC11C\uB9AC\uC871\uC1C4,
    \uC11C\uB9AC\uD30C\uD3B8,
    \uBCF4\uB514\uAC00\uB4DC\uBC43\uC9C0,
    \uD604\uC0C1\uC218\uBC30\uC9C0,
    \uADF8\uB9BC\uC790\uB2E8\uAC80,
    \uD669\uAE08\uD0D0\uC9C0\uAE30,
    \uBCF4\uB108\uC2A4\uCF54\uC778,
    \uB3C5\uAC1C\uAD6C\uB9AC\uC7A5\uAC11,
    \uAD6C\uC6D0\uC758\uBB3C\uC57D,
    \uBBF8\uC2A4\uB9B4\uB4DC\uB9B4,
    \uAC00\uC8FD\uAC11\uC637,
    \uACE0\uAE09\uCE68\uB0AD,
    \uC5F4\uBC88\uC9F8\uB9DD\uCE58,
    \uC815\uD654\uC758\uD5A5\uB85C,
    \uAC00\uC2DC\uB9DD\uD1A0,
    \uC0AC\uB0E5\uAFBC\uACE0\uAE00,
    \uD589\uC6B4\uC758\uD1B1\uB2C8\uBC14\uD034,
    \uD669\uAE08\uC800\uC6B8,
    \uC18C\uAE08\uC8FC\uBA38\uB2C8,
    \uD54F\uBE5B\uBCA8\uD2B8,
    \uC6A9\uC0AC\uC758\uD6C8\uC7A5,
    \uACE0\uBC30\uC728\uD655\uB300\uACBD,
    \uB9C8\uB098\uC99D\uB958\uAE30,
    \uC6B0\uC815\uBC18\uC9C0,
    \uAE68\uC9C4\uB3C5\uBCD1,
    \uBC14\uB78C\uAC1C\uBE44,
    \uCC22\uC5B4\uC9C4\uBD80\uC801,
    \uC804\uB9AC\uD488\uC8FC\uBA38\uB2C8,
    \uBAA8\uB798\uC2DC\uACC4,
    \uD669\uAE08\uC7A5\uBD80,
    \uB9F9\uB3C5\uCF54\uD305\uC561,
    \uB2E8\uBC31\uC9C8\uC250\uC774\uD06C,
    \uC57D\uC810\uB3C4\uAC10,
    \uD53C\uC758\uACC4\uC57D\uC11C,
    \uC131\uC790\uC758\uC720\uACE8\uD568,
    \uAC74\uC804\uC9C0,
    \uB9C8\uBC95\uC22B\uB3CC,
    \uBD09\uC81C\uD0A4\uD2B8,
    \uBC84\uADF8\uB9AC\uD3EC\uD2B8,
    \uC131\uC7A5\uCD09\uC9C4\uC81C,
    \uB2E8\uC5B4\uC7A5,
    \uB0A1\uC740\uAE30\uB3C4\uC0C1,
    \uAE08\uAC04\uB2EC\uAC40,
    \uC720\uB9AC\uC2EC\uC7A5,
    \uAE30\uC0C1\uB098\uD314,
    \uB9CC\uB144\uD544,
    \uC885\uC774\uCE7C,
    \uBBF8\uC815_\uC721\uD134\uD589\uB3D9\uC99D\uAC00,
    \uACE0\uC7A5\uB09C\uD0A4\uBCF4\uB4DC,
    \uBBF8\uC815_\uB9AC\uB864\uBD88\uAC00\uD1A0\uD070\uBC30\uAC00,
    \uAD6C\uBA4D\uB09C\uC8FC\uBA38\uB2C8,
    \uC11C\uB9AC\uC7A5\uAC11,
    \uC800\uAE08\uD1B5,
    \uBE68\uAC04\uBAA8\uC790,
    \uBE44\uB217\uBC29\uC6B8,
    \uB3C5\uAC11\uC637,
    \uBE44\uB2E8\uBC29\uC11D,
    \uAC00\uC8FD\uC9C0\uAC11,
    \uC5D0\uB108\uC9C0\uB4DC\uB9C1\uD06C,
    \uB178\uB780\uC6B0\uC0B0,
    \uC5BC\uC74C\uC8FC\uBA38\uB2C8,
    \uC120\uD48D\uAE30,
    \uB098\uBE44\uB125\uD0C0\uC774,
    \uB3C4\uC2DC\uB77D,
    \uC591\uCD08,
    \uBD89\uC740\uB9DD\uCE58,
    \uB098\uCE68\uBC18,
    \uD638\uB8E8\uB77C\uAE30,
    \uC131\uC790\uC758\uC131\uBC30,
    \uC601\uD63C\uC758\uAE30\uB3C4\uBB38,
    \uCC9C\uC0AC\uC758\uAE43\uD39C,
    \uB3C4\uC801\uC7A5\uAC11,
    \uD22C\uC9C0\uC758\uC7A5\uAC11,
    \uCC98\uD615\uC758\uB3C4\uB07C,
    \uC2E0\uC758\uD654\uC0B4,
    \uC0AC\uB0E5\uAC08\uACE0\uB9AC,
    \uC815\uBC00\uD55C\uC870\uC900\uACBD,
    \uB9DD\uCE58\uC640\uBAA8\uB8E8,
    \uC57C\uBC14\uC704\uAFBC\uC758\uCEF5,
    \uB3C5\uC774\uBE68,
    \uAC70\uC778\uC758\uCD94,
    \uD3EC\uD6A8\uC758\uB098\uD314,
    \uB3C4\uBC15\uC0AC\uC758\uCE69,
    \uB4B7\uAC70\uB798\uC7A5\uBD80,
    \uB3C5\uD55C\uB9E5\uC8FC,
    \uB4A4\uC9D1\uC5B4\uC9C4\uC11C\uC57D,
    \uC18D\uC8C4\uC758\uC131\uBC30,
    \uC5C7\uAC08\uB9B0\uD654\uC0B4\uCD09,
    \uB9DD\uC6D0\uACBD,
    \uB3C5\uD45C\uCC3D,
    \uC7A5\uC778\uC758\uBAA8\uB8E8,
    \uAC1C\uCC99\uC790\uC758\uAE43\uBC1C,
    \uB3C4\uBC15\uC0AC\uC758\uC131\uBC30,
    \uC804\uC7C1\uC758\uC804\uB9AC\uD488,
    \uC78A\uD600\uC9C4\uAE30\uB85D\uC11C,
    \uC218\uD589\uC790\uC758\uBB35\uC8FC,
    \uD669\uAE08\uC5F0\uAE08\uC220\uC7A5\uCE58,
    \uC6B4\uBA85\uC758\uC218\uB808\uBC14\uD034,
    \uD604\uC790\uC758\uC800\uC6B8,
    \uB2EC\uAD6C\uC5B4\uC9C4\uB9DD\uCE58,
    \uC624\uB798\uB41C\uB098\uCE68\uBC18,
    \uCCAD\uACB0\uD55C\uBD95\uB300,
    \uC218\uC9D1\uAC00\uC758\uAC00\uBC29,
    \uC2AC\uB86F\uB808\uBC84,
    \uB0A1\uC740\uB4DC\uB77C\uC774\uBC84,
    \uC774\uC911\uC2AC\uB86F,
    \uD53C\uC758\uC2AC\uB86F,
    \uBD80\uC11C\uC9C4\uB3D9\uC804,
    \uC624\uC5FC\uB41C\uBB3C\uC57D,
    \uC885\uC774\uBC29\uD328,
    \uCC22\uC5B4\uC9C4\uAD50\uBCF8,
    \uAC80\uC740\uC18C\uAE08,
    \uAC80\uC740\uC0C1\uC790,
    \uAE30\uB3C4\uBB38,
    \uD589\uC6B4\uBB3C\uC57D,
    \uAC00\uC18D\uCFE0\uD0A4,
    \uB3C4\uC0B4\uC790\uC758\uC778\uC7A5,
    \uAE30\uAD34\uD55C\uAC00\uBA74,
    \uC800\uC8FC\uC758\uBCF4\uC11D,
    \uC2A4\uD14C\uB85C\uC774\uB4DC,
    \uC6B0\uB300\uAD8C,
    \uD589\uC6B4\uB098\uCE68\uBC18,
}
public enum EArtifactGameModelFloatStat
{
    CounterAttackValue,
    SuccessProbability,
    GreatSuccessProbability,
    UltraSuccessProbability,
    FailureProbability,
}

public enum EArtifactGameModelIntStat
{
    KeywordUpgradeOptionCount,
}

[Flags]
public enum EArtifactPool
{
    None = 0,
    Starter = 1 << 0,
    Special = 1 << 1,
    LevelUp = 1 << 3,
}

#endregion

#region ETC
public enum EChangeType
{
    Add,      // 더하기
    Subtract, // 빼기
    Set,      // 값 고정(Set)하기
}
#endregion
