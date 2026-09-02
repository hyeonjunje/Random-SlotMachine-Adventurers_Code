using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_Rest : UI_Base
{
    [Header("Selection")]
    [SerializeField] private GameObject _pivotSelection;
    [SerializeField] private GameObject _pivotKeywordService;    

    [Header("Fixed Keyword")]

    private IDisposable _onEnterRestNodeEvent;

    public override void Initialize()
    {
        base.Initialize();
        _onEnterRestNodeEvent = EventBus.Subscribe<StEnterRestNodeEvent>(OnEnterRestNodeEvent);
    }

    private void OnDestroy()
    {
        _onEnterRestNodeEvent?.Dispose();
    }

    public override void Close()
    {
        gameObject.SetActive(false);
    }

    public override void Open()
    {
        gameObject.SetActive(true);
        SetInitUI();
    }

    private void OnEnterRestNodeEvent(StEnterRestNodeEvent enterRestNodeEvent)
    {
        Open();
    }

    private void SetInitUI()
    {
        _pivotSelection.SetActive(true);
        _pivotKeywordService.SetActive (false); 
    }

    public void ActiveClearButton()
    {
        // 행동할거 다 했으면 다 꺼주고 떠나기 버튼만 설정
        SetInitUI();
        _pivotSelection.SetActive(false);

        UI_MainHud mainHud = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);
        mainHud.HideLeftButton();

        mainHud.SetRightButton(() =>
        {
            ClearNodeGA clearNodeGA = new ClearNodeGA();
            ActionSystem.Instance.Perform(clearNodeGA);

            mainHud.HideRightButton();
        }, LocalizationManager.Instance.Get("CS_UI_REST_076"));
    }

    #region UIEvent
    public void OnClickRest()
    {
        HealthController partyHealth = CharacterSystem.Instance.PartyHealth;

        int healAmount = Mathf.RoundToInt (partyHealth.MaxHp * DataManager.Instance.GameModel.RestHealingValue);

        partyHealth.RestoreHealth (healAmount);

        AudioManager.Instance.PlaySFX(ESfxId.Rest);
        EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_UI_REST_077"), healAmount), EMessageType.Notice));

        ActiveClearButton ();
    }
    public void OnClickPartyLevelUp()
    {
        bool canLevelUpAny = false;
        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            if (playerView.Player.IsMaxLevel == false)
            {
                canLevelUpAny = true;
                break;
            }
        }

        if (canLevelUpAny == false)
        {
            EventBus.Publish (new StSendMessageEvent (LocalizationManager.Instance.Get("CS_CHARACTERSYSTEM_011"), EMessageType.Warning));
            return;
        }

        LevelUpPartyGA levelUpPartyGA = new LevelUpPartyGA (1, 0);
        _pivotSelection.SetActive(false);

        ActionSystem.Instance.Perform (levelUpPartyGA, () =>
        {
            ActiveClearButton ();
        });
    }

    public void OnClickKeywordService()
    {
        _pivotSelection.SetActive (false);
        _pivotKeywordService.SetActive (true); 

        UI_MainHud mainHud = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);
        mainHud.SetLeftButton (() =>
        {
            SetInitUI (); 
            mainHud.HideLeftButton ();
        }, LocalizationManager.Instance.Get("CS_UI_REST_078"));
    }
    public void OnClickRemoveKeyword()
    {
        OpenKeywordSelect (isUpgrade: false, (selectedKeyword) =>
        {
            RemoveSlotMachineKeywordGA removeGA = new RemoveSlotMachineKeywordGA (selectedKeyword, 0); // 비용 0
            ActionSystem.Instance.Perform (removeGA, () =>
            {
                EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_UI_REST_079"), selectedKeyword), EMessageType.Notice));
                ActiveClearButton (); 
            });
        });
    }

    public void OnClickUpgradeKeyword()
    {
        OpenKeywordSelect (isUpgrade: true, (selectedKeyword) =>
        {
            var upgradePopup = UIManager.Instance.Get<UI_KeywordUpgrade> (EUIType.UI_KeywordUpgrade);

            upgradePopup.OpenUpgrade (selectedKeyword, () =>
            {
                ActiveClearButton (); 
            });
        });
    }

    private void OpenKeywordSelect(bool isUpgrade, Action<EKeyword> onSelectedCallback)
    {
        UI_MainHud mainHud = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);
        UI_MyKeywords myKeywordsUI = UIManager.Instance.Get<UI_MyKeywords> (EUIType.UI_MyKeywords);

        _pivotKeywordService.SetActive (false);

        mainHud.SetLeftButton (() =>
        {
            myKeywordsUI.Close (); 
            _pivotKeywordService.SetActive (true); 

            mainHud.SetLeftButton (() =>
            {
                SetInitUI ();
                mainHud.HideLeftButton ();
            }, LocalizationManager.Instance.Get("CS_UI_REST_078"));

        }, LocalizationManager.Instance.Get("CS_UI_REST_080"));

        myKeywordsUI.OpenForSelect (onSelectedCallback, isUpgrade);
    }
}
#endregion

