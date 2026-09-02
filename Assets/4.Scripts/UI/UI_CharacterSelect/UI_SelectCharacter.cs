using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectCharacter : UI_Base
{
    [Header("----- 리스트 아이템 -----")]
    [SerializeField] private ListItem_CharacterSelection _listItemPrefab;
    [SerializeField] private Transform _listItemParent;
    [SerializeField] private TMP_Text _textPlayerCnt;
    [SerializeField] private Image _illustBack;
    [SerializeField] private Image _illustfront;
    [SerializeField] private Transform _skeletonGraphicParent;
    [SerializeField] private TMP_Text _textName;
    [SerializeField] private TMP_Text _textJob;
    [SerializeField] private TMP_Text _textHP;
    [SerializeField] private TMP_Text _textATK;
    [SerializeField] private TMP_Text _textLore;
    [SerializeField] private Transform _starterArtifactParent;
    [SerializeField] private ListItem_Artifact _artifactPrefab;
    [SerializeField] private Image[] _selectedCharacterIcons;
    [Header("----- Job Background Color -----")]
    [SerializeField] private Transform _backgroundColorRoot;
    [SerializeField] private Image[] _backgroundColorImagesInOrder;
    [SerializeField] private float _backgroundColorTweenDuration = 0.25f;

    private List<ListItem_CharacterSelection> _listItemCharacterSelections = new List<ListItem_CharacterSelection>();
    private readonly List<Image> _backgroundColorImages = new List<Image>();
    private int _currentPhase = 0;                      
    private Player[] _selectedPlayers = new Player[3];
    private SO_ArtifactData[] _selectedStarterArtifacts = new SO_ArtifactData[3];
    private Dictionary<Player, SO_ArtifactData> _starterArtifactByCandidate = new Dictionary<Player, SO_ArtifactData>();
    private ListItem_Artifact _currentArtifactItem;

    public override void Open()
    {
        AudioManager.Instance.PlayBGM(EBgmId.CharacterSelect);

        gameObject.SetActive(true);
        _listItemParent.gameObject.SetActive(true);

        UIManager.Instance.Close(EUIType.UI_MainHud);

        _currentPhase = 0;
        _selectedPlayers = new Player[3];
        _selectedStarterArtifacts = new SO_ArtifactData[3];

        UpdatePhaseText();
        ClearSelectedCharacterIcons();
        ShowSelection();
    }

    public override void Close()
    {
        JobBackgroundColorUtility.KillTweens(_backgroundColorImages);
        UIManager.Instance.HideCharacterGuide();

        _listItemParent.DestroyAllChildren();
        _listItemCharacterSelections.Clear();
        ClearStarterArtifact();
        ClearSelectedCharacterIcons();

        gameObject.SetActive(false);
    }

    public void OnClickCharacter(Player player)
    {
        if (_currentPhase >= 3) return;

        _selectedPlayers[_currentPhase] = player;

        HighlightSelectedListItem(player);
        RefreshCurrentCharacter(player);

        SO_ArtifactData starterArtifact = GetOrCreateStarterArtifactFor(player);
        _selectedStarterArtifacts[_currentPhase] = starterArtifact;
        RefreshStarterArtifact(player, starterArtifact);
    }

    public void OnClickComplete()
    {
        if (_selectedPlayers[_currentPhase] == null) return;

        AddSelectedCharacterIcon(_selectedPlayers[_currentPhase]);

        _currentPhase++;

        UpdatePhaseText();

        if (_currentPhase >= 3)
        {
            FinalizeSelection();
        }
        else
        {
            ShowSelection();
        }
    }

    private void FinalizeSelection()
    {
        StartCoroutine(CoFinalizeSelection());
    }

    private IEnumerator CoFinalizeSelection()
    {
        for (int i = 0; i < 3; i++)
        {
            Player player = _selectedPlayers[i];

            DataManager.Instance.GameModel.SubjectKeywords.Add(player.PlayerData.SubjectKeyword);

            bool done = false;
            SpawnPlayerGA spawnPlayerGA = new SpawnPlayerGA(player);
            ActionSystem.Instance.Perform(spawnPlayerGA, () => done = true);
            yield return new WaitUntil(() => done);

            SO_ArtifactData starterArtifact = _selectedStarterArtifacts[i];
            if (starterArtifact != null)
            {
                ArtifactSystem.Instance.AddArtifact(starterArtifact.ID, player);
            }
        }

        _listItemParent.gameObject.SetActive(false);

        UI_Event uiEvent = UIManager.Instance.Get<UI_Event>(EUIType.UI_Event);
        uiEvent.Setup(DataManager.Instance.StartEvent);

        UIManager.Instance.Open(EUIType.UI_Event);
        UIManager.Instance.Close(EUIType.UI_SelectCharacter);

        // 캐릭터 선택 완료 후 MainHud 표시
        UIManager.Instance.Open(EUIType.UI_MainHud);
        UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);
        mainHUD.HideRightButton();
    }

    private void ShowSelection()
    {
        _starterArtifactByCandidate.Clear();

        // 리스트 아이템 3개 확보
        while (_listItemCharacterSelections.Count < 3)
        {
            _listItemCharacterSelections.Add(Instantiate(_listItemPrefab, _listItemParent));
        }

        // 이미 선택 확정된 캐릭터는 제외
        List<SO_PlayerData> excepts = new List<SO_PlayerData>();
        for (int i = 0; i < _currentPhase; i++)
        {
            if (_selectedPlayers[i] != null)
                excepts.Add(_selectedPlayers[i].PlayerData);
        }

        // 기존 CharacterSystem에 등록된 것도 제외
        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            excepts.Add(playerView.Player.PlayerData);
        }

        // 랜덤 3명 뽑기
        List<SO_PlayerData> pool = new List<SO_PlayerData>();
        for (int i = 0; i < 3; ++i)
        {
            SO_PlayerData playerData = DataManager.Instance.AllPlayers.GetRandomElement(excepts);
            pool.Add(playerData);
            excepts.Add(playerData);
        }

        // ListItem에 세팅
        for (int i = 0; i < 3; ++i)
        {
            Player player = new Player(pool[i]);
            _listItemCharacterSelections[i].SetListItem(player);
        }

        if (_listItemCharacterSelections.Count > 0)
        {
            OnClickCharacter(_listItemCharacterSelections[0].Item);
        }
    }

    private void UpdatePhaseText()
    {
        _textPlayerCnt.text = $"({_currentPhase}/3)";
    }

    private SO_ArtifactData GetOrCreateStarterArtifactFor(Player player)
    {
        if (player == null)
        {
            return null;
        }

        if (_starterArtifactByCandidate.TryGetValue(player, out SO_ArtifactData starterArtifact))
        {
            return starterArtifact;
        }

        starterArtifact = ArtifactSystem.Instance.GetStarterArtifactFor(player, GetSelectedStarterArtifactIds());
        _starterArtifactByCandidate.Add(player, starterArtifact);
        return starterArtifact;
    }

    private HashSet<EArtifactId> GetSelectedStarterArtifactIds()
    {
        HashSet<EArtifactId> selectedArtifactIds = new HashSet<EArtifactId>();

        for (int i = 0; i < _currentPhase; i++)
        {
            SO_ArtifactData selectedArtifact = _selectedStarterArtifacts[i];
            if (selectedArtifact != null)
            {
                selectedArtifactIds.Add(selectedArtifact.ID);
            }
        }

        return selectedArtifactIds;
    }

    private void HighlightSelectedListItem(Player selectedPlayer)
    {
        foreach (var listItem in _listItemCharacterSelections)
        {
            listItem.SetSelected(listItem.Item == selectedPlayer);
        }
    }

    private void RefreshCurrentCharacter(Player player)
    {
        if (player == null) return;

        Sprite illustrationSprite = SpriteManager.Instance.GetSprite(player.PlayerData.IllustrationName);
        if (_illustBack != null)
        {
            _illustBack.sprite = illustrationSprite;
            _illustBack.rectTransform.anchoredPosition = player.PlayerData.SelectionBackgroundIllustrationOffset;
        }

        if (_illustfront != null)
        {
            _illustfront.sprite = illustrationSprite;
        }

        RefreshSkeletonGraphic(player);
        RefreshCharacterInfo(player);
        RefreshJobBackgroundColors(player);
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

    private void RefreshSkeletonGraphic(Player player)
    {
        if (_skeletonGraphicParent == null) return;

        _skeletonGraphicParent.DestroyAllChildren();

        if (player.PlayerData.CharacterSkeletonGraphic == null) return;

        GameObject skeletonObject = Instantiate(player.PlayerData.CharacterSkeletonGraphic, _skeletonGraphicParent, false);
        RectTransform skeletonRectTransform = skeletonObject.transform as RectTransform;

        if (skeletonRectTransform != null)
        {
            skeletonRectTransform.anchoredPosition = Vector2.zero;
            skeletonRectTransform.localRotation = Quaternion.identity;
            skeletonRectTransform.localScale = Vector3.one;
        }
        else
        {
            skeletonObject.transform.localPosition = Vector3.zero;
            skeletonObject.transform.localRotation = Quaternion.identity;
            skeletonObject.transform.localScale = Vector3.one;
        }
    }

    private void RefreshCharacterInfo(Player player)
    {
        if (player == null) return;

        if (_textName != null)
        {
            string characterName = LocalizationManager.Instance.Get(player.PlayerData.SubjectKeyword.ToString());
            if (_textJob == null)
            {
                string jobName = LocalizationManager.Instance.Get(GetJobLocalizationKey(player.PlayerData.PlayerJob));
                _textName.text = $"{characterName}\n<size=70%>{jobName}</size>";
            }
            else
            {
                _textName.text = characterName;
            }
        }

        if (_textJob != null)
        {
            _textJob.text = LocalizationManager.Instance.Get(GetJobLocalizationKey(player.PlayerData.PlayerJob));
        }

        if (_textHP != null)
        {
            _textHP.text = $"HP {player.GetStat(EStatType.MaxHp).Value}";
        }

        if (_textATK != null)
        {
            _textATK.text = $"ATK {player.GetStat(EStatType.AttackPower).Value}";
        }

        if (_textLore != null)
        {
            _textLore.text = LocalizationManager.Instance.Get(player.PlayerData.CharacterLore);
        }
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

    private void RefreshStarterArtifact(Player player, SO_ArtifactData starterArtifact)
    {
        ClearStarterArtifact();

        if (_starterArtifactParent == null) return;
        if (_artifactPrefab == null) return;
        if (starterArtifact == null) return;

        _currentArtifactItem = Instantiate(_artifactPrefab, _starterArtifactParent);
        Artifact tempArtifact = new Artifact(starterArtifact, player);
        _currentArtifactItem.SetListItem(tempArtifact);
    }

    private void ClearStarterArtifact()
    {
        if (_currentArtifactItem == null) return;

        Destroy(_currentArtifactItem.gameObject);
        _currentArtifactItem = null;
    }

    private void AddSelectedCharacterIcon(Player player)
    {
        if (player == null) return;
        if (_selectedCharacterIcons == null) return;
        if (_currentPhase < 0 || _currentPhase >= _selectedCharacterIcons.Length) return;

        Image selectedIcon = _selectedCharacterIcons[_currentPhase];
        if (selectedIcon == null) return;

        selectedIcon.sprite = SpriteManager.Instance.GetSprite(player.PlayerData.PortraitIconName);
        selectedIcon.gameObject.SetActive(true);
    }

    private void ClearSelectedCharacterIcons()
    {
        if (_selectedCharacterIcons == null) return;

        for (int i = 0; i < _selectedCharacterIcons.Length; i++)
        {
            if (_selectedCharacterIcons[i] != null)
            {
                _selectedCharacterIcons[i].sprite = null;
                _selectedCharacterIcons[i].gameObject.SetActive(false);
            }
        }
    }
}
