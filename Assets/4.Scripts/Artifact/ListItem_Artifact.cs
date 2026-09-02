using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ListItem_Artifact : BaseListItem<Artifact>, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color TriggerFlashColor = new Color(1f, 0.92f, 0.35f, 1f);

    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _flashImage;
    public Artifact artifact { get; private set; }

    public override void SetListItem(Artifact item)
    {
        base.SetListItem (item);
        gameObject.SetActive (true);
        artifact = item;

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity; 

        if (item != null && item.Data != null)
        {
            _iconImage.sprite = item.Data.Icon;
            _iconImage.color = Color.white;
            ResetFlashImage(item.Data.Icon);
        }
    }

    public void PlayTriggerEffect()
    {
        transform.DOKill();
        if (_iconImage != null)
        {
            _iconImage.DOKill();
        }

        if (_flashImage != null)
        {
            _flashImage.DOKill();
            _flashImage.transform.DOKill();
        }

        transform.localScale = Vector3.one;
        if (_iconImage != null)
        {
            _iconImage.color = Color.white;
        }

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOPunchScale(Vector3.one * 0.34f, 0.28f, 10, 0.9f));

        if (_flashImage != null)
        {
            _flashImage.sprite = _iconImage != null ? _iconImage.sprite : _flashImage.sprite;
            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            _flashImage.transform.localScale = Vector3.one * 0.75f;

            if (_iconImage != null)
            {
                sequence.Join(_iconImage.DOColor(TriggerFlashColor, 0.06f).SetEase(Ease.OutQuad));
                sequence.Insert(0.12f, _iconImage.DOColor(Color.white, 0.18f).SetEase(Ease.OutQuad));
            }

            sequence.Join(_flashImage.DOFade(1f, 0.05f).SetEase(Ease.OutQuad));
            sequence.Join(_flashImage.transform.DOScale(Vector3.one * 1.65f, 0.24f).SetEase(Ease.OutQuad));
            sequence.Insert(0.08f, _flashImage.DOFade(0.35f, 0.10f).SetEase(Ease.OutQuad));
            sequence.Insert(0.15f, _flashImage.DOFade(0.95f, 0.05f).SetEase(Ease.OutQuad));
            sequence.Append(_flashImage.DOFade(0f, 0.20f).SetEase(Ease.OutQuad));
            sequence.Join(_flashImage.transform.DOScale(Vector3.one * 2.05f, 0.20f).SetEase(Ease.OutQuad));
        }
        else if (_iconImage != null)
        {
            sequence.Join(_iconImage.DOColor(TriggerFlashColor, 0.08f).SetEase(Ease.OutQuad));
            sequence.Append(_iconImage.DOColor(Color.white, 0.22f).SetEase(Ease.OutQuad));
        }
    }

    private void ResetFlashImage(Sprite iconSprite)
    {
        if (_flashImage == null)
        {
            return;
        }

        _flashImage.DOKill();
        _flashImage.transform.DOKill();
        _flashImage.sprite = iconSprite;
        _flashImage.color = new Color(1f, 1f, 1f, 0f);
        _flashImage.transform.localScale = Vector3.one;
        _flashImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (_iconImage != null)
        {
            _iconImage.DOKill();
        }

        if (_flashImage != null)
        {
            _flashImage.DOKill();
            _flashImage.transform.DOKill();
        }
    }

    #region UIEvent 

    public void OnPointerEnter(PointerEventData eventData)
    {
        ArtifactGuideUtility.ShowArtifactGuide(Item, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ArtifactGuideUtility.HideArtifactGuide(transform);
    }

    #endregion
}
