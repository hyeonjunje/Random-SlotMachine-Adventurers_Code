using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_CharacterSelection : BaseListItem<Player>, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _imagePortrait;
    [SerializeField] private Image _focusImage;
    [SerializeField] private Button _selectButton;
    [SerializeField] private Material _grayScaleMat;

    private Material _defaultPortraitMat;
    private bool _isSelected;

    private void Awake()
    {
        _defaultPortraitMat = _imagePortrait.material;

        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(OnClickSelectButton);
    }

    public override void SetListItem(Player item)
    {
        base.SetListItem(item);
        gameObject.SetActive(true);

        _imagePortrait.sprite = SpriteManager.Instance.GetSprite(item.PlayerData.IllustrationName);
        ApplyIllustrationFraming(item.PlayerData);

        if(UIManager.Instance.IsCharacterGuideParent(transform))
        {
            UIManager.Instance.HideCharacterGuide();
        }

        // 선택 상태 초기화
        SetSelected(false);
        transform.localScale = Vector3.one;
    }

    private void ApplyIllustrationFraming(SO_PlayerData playerData)
    {
        if (_imagePortrait == null || playerData == null)
        {
            return;
        }

        RectTransform portraitRect = _imagePortrait.rectTransform;
        portraitRect.anchoredPosition = playerData.SelectionIllustrationOffset;

        float scale = playerData.SelectionIllustrationScale;
        if (scale <= 0f)
        {
            scale = 1f;
        }

        portraitRect.localScale = Vector3.one * scale;
    }

    #region UIEvent

    public void OnClickListItem()
    {
        UI_SelectCharacter uiSelectCharacter = UIManager.Instance.Get<UI_SelectCharacter>(EUIType.UI_SelectCharacter);

        uiSelectCharacter.OnClickCharacter(Item);
    }

    private void OnClickSelectButton()
    {
        UI_SelectCharacter uiSelectCharacter = UIManager.Instance.Get<UI_SelectCharacter>(EUIType.UI_SelectCharacter);
        uiSelectCharacter.OnClickComplete();
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        _focusImage.gameObject.SetActive(isSelected);
        _selectButton.gameObject.SetActive(isSelected);

        if (_imagePortrait != null)
        {
            _imagePortrait.material = isSelected ? _defaultPortraitMat : _grayScaleMat;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isSelected) return;
        _focusImage.gameObject.SetActive (true);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isSelected) return;
        _focusImage.gameObject.SetActive (false);
    }

    #endregion
}
