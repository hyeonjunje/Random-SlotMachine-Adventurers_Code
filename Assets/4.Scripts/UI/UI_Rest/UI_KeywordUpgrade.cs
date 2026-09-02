using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UI_KeywordUpgrade : UI_Base
{
    [Header ("강화 보상 UI")]
    [SerializeField] private Transform _upgradeItemParent;
    [SerializeField] private ListItem_KeywordStore _keywordItemPrefab;

    private EKeyword _targetKeywordToUpgrade;
    private Action _onCloseCallback;

    public override void Open()
    {
        gameObject.SetActive (true);
    }

    public override void Close()
    {
        UIManager.Instance.HideKeywordCardPreview();
        _upgradeItemParent.DestroyAllChildren ();
        gameObject.SetActive (false);
    }

    public void OpenUpgrade(EKeyword targetKeyword, Action onCloseCallback)
    {
        _targetKeywordToUpgrade = targetKeyword;
        _onCloseCallback = onCloseCallback;

        Open ();
        _upgradeItemParent.DestroyAllChildren ();

        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);
        if (mainHUD != null)
        {
            mainHUD.HideLeftButton ();
            mainHUD.SetRightButton (() =>
            {
                FinishUpgradeProcess ();
            }, LocalizationManager.Instance.Get("CS_UI_KEYWORDUPGRADE_073"));
        }

        ShowUpgradeOptions ();
    }

    private void ShowUpgradeOptions()
    {
        SO_KeywordData oldData = DataManager.Instance.GetKeywordData (_targetKeywordToUpgrade);
        int targetRank = oldData.Rank + 1;

        List<SO_KeywordData> fullPool = Utils.GetFullPoolByType (oldData.KeywordType);

        if (fullPool == null || fullPool.Count == 0)
        {
            HandleNoUpgradeAvailable ();
            return;
        }

        List<SO_KeywordData> validCandidates = new List<SO_KeywordData> ();
        foreach (var data in fullPool)
        {
            if (data.Rank == targetRank)
            {
                validCandidates.Add (data);
            }
        }

        if (validCandidates.Count == 0)
        {
            HandleNoUpgradeAvailable ();
            return;
        }

        validCandidates.Shuffle ();
        int desiredOptionCount = Mathf.Max(1, DataManager.Instance.GameModel.KeywordUpgradeOptionCount);
        int optionCount = Mathf.Min (desiredOptionCount, validCandidates.Count);

        for (int i = 0; i < optionCount; i++)
        {
            var item = Instantiate (_keywordItemPrefab, _upgradeItemParent);
            item.InitializeForReward (validCandidates[i], OnClickUpgradeOption);
        }
    }

    private void OnClickUpgradeOption(ListItem_KeywordStore clickedItem)
    {
        EKeyword newKeyword = clickedItem.Item.Keyword;

        RemoveSlotMachineKeywordGA removeGA = new RemoveSlotMachineKeywordGA (_targetKeywordToUpgrade, 0);

        AddSlotMachineKeywordGA addGA = new AddSlotMachineKeywordGA (newKeyword, 0);

        ActionSystem.Instance.Perform (removeGA, () =>
        {
            ActionSystem.Instance.Perform (addGA, () =>
            {
                string newName = LocalizationManager.Instance.Get(DataManager.Instance.GetKeywordData (newKeyword).KeywordName);
                EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_UI_KEYWORDUPGRADE_074"), newName), EMessageType.Notice));

                FinishUpgradeProcess ();
            });
        });
    }

    private void HandleNoUpgradeAvailable()
    {
        Debug.LogWarning ("강화할 수 있는 다음 랭크의 키워드가 없습니다!");
        EventBus.Publish (new StSendMessageEvent (LocalizationManager.Instance.Get("CS_UI_KEYWORDUPGRADE_075"), EMessageType.Warning));
        FinishUpgradeProcess ();
    }

    private void FinishUpgradeProcess()
    {
        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);
        if (mainHUD != null)
        {
            mainHUD.HideRightButton ();
        }

        Close ();
        _onCloseCallback?.Invoke ();
    }
}

