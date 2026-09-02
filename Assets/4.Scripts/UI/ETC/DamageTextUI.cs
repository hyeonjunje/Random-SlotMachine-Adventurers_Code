using TMPro;
using UnityEngine;

public class DamageTextUI : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float initialYVelocity = 5f; // 위로 솟구치는 힘
    [SerializeField] private float gravity = 9.8f;        // 떨어지는 중력 가속도
    [SerializeField] private float xSpread = 2f;          // 좌우로 퍼지는 범위
    [SerializeField] private float fadeDuration = 0.5f;   // 사라지는 시간

    private TMP_Text _textComponent;
    private Color _originalColor;
    private Vector3 _velocity;
    private float _timer;

    private void Awake()
    {
        _textComponent = GetComponent<TMP_Text>();
    }

    // 초기화 메서드 (풀에서 꺼낼 때 호출)
    public void Initialize(int damageAmount, Vector3 startPos, Color color)
    {
        transform.position = startPos;
        _textComponent.text = damageAmount.ToString();
        _textComponent.color = color;

        // 초기 운동량 설정 (위쪽 + 랜덤한 좌우)
        float randomX = UnityEngine.Random.Range(-xSpread, xSpread);
        _velocity = new Vector3(randomX, initialYVelocity, 0);

        _timer = 0f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. 물리 이동 (위치 = 현재위치 + 속도 * 시간)
        transform.position += _velocity * dt;

        // 2. 중력 적용 (속도 = 현재속도 - 중력 * 시간)
        _velocity.y -= gravity * dt;

        // 3. 페이드 아웃 및 반환 처리
        // (낙하 시작 후 일정 시간이 지나거나, 특정 높이 이하로 떨어질 때 등 조건은 자유)
        // 여기서는 간단히 투명해지면 반환하는 로직으로 작성
        _velocity.x = Mathf.Lerp(_velocity.x, 0, dt); // 공기 저항처럼 X축 속도 감속 (선택사항)

        if (_velocity.y < 0) // 떨어지기 시작하면 페이드 아웃 시작 등 디테일 추가 가능
        {
            _timer += dt;
            float alpha = Mathf.Lerp(1, 0, _timer / fadeDuration);
            _textComponent.color = new Color(_textComponent.color.r, _textComponent.color.g, _textComponent.color.b, alpha);

            if (alpha <= 0.01f)
            {
                ReturnToPool();
            }
        }
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);
    }
}
