using UnityEngine;

/// <summary>
/// SpriteRenderer 기반 간선 뷰 (UI가 아닌 월드 스페이스)
/// </summary>
public class ExpeditionEdgeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _lineWidth = 0.1f;

    /// <summary>
    /// 두 월드 좌표 사이에 선 그리기
    /// </summary>
    public void DrawLine(Vector3 startPos, Vector3 endPos)
    {
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;

        // 스프라이트 크기 조정 (가로: 거리, 세로: 두께)
        _spriteRenderer.size = new Vector2(distance, _lineWidth);

        // 중앙 위치로 이동
        transform.position = startPos + direction / 2f;

        // 방향에 맞게 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 투명도 설정
    /// </summary>
    public void SetTransparency(float alpha)
    {
        if (_spriteRenderer != null)
        {
            Color color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
        }
    }
}
