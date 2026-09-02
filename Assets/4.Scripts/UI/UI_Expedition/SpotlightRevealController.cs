using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class SpotlightRevealController : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] private Image _overlayImage;
    [SerializeField] private RectTransform _rectSpotImage;

    [Header("픽셀 단위")]
    [SerializeField] private float _radiusPx = 140f;
    [SerializeField] private float _featherPx = 28f;
    [SerializeField, Range(0f, 1f)] private float _opacity = 0.85f;

    [Header("정렬 보정")]
    [SerializeField] private Vector2 _offsetPx = Vector2.zero;

    [Header("테스트")]
    [SerializeField] private bool _isTest = false;

    private Material _runtimeMat;

    void Awake()
    {
        // 공유 머티리얼을 직접 수정하지 않도록 인스턴스 생성
        _runtimeMat = Instantiate(_overlayImage.material);
        _overlayImage.material = _runtimeMat;

        // UI Image의 color 알파는 1로 (이중 투명도 방지)
        var c = _overlayImage.color;
        _overlayImage.color = new Color(c.r, c.g, c.b, 1f);

        ApplyConfig();
    }

    void Update()
    {
        if (_isTest)
        {
            return;
        }

        Vector2 pointerPx = Input.mousePosition;

        if (_rectSpotImage != null)
        {
            _rectSpotImage.anchoredPosition = pointerPx;
        }

        _runtimeMat.SetVector("_Pointer", new Vector4(pointerPx.x, pointerPx.y, 0f, 0f));
        _runtimeMat.SetVector("_Offset", new Vector4(_offsetPx.x, _offsetPx.y, 0f, 0f));
    }

    private void ApplyConfig()
    {
        _runtimeMat.SetFloat("_Radius", Mathf.Max(0f, _radiusPx));
        _runtimeMat.SetFloat("_Feather", Mathf.Max(0f, _featherPx));
        _runtimeMat.SetFloat("_Opacity", Mathf.Clamp01(_opacity));

        // 머티리얼 _Color의 알파는 1로(색만 쓰고 투명도는 _Opacity로 통일)
        var col = _runtimeMat.GetColor("_Color");
        col.a = 1f;
        _runtimeMat.SetColor("_Color", col);
    }
}
