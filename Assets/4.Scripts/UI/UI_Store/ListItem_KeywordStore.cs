using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_KeywordStore : BaseListItem<SO_KeywordData>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private static readonly Color DiscountPriceColor = new Color (1f, 0.2f, 0.2f, 1f);

    [SerializeField] private KeywordCardPreviewUI _keywordCardPreviewUI;

    [SerializeField] private TMP_Text _textKeywordCost;
    [SerializeField] private GameObject _pivotPurchased;
    [SerializeField] private GameObject _costPanel;
    [SerializeField] private GameObject _discountBadge;

    private Action<ListItem_KeywordStore> _onClickAction;

    private UI_Store _uiStore;
    public bool IsPurchased { get; private set; }
    public int BasePrice { get; private set; }
    public int OriginalPrice { get; private set; }
    private bool _hasCachedPriceColor;
    private Color _defaultPriceColor;


    public int Price
    {
        get
        {
            return ArtifactRuntimeState.GetAdjustedShopPrice(BasePrice);
        }
    }
    public void InitializeForReward(SO_KeywordData keywordData, Action<ListItem_KeywordStore> onClick)
    {
        SetItemCommon (keywordData);
        _costPanel.SetActive (false);
        _onClickAction = onClick;
    }

    public override void SetListItem(SO_KeywordData keywordData)
    {
        StorePriceResult price = StorePricingService.GetKeywordPrice(keywordData);
        SetListItem(keywordData, price.Price, price.OriginalPrice);
    }

    public void SetListItem(SO_KeywordData keywordData, int price, int originalPrice)
    {
        base.SetListItem (keywordData);
        _uiStore = UIManager.Instance.Get<UI_Store> (EUIType.UI_Store);
        BasePrice = price;
        OriginalPrice = originalPrice;

        SetItemCommon (keywordData);

        IsPurchased = false;

        _pivotPurchased.gameObject.SetActive (false);
        _costPanel.SetActive (true);
        RefreshPriceText ();
        _onClickAction = null;
    }

    private void SetItemCommon(SO_KeywordData keywordData)
    {
        base.SetListItem (keywordData);
        _keywordCardPreviewUI.ShowCardView(keywordData, EKeywordCardPreviewType.StoreDisplay);
    }

    private void RefreshPriceText()
    {
        bool isDiscounted = OriginalPrice > BasePrice;

        CachePriceColor ();
        _textKeywordCost.text = $"{Price}G";
        _textKeywordCost.color = isDiscounted ? DiscountPriceColor : _defaultPriceColor;

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

        _defaultPriceColor = _textKeywordCost.color;
        _hasCachedPriceColor = true;
    }

    #region UI Event
    public void Purchased()
    {
        IsPurchased = true;
        _pivotPurchased.gameObject.SetActive (true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsPurchased) return;

        if (_onClickAction != null)
        {
            _onClickAction.Invoke (this);
        }
        else
        {
            _uiStore.HandleClickKeyword (Item, this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsPurchased) return;

        if (ShouldUseGlobalPreview())
        {
            UIManager.Instance.ShowKeywordCardPreview(Item, transform);
        }

        // UIManager.Instance.ShowGuidePopup(Item.KeywordName, Item.KeywordExplain, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ShouldUseGlobalPreview())
        {
            UIManager.Instance.HideKeywordCardPreview();
        }

        // UIManager.Instance.HideGuidePopup(transform);
    }
    #endregion

    private bool ShouldUseGlobalPreview()
    {
        UI_Store uiStore = UIManager.Instance.Get<UI_Store>(EUIType.UI_Store);
        return uiStore == null || uiStore.gameObject.activeInHierarchy == false;
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
