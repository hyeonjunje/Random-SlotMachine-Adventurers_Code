using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InvertedMaskTransition : MonoBehaviour
{
    [SerializeField] private RectTransform _rectMask;
    [SerializeField] private Image _imageBackground;
    [SerializeField] private Image _raycastFilter;

    [SerializeField] private Vector3 _middleSize = Vector3.one * 100f;
    [SerializeField] private float _fadeDuration1;
    [SerializeField] private float _fadeDuration2;

    [SerializeField] private Ease _easeFadeOut;
    [SerializeField] private Ease _easeFadeIn;

    public float AnimationDuration => _fadeDuration1 + _fadeDuration2;

    private Vector3 _initSizeDelta;
    private Image _imageMask;

    private void Awake()
    {
        _initSizeDelta = _rectMask.sizeDelta;
        _imageMask = _rectMask.GetComponent<Image>();
    }

    private void Update()
    {
        if(AppConfig.IsCheatEnabled && Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(FadeOut());
        }
        if (AppConfig.IsCheatEnabled && Input.GetKeyDown(KeyCode.B))
        {
            StartCoroutine(FadeIn());
        }
    }

    public IEnumerator FadeOut()
    {
        _raycastFilter.gameObject.SetActive(true);

        Sequence sequence = DOTween.Sequence();

        sequence.OnStart(() =>
            {
                _rectMask.gameObject.SetActive(true);
                _rectMask.sizeDelta = _initSizeDelta;
            })
            .Append(_rectMask.DOSizeDelta(_middleSize, _fadeDuration1))
            .Append(_rectMask.DOSizeDelta(Vector3.zero, _fadeDuration2).SetEase(_easeFadeOut));

        yield return sequence.WaitForCompletion();
    }

    public IEnumerator FadeIn()
    {
        Sequence sequence = DOTween.Sequence();

        sequence.OnStart(() => { _rectMask.sizeDelta = Vector3.zero; })
            .Append(_rectMask.DOSizeDelta(_middleSize, _fadeDuration2).SetEase(_easeFadeIn))
            .Append(_rectMask.DOSizeDelta(_initSizeDelta, _fadeDuration1))
            .OnComplete(() => _rectMask.gameObject.SetActive(false));

        yield return sequence.WaitForCompletion();

        _raycastFilter.gameObject.SetActive(false);
    }
}
