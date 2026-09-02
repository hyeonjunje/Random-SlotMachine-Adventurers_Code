using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ListItem_CharacterStore : BaseListItem<Player>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private static readonly Color DisabledTint = new Color (0.5f, 0.5f, 0.5f, 1f);

    [SerializeField] private Image _imageJob;
    [SerializeField] private Image _imagePortrait;
    [SerializeField] private Image _imagePurchased;
    [SerializeField] private Image _imageFrame;
    [SerializeField] private Image _imageFocusFrame;

    [SerializeField] private TMP_Text _textJob;
    [SerializeField] private TMP_Text _textCost;



    private UI_Store _uiStore;
    public bool IsPurchased { get; private set; }
    public bool IsLevelUpDisabled { get; private set; }
    public int BasePrice { get; private set; }
    public int Price => ArtifactRuntimeState.GetAdjustedShopPrice(BasePrice);

    private bool _hasCachedColors;
    private Color _jobDefaultColor;
    private Color _portraitDefaultColor;
    private Color _frameDefaultColor;
    private Color _focusFrameDefaultColor;
    private Color _jobTextDefaultColor;
    private Color _costTextDefaultColor;

    public override void SetListItem(Player item)
    {
        StorePriceResult price = StorePricingService.GetLevelUpPrice(item);
        SetListItem(item, price.Price);
    }

    public void SetListItem(Player item, int price)
    {
        base.SetListItem (item);
        gameObject.SetActive (true);
        CacheDefaultColors ();

        IsPurchased = false;
        IsLevelUpDisabled = item.IsMaxLevel;
        BasePrice = price;
        _imagePurchased.gameObject.SetActive (false);

        _imageJob.sprite = SpriteManager.Instance.GetSprite(item.PlayerData.JobIconName);
        _imagePortrait.sprite = SpriteManager.Instance.GetSprite(item.PlayerData.PortraitIconName);
        _textJob.text = LocalizationManager.Instance.Get(Item.PlayerData.SubjectKeyword.ToString ());
        _textCost.text = IsLevelUpDisabled ? "MAX" : Price.ToString() + "G";
        _uiStore = UIManager.Instance.Get<UI_Store> (EUIType.UI_Store);

        ApplyAvailabilityState ();
    }

    public void Purchased()
    {
        _imagePurchased.gameObject.SetActive (true);
        IsPurchased = true;
    }

    private void CacheDefaultColors()
    {
        if (_hasCachedColors)
        {
            return;
        }

        _jobDefaultColor = _imageJob.color;
        _portraitDefaultColor = _imagePortrait.color;
        _frameDefaultColor = _imageFrame.color;
        _focusFrameDefaultColor = _imageFocusFrame.color;
        _jobTextDefaultColor = _textJob.color;
        _costTextDefaultColor = _textCost.color;
        _hasCachedColors = true;
    }

    private void ApplyAvailabilityState()
    {
        _imageJob.color = IsLevelUpDisabled ? DisabledTint : _jobDefaultColor;
        _imagePortrait.color = IsLevelUpDisabled ? DisabledTint : _portraitDefaultColor;
        _imageFrame.color = IsLevelUpDisabled ? DisabledTint : _frameDefaultColor;
        _imageFocusFrame.color = IsLevelUpDisabled ? DisabledTint : _focusFrameDefaultColor;
        _textJob.color = IsLevelUpDisabled ? DisabledTint : _jobTextDefaultColor;
        _textCost.color = IsLevelUpDisabled ? DisabledTint : _costTextDefaultColor;

        _imageFrame.gameObject.SetActive (true);
        _imageFocusFrame.gameObject.SetActive (false);
    }

    #region UI Event
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsPurchased) return;
        if (IsLevelUpDisabled) return;

        _imageFrame.gameObject.SetActive (false);
        _imageFocusFrame.gameObject.SetActive (true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive (true);
        _imageFocusFrame.gameObject.SetActive (false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsPurchased || IsLevelUpDisabled) return;

        if (_uiStore != null)
        {
            _uiStore.HandleClickCharacter (Item, this);
        }
    }
    #endregion
}
