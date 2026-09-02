using UnityEngine;

[CreateAssetMenu(fileName = "SO_AnimationTimeData", menuName = "Scriptable Objects/SO_AnimationTimeData")]
public class SO_AnimationTimeData : ScriptableObject
{
    [Header("타임스케일")]
    [SerializeField] private float _timeScale = 1f;
    [SerializeField] private float _battleTimeScale = 1f;
    [SerializeField] private Vector2 _battleTimeScaleClamp = new Vector2(1f, 3f);

    [Header("전투 관련")]
    [SerializeField, Tooltip("캐릭터가 스폰될 때 애니메이션 시간")] private float _characterSpawnAnimationTime = 0.4f;
    [SerializeField, Tooltip("캐릭터가 죽을 때 애니메이션 시간")] private float _characterDeadAnimationTime = 0.4f;
    [SerializeField, Tooltip("슬롯머신에 키워드가 들어가는 애니메이션 시간")] private float _insertSlotMachineKeywordAnimationTime = 0.4f;
    [SerializeField, Tooltip("공격 애니메이션 시간")] private float _attackAnimationTime = 0.4f;
    [SerializeField, Tooltip("자동 전투 시 행동간의 시간")] private float _actIntervalTime = 0.4f;
    [SerializeField, Tooltip("턴 종료 대기 시간")] private float _turnEndDelayTime = 0.4f;
    [SerializeField, Tooltip("토큰이 생성되는 시간")] private float _appearTokenTime = 0.2f;
    [SerializeField, Tooltip("토큰이 사라지는 시간")] private float _disappearTokenTime = 0.2f;
    [SerializeField, Tooltip("슬롯머신이 다 돌아갔을 때 토큰이 하나씩 생성되는 시간")] private float _createTokenInterval = 0.3f;

    private float SafeTimeScale => _timeScale != 0 ? _timeScale : 0.000001f;
    public float SafeBattleTimeScale => Mathf.Clamp(_battleTimeScale, _battleTimeScaleClamp.x, _battleTimeScaleClamp.y);

    public float CharacterSpawnAnimationTime => _characterSpawnAnimationTime / SafeTimeScale / SafeBattleTimeScale;
    public float CharacterDeadAnimationTime => _characterDeadAnimationTime / SafeTimeScale / SafeBattleTimeScale;
    public float InsertSlotMachineKeywordAnimationTime => _insertSlotMachineKeywordAnimationTime / SafeTimeScale;
    public float AttackAnimationTime => _attackAnimationTime / SafeTimeScale / SafeBattleTimeScale;
    public float ActIntervalTime => _actIntervalTime / SafeTimeScale / SafeBattleTimeScale;
    public float TurnEndDelayTime => _turnEndDelayTime / SafeTimeScale / SafeBattleTimeScale;
    public float AppearTokenTime => _appearTokenTime / SafeTimeScale / SafeBattleTimeScale;
    public float DisappearTokenTime => _disappearTokenTime / SafeTimeScale / SafeBattleTimeScale;
    public float CreateTokenInterval => _createTokenInterval / SafeTimeScale / SafeBattleTimeScale;

    [Header("Multi Spin Delay")]
    [SerializeField, Tooltip("다중 스핀 연출 전 대기 시간")] private float _multiSpinDelayTime = 1.5f;
    public float MultiSpinDelayTime => _multiSpinDelayTime / SafeTimeScale / SafeBattleTimeScale;

    public void SetTimeScale(float timeScale)
    {
        _timeScale = timeScale;
    }

    public void SetBattleTimeScale(float timeScale)
    {
        _battleTimeScale = Mathf.Lerp(_battleTimeScaleClamp.x, _battleTimeScaleClamp.y, timeScale);
    }
}
