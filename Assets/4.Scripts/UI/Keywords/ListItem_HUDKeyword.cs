using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_HUDKeyword : BaseListItem<SO_KeywordData>, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _imageBackground;
    [SerializeField] private TMP_Text _textKeywordName;
    [SerializeField] private Image _imageLock;
    [SerializeField] private Image _imageLockBacground;
    [SerializeField] private Button _button;

    [SerializeField] private CanvasGroup _canvasGroup;
    private bool _canShowPreview = true;

    public void SetListItem(SO_KeywordData keywordData, Action<EKeyword, ListItem_HUDKeyword> onClick = null, bool isLocked = false, bool canShowPreview = true)
    {
        base.SetListItem (keywordData);
        _canShowPreview = canShowPreview;
        if (keywordData == null) return;

        transform.DOKill ();
        transform.localScale = Vector3.one;
        _canvasGroup.alpha = 1f;

        gameObject.SetActive (true);
        _textKeywordName.text = LocalizationManager.Instance.Get(keywordData.KeywordName);
        _imageBackground.color = Utils.GetKeywordColor (keywordData.KeywordType);

        if (_imageLock != null)
        {
            _imageLock.gameObject.SetActive (isLocked);
            _imageLockBacground.gameObject.SetActive (isLocked);
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners ();
            if (onClick != null && !isLocked)
            {
                _button.interactable = true;
                _button.onClick.AddListener (() => onClick.Invoke (keywordData.Keyword, this));
            }
            else
            {
                _button.interactable = false;
            }
        }
    }

    public override void SetListItem(SO_KeywordData keywordData)
    {
        SetListItem (keywordData, null, false);
    }

    public void PlayDeleteAnimation(Action onComplete)
    {
        if (_button != null) _button.interactable = false;

        Sequence seq = DOTween.Sequence ();

        seq.Append (transform.DOShakePosition (0.2f, 8f, 20));
        seq.Append (transform.DOScale (0f, 0.4f).SetEase (Ease.InBack));
        seq.Join (_canvasGroup.DOFade (0f, 0.4f));
        seq.OnComplete (() => onComplete?.Invoke ());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_canShowPreview == false) return;

        UIManager.Instance.ShowKeywordCardPreview(Item, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_canShowPreview == false) return;

        UIManager.Instance.HideKeywordCardPreview();
    }

    private void OnDisable()
    {
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.HideKeywordCardPreview();
        }
    }
}
