using Spine.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UI_Store : UI_Base
{
    [SerializeField] private ListItem_CharacterStore _characterItemPrefab;
    [SerializeField] private Transform _characterItemParent;
    [SerializeField] private ListItem_KeywordStore _keywordItemPrefab;
    [SerializeField] private Transform _keywordItemParent;
    [SerializeField] private ListItem_ArtifactStore _artifactItemPrefab;
    [SerializeField] private Transform _artifactItemParent;
    [SerializeField] private Button _buttonRemoveKeyword;
    [SerializeField] private GameObject _keywordRemovedImage;

    [SerializeField] private KeywordCardPreviewUI _keywordCardPreviewUI;

    private IDisposable _onEnterShopNodeEvent;
    private bool _isKeywordRemoved = false; 
    private List<ListItem_CharacterStore> _characterItems = new List<ListItem_CharacterStore> ();
    private List<ListItem_KeywordStore> _keywordItems = new List<ListItem_KeywordStore> ();
    private List<ListItem_ArtifactStore> _artifactItems = new List<ListItem_ArtifactStore> ();

    public override void Initialize()
    {
        base.Initialize();
          
        _onEnterShopNodeEvent = EventBus.Subscribe<StEnterShopNodeEvent> (OnEnterShopNodeEvent);
    }

    protected override void Dispose()
    {
        base.Dispose();
        _onEnterShopNodeEvent?.Dispose (); 
    }

    public override void Open()
    {
        _isKeywordRemoved = false;
        _buttonRemoveKeyword.interactable = true;
        _keywordRemovedImage.SetActive (false);
        gameObject.SetActive(true);

        RerollStore ();

        ShowLeaveStoreButton ();
    }

    private void ShowLeaveStoreButton()
    {
        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);
        mainHUD.SetRightButton (() =>
        {
            ClearNodeGA clearNodeGA = new ClearNodeGA ();
            ActionSystem.Instance.Perform (clearNodeGA);

            mainHUD.HideRightButton ();
        }, LocalizationManager.Instance.Get("CS_UI_STORE_082"));
    }

    private void HideLeaveStoreButton()
    {
        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);
        mainHUD.HideRightButton ();
    }

    private void OnEnterShopNodeEvent(StEnterShopNodeEvent enterRestNodeEvent)
    {
        Open ();
    }

    public override void Close()
    {
        UIManager.Instance.HideCharacterGuide ();
        UIManager.Instance.Close (EUIType.UI_MyKeywords);

        _characterItemParent.DestroyAllChildren ();
        _characterItems.Clear ();
        _keywordItemParent.DestroyAllChildren ();
        _keywordItems.Clear ();
        _artifactItemParent.DestroyAllChildren ();
        _artifactItems.Clear ();

        gameObject.SetActive (false);
    }

    private void RerollStore()
    {
        List<StorePriceResult> discountablePrices = new List<StorePriceResult> ();
        List<Player> players = StorePricingService.PickLevelUpOffers (3);
        List<SO_KeywordData> keywords = StorePricingService.PickKeywordOffers (3);
        List<SO_ArtifactData> artifacts = StorePricingService.PickArtifactOffers (3);
        List<StorePriceResult> keywordPrices = keywords.Select (StorePricingService.GetKeywordPrice).ToList ();
        List<StorePriceResult> artifactPrices = artifacts.Select (StorePricingService.GetArtifactPrice).ToList ();

        discountablePrices.AddRange (keywordPrices);
        discountablePrices.AddRange (artifactPrices);
        StorePricingService.ApplyGroupDiscounts (discountablePrices);

        RerollStoreCharacter (players);
        RerollKeywords (keywords, keywordPrices);
        RerollArtifacts (artifacts, artifactPrices);
    }

    private void RerollArtifacts(List<SO_ArtifactData> randomArtifacts, List<StorePriceResult> prices)
    {
        while (_artifactItems.Count < 3)
        {
            _artifactItems.Add (Instantiate (_artifactItemPrefab, _artifactItemParent));
        }

        for (int i = 0; i < _artifactItems.Count; i++)
        {
            if (i < randomArtifacts.Count)
            {
                int price = i < prices.Count ? prices[i].Price : StorePricingService.GetArtifactPrice (randomArtifacts[i]).Price;
                int originalPrice = i < prices.Count ? prices[i].OriginalPrice : price;
                _artifactItems[i].InitializeForShop (randomArtifacts[i], price, originalPrice, OnClickArtifactItem);
            }
            else
            {
                _artifactItems[i].gameObject.SetActive (false);
            }
        }
    }

    private void RerollStoreCharacter(List<Player> players)
    {
        while (_characterItems.Count < 3)
        {
            _characterItems.Add (Instantiate (_characterItemPrefab, _characterItemParent));
        }

        for (int i = 0; i < 3; ++i)
        {
            if (i < players.Count && players[i] != null)
            {
                StorePriceResult price = StorePricingService.GetLevelUpPrice (players[i]);
                _characterItems[i].SetListItem (players[i], price.Price);
            }
            else
            {
                _characterItems[i].gameObject.SetActive (false);
            }
        }
    }

    private void RerollKeywords(List<SO_KeywordData> keywords, List<StorePriceResult> prices)
    {
        while (_keywordItems.Count < 3)
        {
            _keywordItems.Add (Instantiate (_keywordItemPrefab, _keywordItemParent));
        }

        for (int i = 0; i < _keywordItems.Count; i++)
        {
            if (i < keywords.Count && keywords[i] != null && keywords[i].KeywordType != EKeywordType.None)
            {
                int price = i < prices.Count ? prices[i].Price : StorePricingService.GetKeywordPrice (keywords[i]).Price;
                int originalPrice = i < prices.Count ? prices[i].OriginalPrice : price;
                _keywordItems[i].SetListItem (keywords[i], price, originalPrice);
            }
            else
            {
                _keywordItems[i].gameObject.SetActive (false);
            }
        }
    }

    public void HandleClickArtifact(SO_ArtifactData artifactData, ListItem_ArtifactStore listItem)
    {
        if (listItem.IsPurchased) return;

        int price = ArtifactRuntimeState.GetAdjustedShopPrice(listItem.Price);
        if (CheckGold (price) == false)
        {
            AudioManager.Instance.PlaySFX(ESfxId.LackOfMoney);
            return;
        }

        PurchaseArtifactGA purchaseGA = new PurchaseArtifactGA (artifactData.ID, price);

        ActionSystem.Instance.Perform (purchaseGA, () => {
            AudioManager.Instance.PlaySFX(ESfxId.Buy_Goods);
            PlayDialogue();
            listItem.Purchased ();
        });
    }

    public void HandleClickCharacter(Player player, ListItem_CharacterStore listItem)
    {
        if (listItem.IsPurchased || listItem.IsLevelUpDisabled) return;

        PlayerView targetView = null;

        foreach (PlayerView partyMember in CharacterSystem.Instance.Players)
        {
            if (partyMember.Player.PlayerData.SubjectKeyword == player.PlayerData.SubjectKeyword)
            {
                targetView = partyMember;
                break;
            }
        }

        if (targetView == null)
        {
            return;
        }

        if (targetView.Player.IsMaxLevel)
        {
            AudioManager.Instance.PlaySFX(ESfxId.LackOfMoney);
            EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_CHARACTERSYSTEM_009")
                , LocalizationManager.Instance.Get(targetView.Player.PlayerData.SubjectKeyword.ToString())), EMessageType.Warning));
            return;
        }

        int price = listItem.Price;
        if (CheckGold(price) == false)
        {
            AudioManager.Instance.PlaySFX(ESfxId.LackOfMoney);
            return;
        }

        if (ActionSystem.Instance.IsPerforming)
        {
            return;
        }

        HideLeaveStoreButton ();

        LevelUpPlayerGA levelUpGA = new LevelUpPlayerGA (1, targetView, price);

        ActionSystem.Instance.Perform (levelUpGA, () => {

            AudioManager.Instance.PlaySFX(ESfxId.Buy_Goods);
            PlayDialogue();
            listItem.Purchased ();
            ShowLeaveStoreButton ();
        });
    }
    private void OnClickArtifactItem(ListItem_ArtifactStore itemScript)
    {
        HandleClickArtifact (itemScript.Item, itemScript);
    }
    public void HandleClickKeyword(SO_KeywordData keywordData, ListItem_KeywordStore listItem)
    {
        if (listItem.IsPurchased) return;
        if (CheckGold(listItem.Price) == false)
        {
            AudioManager.Instance.PlaySFX(ESfxId.LackOfMoney);
            return;
        }

        AddSlotMachineKeywordGA addGA = new AddSlotMachineKeywordGA (keywordData.Keyword, listItem.Price);

        ActionSystem.Instance.Perform (addGA, () => {
            AudioManager.Instance.PlaySFX(ESfxId.Buy_Goods);
            PlayDialogue();
            listItem.Purchased ();
        });
    }

    public void OnClickRemoveKeyword(int cost)
    {
        if(_isKeywordRemoved) return;

        int baseCost = StorePricingService.GetWordRemovalPrice(DataManager.Instance.GameModel.WordRemovalBuyCount);
        int adjustedCost = ArtifactRuntimeState.GetAdjustedShopPrice(baseCost);

        if (!CheckGold(adjustedCost))
        {
            AudioManager.Instance.PlaySFX(ESfxId.LackOfMoney);
            return;
        }

        UI_MyKeywords myKeywordsUI = UIManager.Instance.Get<UI_MyKeywords> (EUIType.UI_MyKeywords);

        myKeywordsUI.OpenForSelect ((selectedKeyword) =>
        {
            RemoveSlotMachineKeywordGA removeGA = new RemoveSlotMachineKeywordGA (selectedKeyword, adjustedCost);

            ActionSystem.Instance.Perform (removeGA, () =>
            {
                AudioManager.Instance.PlaySFX(ESfxId.Buy_Goods);
                PlayDialogue();
                _isKeywordRemoved = true;
                DataManager.Instance.GameModel.WordRemovalBuyCount++;
                _buttonRemoveKeyword.interactable = false;
                EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_UI_REST_079"), selectedKeyword), EMessageType.Notice));
                _keywordRemovedImage.SetActive (true);
            });
        });
    }
    public void OpenFromSave(ShopCheckpoint checkpoint)
    {
        _isKeywordRemoved = false;
        _buttonRemoveKeyword.interactable = true;
        _keywordRemovedImage.SetActive (false);
        gameObject.SetActive (true);

        ApplySavedStore (checkpoint);

        ShowLeaveStoreButton ();
    }
    private void ApplySavedStore(ShopCheckpoint checkpoint)
    {
        while (_characterItems.Count < 3)
        {
            _characterItems.Add (Instantiate (_characterItemPrefab, _characterItemParent));
        }

        while (_keywordItems.Count < 3)
        {
            _keywordItems.Add (Instantiate (_keywordItemPrefab, _keywordItemParent));
        }

        while (_artifactItems.Count < 3)
        {
            _artifactItems.Add (Instantiate (_artifactItemPrefab, _artifactItemParent));
        }

        for (int i = 0; i < _characterItems.Count; i++)
        {
            if (checkpoint != null && i < checkpoint.CharacterSubjects.Count)
            {
                SO_PlayerData playerData = DataManager.Instance.AllPlayers
                    .FirstOrDefault (x => x.SubjectKeyword == checkpoint.CharacterSubjects[i]);

                if (playerData != null)
                {
                    PlayerView ownedPlayerView = CharacterSystem.Instance.Players
                        .FirstOrDefault (x => x.Player.PlayerData.SubjectKeyword == playerData.SubjectKeyword);

                    Player player = ownedPlayerView != null ? ownedPlayerView.Player : new Player (playerData);
                    int price = GetSavedPrice (checkpoint.CharacterOfferPrices, i, StorePricingService.GetLevelUpPrice (player).Price);
                    _characterItems[i].SetListItem (player, price);
                }
                else
                {
                    _characterItems[i].gameObject.SetActive (false);
                }
            }
            else
            {
                _characterItems[i].gameObject.SetActive (false);
            }
        }

        for (int i = 0; i < _keywordItems.Count; i++)
        {
            if (checkpoint != null && i < checkpoint.KeywordOffers.Count)
            {
                SO_KeywordData keywordData = DataManager.Instance.GetKeywordData (checkpoint.KeywordOffers[i]);

                if (keywordData != null)
                {
                    int price = GetSavedPrice (checkpoint.KeywordOfferPrices, i, StorePricingService.GetKeywordPrice (keywordData).Price);
                    int originalPrice = GetSavedPrice (checkpoint.KeywordOfferOriginalPrices, i, price);
                    _keywordItems[i].SetListItem (keywordData, price, originalPrice);
                }
                else
                {
                    _keywordItems[i].gameObject.SetActive (false);
                }
            }
            else
            {
                _keywordItems[i].gameObject.SetActive (false);
            }
        }

        for (int i = 0; i < _artifactItems.Count; i++)
        {
            if (checkpoint != null && i < checkpoint.ArtifactOffers.Count)
            {
                SO_ArtifactData artifactData = DataManager.Instance.AllArtifacts
                    .FirstOrDefault (x => x.ID == checkpoint.ArtifactOffers[i]);

                if (artifactData != null)
                {
                    int price = GetSavedPrice (checkpoint.ArtifactOfferPrices, i, StorePricingService.GetArtifactPrice (artifactData).Price);
                    int originalPrice = GetSavedPrice (checkpoint.ArtifactOfferOriginalPrices, i, price);
                    _artifactItems[i].InitializeForShop (artifactData, price, originalPrice, OnClickArtifactItem);
                }
                else
                {
                    _artifactItems[i].gameObject.SetActive (false);
                }
            }
            else
            {
                _artifactItems[i].gameObject.SetActive (false);
            }
        }
    }

    public void ShowCardPreview(SO_KeywordData kewordData)
    {
        _keywordCardPreviewUI.ShowCardView(kewordData, EKeywordCardPreviewType.Guide);
    }

    public void HideCardPreview()
    {
        _keywordCardPreviewUI.HideCardView();
    }

    private bool CheckGold(int price)
    {
        if (!UIHudSystem.Instance.CanPayGold (price))
        {
            EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_UI_STORE_083"), EMessageType.Warning));
            return false; 
        }

        return true;
    }

    [SerializeField] private TutorialDialogueUI _dialogueUI;
    [SerializeField] private SkeletonGraphic _skeletonGraphic;

    private void PlayDialogue()
    {
        string dialogueRandomKey = "CS_STORE_PURCHASE_00" + UnityEngine.Random.Range (1, 6);
        string dialogueText = LocalizationManager.Instance.Get (dialogueRandomKey);

        _skeletonGraphic.AnimationState.SetAnimation (0, "Talk", false);
        _skeletonGraphic.AnimationState.AddAnimation (0, "Idle", true, 0f);

        _dialogueUI.Show (dialogueText, null);
    }

    private int GetSavedPrice(List<int> prices, int index, int fallback)
    {
        if (prices != null && index >= 0 && index < prices.Count && prices[index] > 0)
        {
            return prices[index];
        }

        return fallback;
    }
}

