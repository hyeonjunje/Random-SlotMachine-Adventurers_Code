using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public enum ERewardType
{
    Normal,  
    Special  
}

public class BattleRewardData
{
    public ERewardType RewardType;

    [Header ("일반 전투용 (1티어 단어)")]
    public List<SO_KeywordData> Keywords;

    [Header ("특수 보상용")]
    public List<SO_ArtifactData> Artifacts;
}

public class UI_Reward : UI_Base
{
    private const float RewardAppearInterval = 0.06f;

    [SerializeField] private GameObject _normalRewardPanel;
    [SerializeField] private GameObject _specialRewardPanel;

    [Header ("단어보상")]
    [SerializeField] private Transform _keywordItemParent;
    [SerializeField] private ListItem_KeywordReward _keywordItemPrefab;

    [Header ("유물 보상")]
    [SerializeField] private Transform _specialRewardParent;
    [SerializeField] private ListItem_ArtifactReward _artifactPrefab;

    private BattleRewardData _currentData;
    private Sequence _rewardAppearSequence;
    private bool _isRewardClaimed;

    public void SetReward(BattleRewardData data)
    {
        _currentData = data;
    }

    public override void Open()
    {
        gameObject.SetActive (true);
        _isRewardClaimed = false;

        KillRewardAppearSequence();
        _keywordItemParent.DestroyAllChildren ();
        _specialRewardParent.DestroyAllChildren (); // 특수 보상 부모도 초기화

        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud> (EUIType.UI_MainHud);

        mainHUD.SetRightButton(() =>
        {
            mainHUD.HideRightButton();

            // 클리어
            ClearNodeGA clearNode = new ClearNodeGA();
            ActionSystem.Instance.Perform(clearNode);
        }, LocalizationManager.Instance.Get("CS_UI_REWARD_081"));

        if (_currentData.RewardType == ERewardType.Normal)
        {
            ShowKeywordPanel ();
        }
        else
        {
            ShowSpecialPanel ();
        }
    }

    public override void Close()
    {
        _isRewardClaimed = false;
        KillRewardAppearSequence();
        _keywordItemParent.DestroyAllChildren ();
        _specialRewardParent.DestroyAllChildren ();
        gameObject.SetActive (false);
    }

    public bool TryBeginClaimReward()
    {
        if (_isRewardClaimed)
        {
            return false;
        }

        _isRewardClaimed = true;
        SetRewardItemsSelectable(false);
        return true;
    }

    // 일반 보상 
    private void ShowKeywordPanel()
    {
        _normalRewardPanel.SetActive (true);
        _specialRewardPanel.SetActive (false);

        if (_currentData.Keywords != null)
        {
            List<RewardItemAppearAnimator> animators = new List<RewardItemAppearAnimator>();

            foreach (var keywordData in _currentData.Keywords)
            {
                var item = Instantiate (_keywordItemPrefab, _keywordItemParent);
                item.SetListItem(keywordData);
                animators.Add(item.GetRewardItemAppearAnimator());
            }

            PlayRewardAppearSequence(_keywordItemParent, animators);
        }
    }

    // 특수 보상
    private void ShowSpecialPanel()
    {
        _normalRewardPanel.SetActive (false);
        _specialRewardPanel.SetActive (true);

        _specialRewardParent.DestroyAllChildren ();

        if (_currentData.Artifacts == null)
        {
            return;
        }

        List<RewardItemAppearAnimator> animators = new List<RewardItemAppearAnimator>();

        foreach (var artifactData in _currentData.Artifacts)
        {
            var item = Instantiate (_artifactPrefab, _specialRewardParent);
            item.SetListItem(artifactData);
            animators.Add(item.GetRewardItemAppearAnimator());
        }

        PlayRewardAppearSequence(_specialRewardParent, animators);
    }

    private void PlayRewardAppearSequence(Transform parent, List<RewardItemAppearAnimator> animators)
    {
        Canvas.ForceUpdateCanvases();

        if (parent is RectTransform parentRect)
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
            if (_isRewardClaimed == false)
            {
                foreach (var animator in animators)
                {
                    animator.SetSelectable(true);
                }
            }

            _rewardAppearSequence = null;
        });
    }

    private void SetRewardItemsSelectable(bool isSelectable)
    {
        foreach (RewardItemAppearAnimator animator in GetComponentsInChildren<RewardItemAppearAnimator>(true))
        {
            animator.SetSelectable(isSelectable);
        }
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

public class RewardItemAppearAnimator : MonoBehaviour
{
    [SerializeField] private float _startOffsetY = 120f;
    [SerializeField] private float _overshootY = 22f;
    [SerializeField] private float _riseDuration = 0.22f;
    [SerializeField] private float _settleDuration = 0.12f;
    [SerializeField] private Ease _riseEase = Ease.OutCubic;
    [SerializeField] private Ease _settleEase = Ease.InCubic;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _targetPosition;
    private Vector3 _targetScale;

    public bool IsSelectable { get; private set; } = true;

    public void PrepareAppear()
    {
        Initialize();

        _targetPosition = _rectTransform.anchoredPosition;
        _targetScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        SetSelectable(false);
        _canvasGroup.alpha = 0f;
        _rectTransform.anchoredPosition = _targetPosition + Vector2.down * _startOffsetY;
        transform.localScale = _targetScale * 0.96f;
    }

    public Tween CreateAppearTween()
    {
        Initialize();

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        sequence.Append(
            _rectTransform.DOAnchorPos(_targetPosition + Vector2.up * _overshootY, _riseDuration)
                .SetEase(_riseEase));
        sequence.Join(_canvasGroup.DOFade(1f, _riseDuration * 0.8f).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOScale(_targetScale, _riseDuration).SetEase(Ease.OutBack));
        sequence.Append(
            _rectTransform.DOAnchorPos(_targetPosition, _settleDuration)
                .SetEase(_settleEase));

        return sequence;
    }

    public void SetSelectable(bool isSelectable)
    {
        Initialize();

        IsSelectable = isSelectable;
        _canvasGroup.blocksRaycasts = isSelectable;
        _canvasGroup.interactable = isSelectable;
    }

    private void Initialize()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
    }
}

