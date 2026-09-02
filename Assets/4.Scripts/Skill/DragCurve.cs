using UnityEngine;
using UnityEngine.UI;

public class DragCurve : MonoBehaviour
{
    [Header ("Sprites")]
    [SerializeField] private Image[] curveImages; 

    [Header ("Size Setting")]
    [SerializeField] private float _minSize;
    [SerializeField] private float _maxSize;
    [SerializeField] private float _arrowSize;

    // 베지에 곡선의 각 컨트롤 포인트
    [SerializeField] private RectTransform _p1;
    private RectTransform _p0;   
    private RectTransform _p2;

    private RectTransform _rect; 
    private bool _active;

    void Awake()
    {
        _rect = GetComponent<RectTransform> ();

        for (int i = 0; i < curveImages.Length - 1; i++)
        {
            float k = (float)i / (curveImages.Length - 1);
            curveImages[i].transform.localScale = Vector3.one * Mathf.Lerp (_minSize, _maxSize, k);
        }
        curveImages[curveImages.Length - 1].transform.localScale = Vector3.one * _arrowSize;

        SetVisible (false);
    }

    public void Begin(RectTransform startAnchor)
    {
        _p0 = startAnchor;
        _active = true;
        SetVisible (true);
    }

    public void End()
    {
        _active = false;
        SetVisible (false);
    }

    private void SetVisible(bool on)
    {
        if (curveImages == null) return;
        foreach (var img in curveImages)
        {
            if (img)
            {
                img.gameObject.SetActive (on);
            }
        }
    }

    void Update()
    {
        if (!_active || _p0 == null || _p1 == null || curveImages == null || curveImages.Length == 0)
        {
            return;
        }

        Vector2 p0 = _rect.InverseTransformPoint (_p0.position);
        Vector2 p1 = _rect.InverseTransformPoint (_p1.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle (_rect, Input.mousePosition, null, out Vector2 p2);

        int n = curveImages.Length;
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);   
            Vector2 pos = GetPoint (p0, p1, p2, t);

            var rt = curveImages[i].rectTransform;
            rt.anchoredPosition = pos;

            Vector2 tan = GetTangent (p0, p1, p2, t);
        
            float deg = Mathf.Atan2 (tan.y, tan.x) * Mathf.Rad2Deg - 90f; // Y축 위로 향해있는 스프라이트라 -90 뺴줌.
            rt.localRotation = Quaternion.Euler (0f, 0f, deg);
        }
    }

    private Vector2 GetPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return (u * u) * p0 + 2f * u * t * p1 + (t * t) * p2;
    }

    private static Vector2 GetTangent(in Vector2 p0, in Vector2 p1, in Vector2 p2, float t)
    {
        return 2f * ((1f - t) * (p1 - p0) + t * (p2 - p1));
    }
}
