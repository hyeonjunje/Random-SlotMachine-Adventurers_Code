using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TargetSelectUI : MonoBehaviour
{
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private Ease _animationEase = Ease.Linear;

    [SerializeField] private Image[] _imageTargets;
    [SerializeField] private float _targetActiveInterval = 0.1f;

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Sequence _sequence = null;
    private Coroutine _coUpdateUI = null;
    private bool _isOpening;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_isOpening == false)
        {
            HideImmediate();
        }
    }

    public void Open()
    {
        _isOpening = true;
        gameObject.SetActive(true);
        _isOpening = false;

        BattleSystem.Instance.ChangeBattleState(EBattleState.SelectTarget);

        _rect.anchoredPosition = Vector3.up * _rect.sizeDelta.y;
        _canvasGroup.alpha = 0;

        if(_sequence != null)
        {
            _sequence.Kill();
        }

        _sequence = DOTween.Sequence();
        _sequence.Join(_rect.DOAnchorPos(Vector3.up * -_rect.sizeDelta.y, _animationDuration))
            .Join(_canvasGroup.DOFade(1, _animationDuration))
            .SetEase(_animationEase);

        UpdateUI();
    }

    public void HideImmediate()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }

        if (_coUpdateUI != null)
        {
            StopCoroutine(_coUpdateUI);
            _coUpdateUI = null;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0;
        }

        gameObject.SetActive(false);
    }

    public void Close()
    {
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

    public void UpdateUI()
    {
        if(_coUpdateUI != null)
        {
            StopCoroutine(_coUpdateUI);
        }

        _coUpdateUI = StartCoroutine(CoUpdateUI());
    }

    private IEnumerator CoUpdateUI()
    {
        foreach (Image imageTarget in _imageTargets)
        {
            imageTarget.gameObject.SetActive(false);
        }

        for (int i = 0; i < CharacterSystem.Instance.Enemies.Count; ++i)
        {
            _imageTargets[i].gameObject.SetActive(true);
            _imageTargets[i].color = Color.gray;
        }

        for (int i = 0; i < BattleSystem.Instance.CurrentTargets.Count; ++i)
        {
            _imageTargets[i].color = Color.white;
            yield return new WaitForSeconds(_targetActiveInterval);
        }
    }
}
