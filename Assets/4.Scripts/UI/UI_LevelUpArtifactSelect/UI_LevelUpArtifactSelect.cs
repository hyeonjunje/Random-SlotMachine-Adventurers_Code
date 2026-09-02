using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LevelUpArtifactSelect : UI_Base
{
    private const float RewardAppearInterval = 0.06f;

    [SerializeField] private ListItem_LevelUpArtifactSelect _artifactItemPrefab;
    [SerializeField] private Transform _artifactItemParent;
    [SerializeField] private TMP_Text _textTitle;
    [SerializeField] private Image _characterIcon;
    [SerializeField] private Image _characterBackground;
    [SerializeField, Range(0f, 1f)] private float _characterBackgroundAlpha = 0.35f;
    [Header("----- Job Background Color -----")]
    [SerializeField] private Transform _backgroundColorRoot;
    [SerializeField] private Image[] _backgroundColorImagesInOrder;
    [SerializeField] private float _backgroundColorTweenDuration = 0.25f;

    private Action<SO_ArtifactData> _onArtifactSelectedCallback;
    private List<ListItem_LevelUpArtifactSelect> _artifactItems = new List<ListItem_LevelUpArtifactSelect>();
    private readonly List<Image> _backgroundColorImages = new List<Image>();
    private Sequence _rewardAppearSequence;

    private bool _isExistRightButton;
    private bool _isExistLeftButton;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Close()
    {
        KillRewardAppearSequence();
        JobBackgroundColorUtility.KillTweens(_backgroundColorImages);
        _artifactItemParent.DestroyAllChildren();
        _artifactItems.Clear();
        gameObject.SetActive(false);

        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);

        if (_isExistLeftButton)
        {
            mainHUD.ShowLeftButton();
        }

        if(_isExistRightButton)
        {
            mainHUD.ShowRightButton();
        }
    }

    public override void Open()
    {
        gameObject.SetActive(true);

        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);

        _isExistLeftButton = false;
        _isExistRightButton = false;

        if (mainHUD.IsExistLeftButton)
        {
            _isExistLeftButton = true;
            mainHUD.HideLeftButton();
        }

        if (mainHUD.IsExistRightButton)
        {
            _isExistRightButton = true;
            mainHUD.HideRightButton();
        }
    }

    public void OpenForArtifactSelect(Player player, List<SO_ArtifactData> selectableArtifacts, Action<SO_ArtifactData> onSelected)
    {
        _onArtifactSelectedCallback = onSelected;

        _characterIcon.sprite = SpriteManager.Instance.GetSprite(player.PlayerData.PortraitIconName);
        _textTitle.text = string.Format(LocalizationManager.Instance.Get("CS_UI_LEVELUPARTIFACTSELECT_072"), player.Level
            , LocalizationManager.Instance.Get(player.PlayerData.SubjectKeyword.ToString()));

        _artifactItemParent.DestroyAllChildren();
        _artifactItems.Clear();
        List<RewardItemAppearAnimator> animators = new List<RewardItemAppearAnimator>();

        foreach (SO_ArtifactData artifact in selectableArtifacts)
        {
            ListItem_LevelUpArtifactSelect artifactItem = Instantiate(_artifactItemPrefab, _artifactItemParent);
            artifactItem.InitializeForArtifactSelect(artifact, player);
            _artifactItems.Add(artifactItem);
            animators.Add(artifactItem.GetRewardItemAppearAnimator());
        }

        Open();
        RefreshCharacterBackground(player);
        RefreshJobBackgroundColors(player);
        PlayRewardAppearSequence(animators);
    }

    private void RefreshCharacterBackground(Player player)
    {
        if (_characterBackground == null || player?.PlayerData == null)
        {
            return;
        }

        _characterBackground.rectTransform.anchoredPosition = player.PlayerData.LevelUpBackgroundIllustrationOffset;

        Sprite illustrationSprite = SpriteManager.Instance.GetSprite(player.PlayerData.IllustrationName);
        if (illustrationSprite == null)
        {
            _characterBackground.sprite = null;
            _characterBackground.color = new Color(0f, 0f, 0f, 0.8666667f);
            return;
        }

        _characterBackground.sprite = illustrationSprite;
        _characterBackground.preserveAspect = true;
        _characterBackground.color = new Color(1f, 1f, 1f, _characterBackgroundAlpha);
    }

    private void RefreshJobBackgroundColors(Player player)
    {
        if (player?.PlayerData == null)
        {
            return;
        }

        if (_backgroundColorImages.Count == 0)
        {
            JobBackgroundColorUtility.CacheImages(
                _backgroundColorImagesInOrder,
                transform,
                _backgroundColorRoot,
                _backgroundColorImages);
        }

        JobBackgroundColorUtility.ApplyColor(
            _backgroundColorImages,
            player.PlayerData,
            _backgroundColorTweenDuration,
            gameObject.activeInHierarchy,
            this);
    }

    public void HandleClickArtifact(SO_ArtifactData artifactData, ListItem_LevelUpArtifactSelect listItem)
    {
        _onArtifactSelectedCallback?.Invoke(artifactData);
        Close();
    }

    private void PlayRewardAppearSequence(List<RewardItemAppearAnimator> animators)
    {
        KillRewardAppearSequence();
        Canvas.ForceUpdateCanvases();

        if (_artifactItemParent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        _rewardAppearSequence = DOTween.Sequence().SetTarget(this);

        foreach (var animator in animators)
        {
            animator.PrepareAppear();
            _rewardAppearSequence.Append(animator.CreateAppearTween());
            _rewardAppearSequence.AppendInterval(RewardAppearInterval);
        }

        _rewardAppearSequence.OnComplete(() =>
        {
            foreach (var animator in animators)
            {
                animator.SetSelectable(true);
            }

            _rewardAppearSequence = null;
        });
    }

    private void KillRewardAppearSequence()
    {
        if (_rewardAppearSequence != null && _rewardAppearSequence.IsActive())
        {
            _rewardAppearSequence.Kill(false);
        }

        _rewardAppearSequence = null;
    }
}

