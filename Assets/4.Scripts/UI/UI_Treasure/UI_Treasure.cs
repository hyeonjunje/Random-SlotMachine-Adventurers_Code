using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;    

public class UI_Treasure : UI_Base
{
    [SerializeField] private Button _chestButton;         
    [SerializeField] private GameObject _closedChestImage; 
    [SerializeField] private GameObject _openedChestImage;

    [Header ("Glow Effect")]
    [SerializeField] private Image _glowImage;
    [SerializeField] private float _glowDuration = 1.0f;
    [SerializeField] private Color _glowColor = new Color (1f, 0.8f, 0f, 1f);

    private IDisposable _onEnterTreasureNodeEvent;
    private bool _isChestOpened = false;
    private TreasureCheckpoint _savedCheckpoint;

    public override void Initialize()
    {
        base.Initialize ();
        _onEnterTreasureNodeEvent = EventBus.Subscribe<StEnterTreasureNodeEvent> (OnEnterTreasureNodeEvent);

        _chestButton.onClick.AddListener (OnClickChest);
    }

    protected override void Dispose()
    {
        base.Dispose ();
        _onEnterTreasureNodeEvent?.Dispose ();
        _chestButton.onClick.RemoveListener (OnClickChest);
        StopGlowEffect ();
    }

    private void OnEnterTreasureNodeEvent(StEnterTreasureNodeEvent evt)
    {
        Open ();
    }

    public override void Open()
    {
        gameObject.SetActive (true);
        ResetChestState ();
        PlayGlowEffect ();
    }

    public override void Close()
    {
        StopGlowEffect ();
        gameObject.SetActive (false);
        UIManager.Instance.Close (EUIType.UI_Treasure);
    }

    private void ResetChestState()
    {
        _isChestOpened = false;
        _chestButton.interactable = true;
        _closedChestImage.SetActive (true);
        _openedChestImage.SetActive (false);
    }

    private void OnClickChest()
    {
        if (_isChestOpened) return; 

        StartCoroutine (Co_OpenChestSequence ());
    }

    private IEnumerator Co_OpenChestSequence()
    {
        _isChestOpened = true;
        _chestButton.interactable = false;

        StopGlowEffect ();

        yield return StartCoroutine (UIManager.Instance.FadeOut (0.5f));

        _closedChestImage.SetActive (false);
        _openedChestImage.SetActive (true);


        yield return StartCoroutine (UIManager.Instance.FadeIn (0.5f));
        yield return new WaitForSeconds (1f);
        ShowTreasureReward ();
    }

    private void PlayGlowEffect()
    {
        _glowImage.gameObject.SetActive (true);

        Color startColor = _glowColor;
        startColor.a = 0.2f; 
        _glowImage.color = startColor;

        _glowImage.DOFade (1.0f, _glowDuration)
            .SetLoops (-1, LoopType.Yoyo) 
            .SetEase (Ease.InOutSine);    
    }

    private void StopGlowEffect()
    {
        _glowImage.DOKill ();
        _glowImage.transform.DOKill ();

        _glowImage.gameObject.SetActive (false);
    }

    private void ShowTreasureReward()
    {
        BattleRewardData rewardData = new BattleRewardData ();
        rewardData.RewardType = ERewardType.Special;
        rewardData.Artifacts = new List<SO_ArtifactData> ();
        rewardData.Artifacts = ArtifactSystem.Instance.GetRandomRewardArtifacts (3);

        var rewardUI = UIManager.Instance.Get<UI_Reward> (EUIType.UI_Reward);

        rewardUI.SetReward (rewardData);
        UIManager.Instance.Open(EUIType.UI_Reward);
    }
    public void OpenFromSave(TreasureCheckpoint checkpoint)
    {
        _savedCheckpoint = checkpoint;
        Open ();
    }
}
