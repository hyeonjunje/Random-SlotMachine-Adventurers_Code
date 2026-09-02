using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_MainHud : UI_Base
{
    private const float ARTIFACT_TRIGGER_EFFECT_COOLDOWN = 0.18f;

    [Header("UpperBar")]
    [SerializeField] private Image _imageHeartFillAmount;
    [SerializeField] private TMP_Text _textHp;
    [SerializeField] private GameObject _objTimer;
    [SerializeField] private TMP_Text _textTimer;

    private Coroutine _coTimer;

    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private Transform _goldDestination; 
    [SerializeField] private Transform _goldParents;

    [SerializeField] private TMP_Text _slotMachineFailPercent;
    [SerializeField] private TMP_Text _slotMachineSuccessPercent;
    [SerializeField] private TMP_Text _slotMachineGreatSucessPercent;

    [Header("Left, Right Buttons")]
    [SerializeField] private Button _buttonLeft;
    [SerializeField] private TMP_Text _textLeft;
    [SerializeField] private Button _buttonRight;
    [SerializeField] private TMP_Text _textRight;

    [Header ("Artifacts")]
    [SerializeField] private Transform _artifactSlotParent;
    [SerializeField] private ListItem_Artifact _artifactItemPrefab;

    private List<ListItem_Artifact> _activeArtifactItems = new List<ListItem_Artifact> ();
    private Dictionary<Artifact, float> _lastArtifactTriggerEffectTimes = new Dictionary<Artifact, float> ();
    private IDisposable _onGoldChangedEvent;
    private IDisposable _onArtifactChangedEvent;
    private IDisposable _onArtifactTriggeredEvent;
    private IDisposable _onSlotMachineProbabilityChangedEvent;
    private bool _isGoldGainAnimating;
    private int _displayGold;
    private int _goldGainTarget;

    public bool IsExistRightButton => _buttonRight.gameObject.activeSelf;
    public bool IsExistLeftButton => _buttonLeft.gameObject.activeSelf;

    public override void Initialize()
    {
        base.Initialize();
        UpdateGoldUI (UIHudSystem.Instance.CurrentGold);
        UpdateSlotMachineProbabilityUI ();
    }

    private void OnEnable()
    {
        CharacterSystem.Instance.PartyHealth.OnChangeHp += OnUpdateHp;

        _onGoldChangedEvent = EventBus.Subscribe<StGoldChangedEvent>(OnGoldChanged);
        _onSlotMachineProbabilityChangedEvent =
            EventBus.Subscribe<StSlotMachineProbabilityChangedEvent>(OnSlotMachineProbabilityChanged);

        UpdateGoldUI (UIHudSystem.Instance.CurrentGold);
        UpdateSlotMachineProbabilityUI ();

        _onArtifactChangedEvent = EventBus.Subscribe<StArtifactChangedEvent> (OnArtifactChanged);
        _onArtifactTriggeredEvent = EventBus.Subscribe<StArtifactTriggeredEvent> (OnArtifactTriggered);

        RefreshArtifacts ();
        if (_coTimer != null) StopCoroutine(_coTimer);
        _coTimer = StartCoroutine(CoUpdateTimer());
        RefreshHpUIImmediately ();
    }

    private void OnDisable()
    {
        if (CharacterSystem.Instance != null && CharacterSystem.Instance.PartyHealth != null)
        {
            CharacterSystem.Instance.PartyHealth.OnChangeHp -= OnUpdateHp;
        }

        _onGoldChangedEvent?.Dispose ();
        _onArtifactChangedEvent?.Dispose ();
        _onArtifactTriggeredEvent?.Dispose ();
        _onSlotMachineProbabilityChangedEvent?.Dispose ();

        if (_coTimer != null)
        {
            StopCoroutine(_coTimer);
            _coTimer = null;
        }
    }

    public override void Open()
    {
        gameObject.SetActive(true);
        UpdateGoldUI (UIHudSystem.Instance.CurrentGold);
        UpdateSlotMachineProbabilityUI ();
        RefreshHpUIImmediately ();
    }

    public override void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnUpdateHp(int currentHp, int maxHp)
    {
        _imageHeartFillAmount.fillAmount = (float)currentHp / maxHp;
        _textHp.text = $"{currentHp}/{maxHp}";
    }

    private void OnArtifactChanged(StArtifactChangedEvent stArtifactChangedEvent)
    {
        RefreshArtifacts();
    }

    private void OnArtifactTriggered(StArtifactTriggeredEvent artifactTriggeredEvent)
    {
        Artifact triggeredArtifact = artifactTriggeredEvent.Artifact;
        if (triggeredArtifact?.Data == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (_lastArtifactTriggerEffectTimes.TryGetValue(triggeredArtifact, out float lastTime) &&
            now - lastTime < ARTIFACT_TRIGGER_EFFECT_COOLDOWN)
        {
            return;
        }

        ListItem_Artifact item = _activeArtifactItems.Find(activeItem =>
            activeItem != null && ReferenceEquals(activeItem.artifact, triggeredArtifact));

        if (item == null)
        {
            return;
        }

        _lastArtifactTriggerEffectTimes[triggeredArtifact] = now;
        item.PlayTriggerEffect();
    }

    private void CreateArtifactItem(Artifact artifact)
    {
        if (_artifactItemPrefab == null || _artifactSlotParent == null) return;

        ListItem_Artifact item = Instantiate (_artifactItemPrefab, _artifactSlotParent);

        item.SetListItem (artifact);

        _activeArtifactItems.Add (item);
    }

    private void RefreshArtifacts()
    {
        foreach (Transform child in _artifactSlotParent)
        {
            Destroy (child.gameObject);
        }

        _activeArtifactItems.Clear ();
        _lastArtifactTriggerEffectTimes.Clear ();

        foreach (var artifact in ArtifactSystem.Instance.OwnedArtifacts)
        {
            CreateArtifactItem (artifact);
        }
    }

    private void OnGoldChanged(StGoldChangedEvent goldEvent)
    {
        if (_isGoldGainAnimating && goldEvent.Delta > 0)
        {
            _goldGainTarget = goldEvent.CurrentGold;
            return;
        }

        UpdateGoldUI(goldEvent.CurrentGold);
    }

    private void OnSlotMachineProbabilityChanged(StSlotMachineProbabilityChangedEvent probabilityChangedEvent)
    {
        UpdateSlotMachineProbabilityUI();
    }

    private void UpdateGoldUI(int gold)
    {
        _displayGold = gold;
        _goldText.text = gold.ToString();
    }

    private void UpdateSlotMachineProbabilityUI()
    {
        if (DataManager.Instance == null || DataManager.Instance.GameModel == null)
        {
            return;
        }

        SO_GameModel model = DataManager.Instance.GameModel;
        float fail = Mathf.Clamp01(model.FailureProbability);
        float success = Mathf.Min(Mathf.Clamp01(model.SuccessProbability), Mathf.Max(0f, 1f - fail));
        float great = Mathf.Min(
            Mathf.Clamp01(model.GreatSuccessProbability * ArtifactRuntimeState.GreatSuccessProbabilityMultiplier),
            Mathf.Max(0f, 1f - fail - success));

        if (_slotMachineFailPercent != null)
        {
            _slotMachineFailPercent.text = FormatPercent(fail);
        }

        if (_slotMachineSuccessPercent != null)
        {
            _slotMachineSuccessPercent.text = FormatPercent(success);
        }

        if (_slotMachineGreatSucessPercent != null)
        {
            _slotMachineGreatSucessPercent.text = FormatPercent(great);
        }
    }

    private static string FormatPercent(float value)
    {
        return $"{value * 100f:0.#}%";
    }

    public void ClickLeftButton()
    {
        _buttonLeft.onClick?.Invoke();
    }

    public void SetLeftButton(Action action, string text)
    {
        _buttonLeft.gameObject.SetActive(true);

        _buttonLeft.onClick.RemoveAllListeners();
        _buttonLeft.onClick.AddListener(() => action());
        _textLeft.text = text;
    }

    public void ShowLeftButton()
    {
        _buttonLeft.gameObject.SetActive(true);
    }

    public void HideLeftButton()
    {
        _buttonLeft.gameObject.SetActive(false);
    }

    public void ClickRightButton()
    {
        _buttonRight.onClick?.Invoke();
    }

    public void SetRightButton(Action action, string text)
    {
        _buttonRight.gameObject.SetActive(true);

        _buttonRight.onClick.RemoveAllListeners();
        _buttonRight.onClick.AddListener(() => action());
        _textRight.text = text;
    }

    public void ShowRightButton()
    {
        _buttonRight.gameObject.SetActive(true);
    }

    public void HideRightButton()
    {
        _buttonRight.gameObject.SetActive(false);
    }

    public void PlayGoldGainEffect(int amount, Vector3 startPos)
    {
        if (amount <= 0)
        {
            return;
        }

        _isGoldGainAnimating = true;
        _goldGainTarget = UIHudSystem.Instance.CurrentGold + amount;
        _displayGold = _goldGainTarget - amount;
        _goldText.text = _displayGold.ToString();

        StartCoroutine(Co_PlayGoldGainEffect(amount, startPos));
    }

    public IEnumerator Co_PlayGoldGainEffect(int amount, Vector3 startPos)
    {
        if (amount <= 0)
        {
            yield break;
        }

        AudioManager.Instance.PlaySFX(ESfxId.Gain_Money);

        int coinCount = Mathf.Clamp (amount, 1, 30);
        int goldPerCoin = amount / coinCount;
        int remain = amount % coinCount;

        int activeCoins = 0; 
        float spawnInterval = 0.05f;

        for (int i = 0; i < coinCount; i++)
        {
            RectTransform coin = Creator.Instance.CreatAsset<RectTransform> (CreateAssetName.Coin);
            if (coin == null) yield break;

            activeCoins++; 
            coin.SetParent (_goldParents, false);
            coin.position = startPos;
            coin.localScale = Vector3.zero;

            PlayIndividualCoinAnim (coin, i, coinCount, goldPerCoin, remain, () => {
                activeCoins--; 
            });

            yield return new WaitForSeconds (spawnInterval);
        }

        while (activeCoins > 0)
        {
            yield return null;
        }

        _isGoldGainAnimating = false;
        UpdateGoldUI(_goldGainTarget);

        yield return new WaitForSeconds (0.2f);
    }

    private void PlayIndividualCoinAnim(RectTransform coin, int index, int totalCount, int goldPerCoin, int remain, System.Action onComplete)
    {
        Sequence seq = DOTween.Sequence ();
        bool isFinished = false;

        Vector3 spreadPos = coin.position + (Vector3)UnityEngine.Random.insideUnitCircle * 150f;
        float jumpDuration = 0.3f;

        seq.Append (coin.DOScale (1.2f, jumpDuration).SetEase (Ease.OutBack));
        seq.Join (coin.DOMove (spreadPos, jumpDuration).SetEase (Ease.OutQuad));
        seq.Join (coin.DORotate (new Vector3 (0, 0, 360f), jumpDuration, RotateMode.FastBeyond360));

        float flyDelay = UnityEngine.Random.Range (0.1f, 0.3f);
        float moveDuration = 0.5f;

        seq.AppendInterval (flyDelay);
        seq.Append (coin.DOMove (_goldDestination.position, moveDuration).SetEase (Ease.InBack));
        seq.Join (coin.DOScale (0.4f, moveDuration));
        seq.Join (coin.DORotate (new Vector3 (0, 0, 720f), moveDuration, RotateMode.FastBeyond360));

        void FinishCoin(bool applyReward)
        {
            if (isFinished)
            {
                return;
            }

            isFinished = true;

            if (applyReward)
            {
                int addValue = goldPerCoin + (index == totalCount - 1 ? remain : 0);
                UpdateGoldText (addValue);

                _goldDestination.DOKill ();
                _goldDestination.localScale = Vector3.one;
                _goldDestination.DOPunchScale (Vector3.one * 0.2f, 0.1f);
            }

            if (coin != null)
            {
                Creator.Instance.RemoveAsset (CreateAssetName.Coin, coin.gameObject);
            }

            onComplete?.Invoke ();
        }

        seq.OnComplete (() => {
            FinishCoin(true);
        });

        seq.OnKill(() =>
        {
            FinishCoin(false);
        });
    }

    private void UpdateGoldText(int addValue)
    {
        _displayGold += addValue;
        _goldText.text = _displayGold.ToString();

        _goldText.transform.DOKill ();
        _goldText.transform.localScale = Vector3.one;
        _goldText.transform.DOPunchScale (Vector3.one * 0.15f, 0.1f);
    }
    public void RefreshHpUIImmediately()
    {
        if (CharacterSystem.Instance == null || CharacterSystem.Instance.PartyHealth == null)
        {
            _imageHeartFillAmount.fillAmount = 0f;
            _textHp.text = "0/0";
            return;
        }

        int currentHp = CharacterSystem.Instance.PartyHealth.CurrentHp;
        int maxHp = CharacterSystem.Instance.PartyHealth.MaxHp;

        if (maxHp <= 0)
        {
            _imageHeartFillAmount.fillAmount = 0f;
            _textHp.text = "0/0";
            return;
        }

        OnUpdateHp (currentHp, maxHp);
    }


    #region UIEvent
    public void OnClickKeyword()
    {
        UI_MyKeywords myKeywordUI = UIManager.Instance.Get<UI_MyKeywords>(EUIType.UI_MyKeywords);

        if (myKeywordUI.gameObject.activeSelf)
        {
            if (myKeywordUI.IsSelectMode)
            {
                return;
            }

            UIManager.Instance.Close(EUIType.UI_MyKeywords);
        }
        else
        {
            UIManager.Instance.Open(EUIType.UI_MyKeywords);
        }
    }

    public void OnClickPause()
    {
        UI_Pause uiPause = UIManager.Instance.Get<UI_Pause>(EUIType.UI_Pause);

        if (uiPause.gameObject.activeSelf)
        {
            UIManager.Instance.Close(EUIType.UI_Pause);
        }
        else
        {
            UIManager.Instance.Open(EUIType.UI_Pause);

        }
    }
    #endregion
    
    private IEnumerator CoUpdateTimer()
    {
        while (true)
        {
            // 설정에서 타이머 보기를 켰는지 확인 (SettingsManager 연동 로직 추가)
            bool isShowTimer = SettingsManager.Instance != null ? SettingsManager.Instance.ShowTimer : true;

            if (_objTimer.activeSelf != isShowTimer)
            {
                _objTimer.SetActive(isShowTimer);
            }

            if (isShowTimer)
            {
                float currentElapsed = DataManager.Instance != null ? DataManager.Instance.GameModel.ElapsedTime : 0f;
                int minutes = Mathf.FloorToInt(currentElapsed / 60f);
                int seconds = Mathf.FloorToInt(currentElapsed % 60f);
                _textTimer.text = $"{minutes:D2}:{seconds:D2}";
            }

            yield return null;
        }
    }
}
