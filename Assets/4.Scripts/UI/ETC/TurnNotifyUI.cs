using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class TurnNotifyUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _textTurn;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _endPoint;

    [SerializeField] private float _appearDuration;
    [SerializeField] private Ease _appearEase;
    [SerializeField] private float _disappearDuration;
    [SerializeField] private Ease _disappearEase;

    public IEnumerator StartBattleNotify()
    {
        gameObject.SetActive(true);
        _textTurn.text = LocalizationManager.Instance.Get("CS_TURNNOTIFYUI_070");

        Sequence seq = DOTween.Sequence();
        seq.OnStart(() =>
        {
            _canvasGroup.alpha = 1f;
            _textTurn.rectTransform.anchoredPosition = Vector3.zero;
            _textTurn.rectTransform.localScale = Vector3.one * 3.5f;
        });
        seq.Append(_textTurn.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.6f);
        seq.Append(_canvasGroup.DOFade(0f, 0.4f));

        yield return seq.WaitForCompletion();
        gameObject.SetActive(false);
    }

    public IEnumerator PlayTurnNotify(int turn)
    {
        _canvasGroup.alpha = 0;
        _textTurn.text = string.Format(LocalizationManager.Instance.Get("CS_TURNNOTIFYUI_071"), turn);
        _textTurn.transform.localPosition = _startPoint.localPosition;

        yield return null;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(_textTurn.transform.DOLocalMoveX(0f, _appearDuration).SetEase(_appearEase))
            .Join(_canvasGroup.DOFade(1f, _appearDuration).SetEase(_appearEase))
            .Append(_textTurn.transform.DOLocalMoveX(_endPoint.localPosition.x, _disappearDuration).SetEase(_disappearEase))
            .Join(_canvasGroup.DOFade(0f, _disappearDuration).SetEase(_disappearEase));

        yield return sequence.WaitForCompletion();

        gameObject.SetActive(false);
    }
}

