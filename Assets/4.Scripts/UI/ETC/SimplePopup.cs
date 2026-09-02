using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public enum EPopupButtonType
{
    None,
    One,
    Two,
}

public class SimplePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _textContents;
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private TMP_Text _leftButtonText;
    [SerializeField] private TMP_Text _rightButtonText;

    [SerializeField] private RectTransform _popupRect;
    [SerializeField] private CanvasGroup _canvasGroup;

    [SerializeField] private float _duration = 0.5f;          // 애니메이션 지속 시간
    [SerializeField] private Ease _scaleEase = Ease.OutBack;   // 스케일 Ease 타입 (원하는 느낌으로 변경 가능)
    [SerializeField] private Ease _fadeEase = Ease.Linear;    // 페이드 Ease 타입 (보통 Linear)

    private Tween _scaleTween;
    private Tween _fadeTween;

    private void OnEnable()
    {
        // 이전 애니메이션이 실행 중이라면 정리
        KillTweens();

        // 초기 상태 설정 (이미 UI에서 설정했다면 생략 가능하지만, 스크립트에서 명시하는 것이 좋음)
        _popupRect.localScale = Vector3.zero;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        // 크기 연출: 0에서 1로
        _scaleTween = _popupRect.DOScale(Vector3.one, _duration)
            .SetEase(_scaleEase)
            .OnComplete(() => {
                // 연출 완료 후 상호작용 가능하도록 설정
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            });

        // 알파 연출: 0에서 1로
        _fadeTween = _canvasGroup.DOFade(1f, _duration)
            .SetEase(_fadeEase);
    }

    public void Open(EPopupButtonType poupButtonType, string contents, string leftButtonText, string rightButtonText, Action onClickLeftButton, Action onClickRightButton)
    {
        gameObject.SetActive(true);

        _textContents.text = contents;

        switch (poupButtonType)
        {
            case EPopupButtonType.None:
                _leftButton.gameObject.SetActive(false);
                _rightButton.gameObject.SetActive(false);
                break;
            case EPopupButtonType.One:
                _leftButton.gameObject.SetActive(false);
                _rightButton.gameObject.SetActive(true);
                break;
            case EPopupButtonType.Two:
                _leftButton.gameObject.SetActive(true);
                _rightButton.gameObject.SetActive(true);
                break;
        }

        _leftButtonText.text = leftButtonText;
        _rightButtonText.text = rightButtonText;

        _leftButton.onClick.RemoveAllListeners();
        _rightButton.onClick.RemoveAllListeners();

        _leftButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            onClickLeftButton?.Invoke();
        });
        _rightButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            onClickRightButton?.Invoke();
        });
    }

    private void KillTweens()
    {
        _scaleTween?.Kill();
        _fadeTween?.Kill();
    }
}
