using UnityEngine;

public class MapEdgeUI : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private float _lineWidth = 8f;

    /// 두 UI 위치(시작 anchoredPosition, 끝 anchoredPosition) 사이에 선을 그립니다.
    public void DrawLine(Vector2 startPos, Vector2 endPos)
    {
        Vector2 direction = endPos - startPos;
        float distance = direction.magnitude;

        _rectTransform.sizeDelta = new Vector2(distance, _lineWidth);

        _rectTransform.anchoredPosition = startPos + direction / 2f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
