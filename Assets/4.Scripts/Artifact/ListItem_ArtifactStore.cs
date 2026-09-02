    using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_ArtifactStore : BaseListItem<SO_ArtifactData>, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color DiscountPriceColor = new Color (1f, 0.2f, 0.2f, 1f);

    [SerializeField] private Image _imageIcon;
    [SerializeField] private TextMeshProUGUI _textArtifactCost;
    [SerializeField] private Image _imagePurchased;
    [SerializeField] private Image _imageFrame;
    [SerializeField] private Image _imageFocusFrame;
    [SerializeField] private GameObject _costPanel;
    [SerializeField] private GameObject _discountBadge;
    public bool IsPurchased { get; private set; }
    public int Price { get; private set; }
    public int OriginalPrice { get; private set; }
    private Action<ListItem_ArtifactStore> _onClickCallback;
    private bool _hasCachedPriceColor;
    private Color _defaultPriceColor;
    public void InitializeForShop(SO_ArtifactData item, Action<ListItem_ArtifactStore> onClick)
    {
        StorePriceResult price = StorePricingService.GetArtifactPrice(item);
        InitializeForShop(item, price.Price, price.OriginalPrice, onClick);
    }

    public void InitializeForShop(SO_ArtifactData item, int price, int originalPrice, Action<ListItem_ArtifactStore> onClick)
    {
        SetListItem (item);

        Price = price;
        OriginalPrice = originalPrice;

        _textArtifactCost.gameObject.SetActive (true);
        RefreshPriceText ();
        _onClickCallback = onClick;
    }

    public void InitializeForReward(SO_ArtifactData item, Action<ListItem_ArtifactStore> onClick)
    {
        SetListItem (item);
        _textArtifactCost.gameObject.SetActive (false);
        _onClickCallback = onClick;
        _costPanel.SetActive (false);
    }
    public override void SetListItem(SO_ArtifactData item)
    {
        base.SetListItem (item);
        IsPurchased = false;

        _imagePurchased.gameObject.SetActive (false);

        _imageIcon.sprite = item.Icon;
        StorePriceResult price = StorePricingService.GetArtifactPrice(item);
        Price = price.Price;
        OriginalPrice = price.OriginalPrice;
        RefreshPriceText ();

        gameObject.SetActive (true);
    }

    private void RefreshPriceText()
    {
        int adjustedPrice = ArtifactRuntimeState.GetAdjustedShopPrice (Price);
        bool isDiscounted = OriginalPrice > Price;

        CachePriceColor ();
        _textArtifactCost.text = $"{adjustedPrice}G";
        _textArtifactCost.color = isDiscounted ? DiscountPriceColor : _defaultPriceColor;

        if (_discountBadge != null)
        {
            _discountBadge.SetActive (isDiscounted);
        }
    }

    private void CachePriceColor()
    {
        if (_hasCachedPriceColor)
        {
            return;
        }

        _defaultPriceColor = _textArtifactCost.color;
        _hasCachedPriceColor = true;
    }

    public void Purchased()
    {
        IsPurchased = true;
        _imagePurchased.gameObject.SetActive (true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsPurchased)
        {
            return;
        }

        _onClickCallback?.Invoke (this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsPurchased) return;

        _imageFrame.gameObject.SetActive (false);
        _imageFocusFrame.gameObject.SetActive (true);

        ArtifactGuideUtility.ShowArtifactGuide(Item, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive (true);
        _imageFocusFrame.gameObject.SetActive (false);
        ArtifactGuideUtility.HideArtifactGuide(transform);
    }

}
