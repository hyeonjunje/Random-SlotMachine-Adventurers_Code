using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterIllustPanel : MonoBehaviour
{
    [SerializeField] private RectTransform _shutterContent;      
    [SerializeField] private Image _illustImage;                 
    [SerializeField] private CanvasGroup _infoOverlay;          
    [SerializeField] private TMP_Text _textName;
    [SerializeField] private TMP_Text _textJob;
    [SerializeField] private TMP_Text _textATK;
    [SerializeField] private TMP_Text _textHP;
    [SerializeField] private TMP_Text _textLore;
    [SerializeField] private Transform _starterArtifactParent;   
    [SerializeField] private GameObject _emptyState;             

    [Header("----- 유물 프리팹 -----")]
    [SerializeField] private ListItem_Artifact _artifactPrefab;

    [Header("----- 연출 설정 -----")]
    [SerializeField] private float _shutterDuration = 0.35f;
    [SerializeField] private float _impactShakeDuration = 0.15f;
    [SerializeField] private float _impactShakeStrength = 15f;
    [SerializeField] private float _infoFadeDuration = 0.3f;
    [SerializeField] private float _infoFadeDelay = 0.2f;

    private RectTransform _panelRect;
    private ListItem_Artifact _currentArtifactItem;
    private Player _currentPlayer;
    private Sequence _currentSequence;

    public Player CurrentPlayer => _currentPlayer;
    public bool IsRevealed => _currentPlayer != null;
    public bool IsAnimating { get; private set; }

    private void Awake()
    {
        _panelRect = GetComponent<RectTransform>();
    }

    public void SetEmpty()
    {
        KillCurrentSequence();

        _currentPlayer = null;
        _emptyState.SetActive(true);
        _illustImage.gameObject.SetActive(false);
        _infoOverlay.alpha = 0f;
        _infoOverlay.interactable = false;
        _infoOverlay.blocksRaycasts = false;
        _infoOverlay.gameObject.SetActive(false);

        float panelHeight = _panelRect.rect.height;
        _shutterContent.anchoredPosition = new Vector2(0, panelHeight);

        ClearArtifactItem();
    }
    public void PlayShutterReveal(Player player, SO_ArtifactData starterArtifact, System.Action onComplete = null)
    {
        KillCurrentSequence();

        _currentPlayer = player;
        IsAnimating = true;

        // 데이터 세팅
        _emptyState.SetActive(false);
        _illustImage.gameObject.SetActive(true);
        _illustImage.sprite = SpriteManager.Instance.GetSprite(player.PlayerData.IllustrationIconName);

        string characterName = LocalizationManager.Instance.Get(player.PlayerData.SubjectKeyword.ToString());
        string jobName = LocalizationManager.Instance.Get(GetJobLocalizationKey(player.PlayerData.PlayerJob));

        if (_textName != null)
        {
            _textName.text = _textJob == null
                ? $"{characterName}\n<size=70%>{jobName}</size>"
                : characterName;
        }

        if (_textJob != null)
        {
            _textJob.text = jobName;
        }

        _textATK.text = $"ATK {player.GetStat(EStatType.AttackPower).Value}";
        _textHP.text = $"HP {player.GetStat(EStatType.MaxHp).Value}";
        _textLore.text = LocalizationManager.Instance.Get(player.PlayerData.CharacterLore);

        // 유물 세팅
        SetupStarterArtifact(player, starterArtifact);

        // 정보 오버레이 초기화
        _infoOverlay.gameObject.SetActive(true);
        _infoOverlay.alpha = 0f;

        // 셔터 시퀀스
        float panelHeight = _panelRect.rect.height;
        _shutterContent.anchoredPosition = new Vector2(0, panelHeight);

        _currentSequence = DOTween.Sequence();

        _currentSequence.Append(
            _shutterContent.DOAnchorPosY(0, _shutterDuration)
                .SetEase(Ease.InQuad)
        );

        _currentSequence.Append(
            _shutterContent.DOShakeAnchorPos(
                _impactShakeDuration,
                new Vector2(0, _impactShakeStrength),
                vibrato: 10,
                randomness: 90,
                snapping: false,
                fadeOut: true,
                randomnessMode: ShakeRandomnessMode.Harmonic
            )
        );

        // 셔터 + 쉐이크 끝난 뒤 약간 딜레이 후 Info 페이드인
        _currentSequence.AppendInterval(_infoFadeDelay);
        _currentSequence.Append(
            _infoOverlay.DOFade(1f, _infoFadeDuration)
        );

        _currentSequence.OnComplete(() =>
        {
            IsAnimating = false;
            _currentSequence = null;

            // 유물 호버 툴팁 등 포인터 이벤트 활성화
            _infoOverlay.interactable = true;
            _infoOverlay.blocksRaycasts = true;

            onComplete?.Invoke();
        });
    }

    private void SetupStarterArtifact(Player player, SO_ArtifactData starterArtifactData)
    {
        ClearArtifactItem();

        if (starterArtifactData == null) return;

        _currentArtifactItem = Instantiate(_artifactPrefab, _starterArtifactParent);
        Artifact tempArtifact = new Artifact(starterArtifactData, player);
        _currentArtifactItem.SetListItem(tempArtifact);
    }

    private string GetJobLocalizationKey(EPlayerJob job)
    {
        return job switch
        {
            EPlayerJob.Warrior => "JOB_WARRIOR",
            EPlayerJob.Dwarf => "JOB_DWARF",
            EPlayerJob.Archer => "JOB_ARCHER",
            EPlayerJob.Priest => "JOB_PRIEST",
            EPlayerJob.Rogue => "JOB_ROGUE",
            _ => string.Empty
        };
    }

    private void ClearArtifactItem()
    {
        if (_currentArtifactItem != null)
        {
            Destroy(_currentArtifactItem.gameObject);
            _currentArtifactItem = null;
        }
    }
    private void KillCurrentSequence()
    {
        if (_currentSequence != null && _currentSequence.IsActive())
        {
            _currentSequence.Kill(complete: false);
            _currentSequence = null;
        }
        IsAnimating = false;
    }

    private void OnDestroy()
    {
        KillCurrentSequence();
    }
}
