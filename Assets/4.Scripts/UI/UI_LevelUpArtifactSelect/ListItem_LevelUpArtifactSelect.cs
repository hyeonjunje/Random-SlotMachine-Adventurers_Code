using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListItem_LevelUpArtifactSelect : BaseListItem<SO_ArtifactData>, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image _imageArtifactIcon;
    [SerializeField] private Image _imageFrame;
    [SerializeField] private Image _imageFocusFrame;

    private UI_LevelUpArtifactSelect _uiLevelUpArtifactSelect;
    private Player _ownerPlayer;
    private RewardItemAppearAnimator _rewardItemAppearAnimator;

    public void InitializeForArtifactSelect(SO_ArtifactData item, Player ownerPlayer)
    {
        _ownerPlayer = ownerPlayer;
        SetListItem(item);
    }

    public override void SetListItem(SO_ArtifactData item)
    {
        base.SetListItem (item);
        gameObject.SetActive (true);

        _imageArtifactIcon.sprite = item.Icon;

        _uiLevelUpArtifactSelect =
            UIManager.Instance.Get<UI_LevelUpArtifactSelect>(EUIType.UI_LevelUpArtifactSelect);
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
    public void OnPointerEnter(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive (false);
        _imageFocusFrame.gameObject.SetActive (true);

        ArtifactGuideUtility.ShowArtifactGuide(Item, transform, _ownerPlayer);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _imageFrame.gameObject.SetActive (true);
        _imageFocusFrame.gameObject.SetActive (false);
        ArtifactGuideUtility.HideArtifactGuide(transform);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GetRewardItemAppearAnimator().IsSelectable == false)
        {
            return;
        }

        _uiLevelUpArtifactSelect.HandleClickArtifact(Item, this);
    }
    #endregion
}
