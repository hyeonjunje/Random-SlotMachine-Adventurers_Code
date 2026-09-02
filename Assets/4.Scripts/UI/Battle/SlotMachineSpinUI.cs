using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineSpinUI : MonoBehaviour
{
    [SerializeField] private Image _imageClick;
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private Ease _animationEase = Ease.Linear;

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Sequence _sequence = null;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        _imageClick.raycastTarget = true;

        _rect.anchoredPosition = Vector3.up * _rect.sizeDelta.y;
        _canvasGroup.alpha = 0;

        if (_sequence != null)
        {
            _sequence.Kill();
        }

        _sequence = DOTween.Sequence();
        _sequence.Join(_rect.DOAnchorPos(Vector3.up * -_rect.sizeDelta.y, _animationDuration))
            .Join(_canvasGroup.DOFade(1, _animationDuration))
            .SetEase(_animationEase);
    }

    public void Close()
    {
        _imageClick.raycastTarget = false;

        if (_sequence != null)
        {
            _sequence.Kill();
        }

        _sequence = DOTween.Sequence();
        _sequence.Join(_canvasGroup.DOFade(0, _animationDuration))
            .SetEase(_animationEase)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }
}
