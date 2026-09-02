using UnityEngine;

public class EffectAnimationController : MonoBehaviour
{
    private Animator _animator;

    // 캐싱을 위해 Awake 사용
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        // 중복 반환 방지
        Creator.Instance.RemoveAsset(gameObject.name, gameObject);
    }
}
