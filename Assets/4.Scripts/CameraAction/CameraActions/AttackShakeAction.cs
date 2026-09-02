using DG.Tweening;
using UnityEngine;

public class AttackShakeAction : CameraAction
{
    [SerializeField] private ECameraActionType _cameraActionType;
    [SerializeField] private bool _isDirection = true;

    [Header("Punch Shake Settings")]
    [SerializeField] private Vector3 _direction = Vector3.right;
    [SerializeField] private float _shakeDuration = 0.3f;     // 흔들리는 총 시간
    [SerializeField] private float _shakeStrength = 0.5f;     // 흔들림의 강도 (이동 거리)
    [SerializeField] private int _vibrato = 10;               // 진동 횟수
    [SerializeField] private float _elasticity = 1f;          // 탄성 (1에 가까울수록 통통 튀고, 0에 가까울수록 뻣뻣함)

    public override ECameraActionType CameraActionType => _cameraActionType;

    public override Tween Action()
    {
        Camera cameraMain = Camera.main;

        Sequence seq = DOTween.Sequence();

        if(SettingsManager.Instance.ScreenShake == false)
        {
            return seq;
        }

        if(_isDirection)
        {
            seq.Append(
                cameraMain.transform.DOPunchPosition(
                    punch: _direction * _shakeStrength,
                    duration: _shakeDuration / StyleManager.Instance.AnimationTimeData.SafeBattleTimeScale,
                    vibrato: _vibrato,
                    elasticity: _elasticity
                )
            );
        }
        else
        {
            Vector3 shakeVector = new Vector3(_shakeStrength, _shakeStrength, 0f);

            seq.Append(
                cameraMain.transform.DOShakePosition(
                    duration: _shakeDuration / StyleManager.Instance.AnimationTimeData.SafeBattleTimeScale,
                    strength: shakeVector,
                    vibrato: _vibrato,
                    randomness: 90f, // 무작위 방향성의 각도 (기본값 90 권장)
                    snapping: false,
                    fadeOut: true    // true로 설정하면 진동이 서서히 줄어들며 자연스럽게 멈춥니다.
                )
            );
        }

        return seq;
    }
}
