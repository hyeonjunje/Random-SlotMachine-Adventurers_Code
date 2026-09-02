using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListItem_KeywordReward : BaseListItem<SO_KeywordData>, IPointerClickHandler
{
    [SerializeField] private KeywordCardPreviewUI _keywordCardPreviewUI;
    private RewardItemAppearAnimator _rewardItemAppearAnimator;

    public override void SetListItem(SO_KeywordData keywordData)
    {
        base.SetListItem(keywordData);
        _keywordCardPreviewUI.ShowCardView(keywordData, EKeywordCardPreviewType.Reward);
    }

    public RewardItemAppearAnimator GetRewardItemAppearAnimator()
    {
        if (_rewardItemAppearAnimator == null)
        {
            _rewardItemAppearAnimator = GetComponent<RewardItemAppearAnimator>();
            if (_rewardItemAppearAnimator == null)
            {
                _rewardItemAppearAnimator = gameObject.AddComponent<RewardItemAppearAnimator>();
            }
        }

        return _rewardItemAppearAnimator;
    }

    #region UI Event
    public void OnPointerClick(PointerEventData eventData)
    {
        if (GetRewardItemAppearAnimator().IsSelectable == false)
        {
            return;
        }

        UI_Reward rewardUI = GetComponentInParent<UI_Reward>();
        if (rewardUI != null && rewardUI.TryBeginClaimReward() == false)
        {
            return;
        }

        AddSlotMachineKeywordGA addGA = new AddSlotMachineKeywordGA(Item.Keyword, 0);
        ActionSystem.Instance.Perform(addGA, () =>
        {
            UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);
            mainHUD.ClickRightButton();
        });
    }
    #endregion
}
