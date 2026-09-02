using Spine.Unity;
using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator _animator;
    private SkeletonAnimation _skeletonAnimation = null;

    private readonly string SpineEventKeyName = "AnimationEvent";
    private readonly string AnimatorEventKeyName = "OnAnimMarker";

    public void Setup(Animator animator)
    {
        _animator = animator;
    }

    public void Setup(SkeletonAnimation skeletonAnimation)
    {
        _skeletonAnimation = skeletonAnimation;
    }

    // 애니메이션 재생
    public void PlayAnimation(ECharacterAnimationType characterAnimationType)
    {
        if (!IsValid())
        {
            return;
        }

        string animName = characterAnimationType.ToString();

        // 1. 목표 배속 계산: Idle이면 무조건 1.0f, 그 외에는 전투 배속 적용
        float targetSpeed = (characterAnimationType == ECharacterAnimationType.Idle)
            ? 1.0f
            : StyleManager.Instance.AnimationTimeData.SafeBattleTimeScale;

        // Animator 처리
        if (_animator != null)
        {
            _animator.speed = targetSpeed;
            _animator.Play(animName);
        }

        // SkeletonAnimation 처리
        if (_skeletonAnimation != null)
        {
            var mainTrackEntry = _skeletonAnimation.AnimationState.SetAnimation(0, animName, false);
            mainTrackEntry.TimeScale = targetSpeed;

            var idleTrackEntry = _skeletonAnimation.AnimationState.AddAnimation(0, ECharacterAnimationType.Idle.ToString(), true, 0f);
            idleTrackEntry.TimeScale = 1.0f;
        }
    }

    // 이벤트 트리거까지의 길이 반환
    public float GetTimeUntilEvent(ECharacterAnimationType animationType)
    {
        if (!IsValid()) return 0f;

        string animName = animationType.ToString();

        // 앞서 논의한 배속 룰 적용 (Idle은 1배속, 나머지는 전투 배속)
        float targetSpeed = (animationType == ECharacterAnimationType.Idle)
            ? 1.0f
            : StyleManager.Instance.AnimationTimeData.SafeBattleTimeScale;

        // 0으로 나누는 오류 방지용 안전장치
        float safeSpeed = Mathf.Max(0.001f, targetSpeed);

        float rawEventTime = 0f;
        bool isEventFound = false;

        // 1. spine용 검색
        if (_skeletonAnimation != null)
        {
            var skeletonData = _skeletonAnimation.SkeletonDataAsset.GetSkeletonData(true);
            var animation = skeletonData.FindAnimation(animName);

            if (animation != null)
            {
                foreach (var timeline in animation.Timelines)
                {
                    if (timeline is Spine.EventTimeline eventTimeline)
                    {
                        foreach (var ev in eventTimeline.Events)
                        {
                            if (ev.Data.Name == SpineEventKeyName)
                            {
                                rawEventTime = ev.Time;
                                isEventFound = true;
                                break;
                            }
                        }
                    }
                    if (isEventFound) break;
                }
            }
        }

        // 2. animator 용 검색 (스파인에서 못 찾았을 경우)
        if (!isEventFound && _animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animName)
                {
                    foreach (var ev in clip.events)
                    {
                        if (ev.functionName == AnimatorEventKeyName)
                        {
                            rawEventTime = ev.time;
                            isEventFound = true;
                            break;
                        }
                    }
                }
                if (isEventFound) break;
            }
        }

        // 최종적으로 원본 시간을 현재 배속으로 나눈 '실제 소요 시간' 반환
        return rawEventTime / safeSpeed;
    }

    private bool IsValid()
    {
        return _animator != null || _skeletonAnimation != null;
    }
}
