using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_ArtifactReward : BaseListItem<SO_ArtifactData>, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _imageIcon;
    [SerializeField] private Image _imageFrame;
    [SerializeField] private Image _imageFocusFrame;
    private RewardItemAppearAnimator _rewardItemAppearAnimator;

    public override void SetListItem(SO_ArtifactData item)
    {
        base.SetListItem(item);

        _imageIcon.sprite = item.Icon;
        gameObject.SetActive(true);
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

        ArtifactSystem.Instance.AddArtifact(Item.ID);

        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);
        mainHUD.ClickRightButton();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive(false);
        _imageFocusFrame.gameObject.SetActive(true);

        ArtifactGuideUtility.ShowArtifactGuide(Item, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive(true);
        _imageFocusFrame.gameObject.SetActive(false);
        ArtifactGuideUtility.HideArtifactGuide(transform);
    }
    #endregion
}
