using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterSystem : SingletonScene<CharacterSystem>
{
    [SerializeField] private Transform _characterParent;

    private List<PlayerView> _players = new List<PlayerView>(GameDefine.MAXPLAYERCOUNT);

    private List<EnemyView> _enemies = new List<EnemyView>();
    public IReadOnlyList<PlayerView> Players => _players;
    public IReadOnlyList<EnemyView> Enemies => _enemies;

    public GhostView GhostView { get; private set; }

    public HealthController PartyHealth { get; private set; }
    public StatusController PartyStatusController { get; private set; }

    private void OnEnable()
    {
        PartyHealth = new HealthController(0);
        PartyStatusController = new StatusController();

        GhostView = Creator.Instance.CreatAsset<GhostView>(CreateAssetName.GhostView);
        GhostView.transform.SetParent(_characterParent, false);
        GhostView.Init();

        ActionSystem.AttachPerformer<SpawnPlayerGA>(SpawnPlayerPerformer);
        ActionSystem.AttachPerformer<SpawnEnemyGA>(SpawnEnemyPerformer);

        ActionSystem.AttachPerformer<DespawnPlayerGA>(DespawnPlayerPerformer);

        ActionSystem.AttachPerformer<EnemyDeadGA>(EnemyDeadPerformer);
        ActionSystem.AttachPerformer<MergeCharacterGA>(MergeCharacterPerformer);

        ActionSystem.AttachPerformer<LevelUpPlayerGA>(LevelUpPlayerPerformer);
        ActionSystem.AttachPerformer<LevelUpPartyGA> (LevelUpPartyPerformer);
        ActionSystem.AttachPerformer<GainJobArtifactGA> (GainJobArtifactPerformer);

        ActionSystem.AttachPerformer<ChangeEnemyActCountGA>(ChangeEnemyActCountPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<SpawnPlayerGA>();
        ActionSystem.DetachPerformer<SpawnEnemyGA>();

        ActionSystem.DetachPerformer<DespawnPlayerGA>();

        ActionSystem.DetachPerformer<EnemyDeadGA>();
        ActionSystem.DetachPerformer<MergeCharacterGA>();

        ActionSystem.DetachPerformer<LevelUpPlayerGA>();
        ActionSystem.DetachPerformer<LevelUpPartyGA> ();
        ActionSystem.DetachPerformer<GainJobArtifactGA> ();

        ActionSystem.DetachPerformer<ChangeEnemyActCountGA>();
    }

    public PlayerView GetPlayer(int index)
    {
        if(index >= _players.Count)
        {
            return null;
        }
        return _players[index];
    }
    public void RegisterPlayer(PlayerView playerView)
    {
        _players.Add (playerView);
    }

    private IEnumerator SpawnPlayerPerformer(SpawnPlayerGA spawnPlayerGA)
    {
        // 캐릭터 생성
        PlayerView playerView = Creator.Instance.CreatAsset<PlayerView>(CreateAssetName.PlayerView);
        playerView.transform.SetParent(_characterParent, false);

        playerView.Init(spawnPlayerGA.Player, PartyHealth, PartyStatusController);
        PartyHealth.ChangeMaxHp(spawnPlayerGA.Player.GetStat(EStatType.MaxHp).Value);
        PartyHealth.Init();

        _players.Add(playerView);

        // 캐릭터 배치 조정
        EventBus.Publish(new StArrangePlayerEvent());

        yield return null;
    }

    private IEnumerator MergeCharacterPerformer(MergeCharacterGA mergeCharacterGA)
    {
        mergeCharacterGA.TargetView.Player.Merge (mergeCharacterGA.SourcePlayer);
        yield return null;
    }

    private IEnumerator LevelUpPlayerPerformer(LevelUpPlayerGA levelUpPlayerGA)
    {
        int oldLevel = levelUpPlayerGA.TargetPlayer.Player.Level;
        if (oldLevel >= GameDefine.MAX_LEVEL)
        {
            EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_CHARACTERSYSTEM_009")
                , LocalizationManager.Instance.Get(levelUpPlayerGA.TargetPlayer.Player.PlayerData.SubjectKeyword.ToString())), EMessageType.Warning));
            yield break;
        }

        if (levelUpPlayerGA.Cost > 0)
        {
            ActionSystem.Instance.AddReaction (new ApplyGoldDeltaGA (-levelUpPlayerGA.Cost));
        }

        int gainedLevels = levelUpPlayerGA.TargetPlayer.Player.LevelUp (levelUpPlayerGA.LevelDiff);
        int newLevel = levelUpPlayerGA.TargetPlayer.Player.Level;

        if (gainedLevels <= 0)
        {
            yield break;
        }

        EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_CHARACTERSYSTEM_010")
            , LocalizationManager.Instance.Get(levelUpPlayerGA.TargetPlayer.Player.PlayerData.SubjectKeyword.ToString()), newLevel), EMessageType.Notice));

        CheckAndGainJobArtifact (levelUpPlayerGA.TargetPlayer, oldLevel, newLevel);
        yield return null;
    }

    private IEnumerator LevelUpPartyPerformer(LevelUpPartyGA levelUpPartyGA)
    {
        bool canLevelUpAny = false;
        foreach (PlayerView playerView in _players)
        {
            if (playerView.Player.IsMaxLevel == false)
            {
                canLevelUpAny = true;
                break;
            }
        }

        if (canLevelUpAny == false)
        {
            EventBus.Publish (new StSendMessageEvent (LocalizationManager.Instance.Get("CS_CHARACTERSYSTEM_011"), EMessageType.Warning));
            yield break;
        }

        if (levelUpPartyGA.Cost > 0)
        {
            ActionSystem.Instance.AddReaction (new ApplyGoldDeltaGA (-levelUpPartyGA.Cost));
        }

        bool hasLeveledUp = false;

        foreach (PlayerView playerView in _players)
        {
            int oldLevel = playerView.Player.Level;
            int gainedLevels = playerView.Player.LevelUp (levelUpPartyGA.LevelDiff);
            int newLevel = playerView.Player.Level;

            if (gainedLevels <= 0)
            {
                continue;
            }

            hasLeveledUp = true;
            CheckAndGainJobArtifact (playerView, oldLevel, newLevel);
        }

        if (hasLeveledUp)
        {
            EventBus.Publish (new StSendMessageEvent (LocalizationManager.Instance.Get("CS_CHARACTERSYSTEM_012"), EMessageType.Notice));
        }
        yield return null;
    }

    // 일단 3,6,9만 레벨 업이라 가정.
    private void CheckAndGainJobArtifact(PlayerView playerView, int oldLevel, int newLevel)
    {
        if (oldLevel < 3 && newLevel >= 3)
        {
            ActionSystem.Instance.AddReaction (new GainJobArtifactGA (playerView, 3));
        }

        if (oldLevel < 6 && newLevel >= 6)
        {
            ActionSystem.Instance.AddReaction (new GainJobArtifactGA (playerView, 6));
        }

        if (oldLevel < 9 && newLevel >= 9)
        {
            ActionSystem.Instance.AddReaction (new GainJobArtifactGA (playerView, 9));
        }
    }
    private IEnumerator GainJobArtifactPerformer(GainJobArtifactGA gainJobArtifactGA)
    {
        Player player = gainJobArtifactGA.TargetPlayerView.Player;

        List<SO_ArtifactData> candidates = ArtifactSystem.Instance.GetLevelUpArtifactCandidates (player, 3);
        if (candidates.Count == 0)
        {
            EventBus.Publish (new StSendMessageEvent (string.Format(LocalizationManager.Instance.Get("CS_CHARACTERSYSTEM_013")
                , LocalizationManager.Instance.Get(player.PlayerData.SubjectKeyword.ToString())), EMessageType.Warning));
            yield break;
        }

        bool isSelected = false;
        SO_ArtifactData chosenArtifact = null;

        UI_LevelUpArtifactSelect uiLevelUpArtifactSelect =
            UIManager.Instance.Get<UI_LevelUpArtifactSelect>(EUIType.UI_LevelUpArtifactSelect);

        if (uiLevelUpArtifactSelect != null)
        {
            uiLevelUpArtifactSelect.OpenForArtifactSelect(player, candidates, (selected) =>
            {
                chosenArtifact = selected;
                isSelected = true;
            });

            yield return new WaitUntil (() => isSelected);

            if (chosenArtifact != null)
            {
                ArtifactSystem.Instance.AddArtifact (chosenArtifact.ID, player);
            }
        }
    }

    private IEnumerator SpawnEnemyPerformer(SpawnEnemyGA spawnEnemyGA)
    {
        EnemyView enemyView = Creator.Instance.CreatAsset<EnemyView>(CreateAssetName.EnemyView);
        enemyView.transform.SetParent(_characterParent, false);

        HealthController healthController = new HealthController(spawnEnemyGA.Enemy.GetStat(EStatType.MaxHp).Value);
        StatusController statusController = new StatusController();
        enemyView.Init(spawnEnemyGA.Enemy, healthController, statusController);
        enemyView.Character.PosIndex = spawnEnemyGA.PosIndex;
        healthController.Init();

        _enemies.Add(enemyView);

        // 캐릭터 배치 조정
        EventBus.Publish(new StArrangeEnemyEvent());

        yield return null;
    }

    private IEnumerator DespawnPlayerPerformer(DespawnPlayerGA despawnEnemyGA)
    {
        _players.Remove(despawnEnemyGA.PlayerView);
        Creator.Instance.RemoveAsset(CreateAssetName.PlayerView, despawnEnemyGA.PlayerView.gameObject);

        // 캐릭터 배치 조정
        EventBus.Publish(new StArrangePlayerEvent());
        yield break;
    }

    private IEnumerator EnemyDeadPerformer(EnemyDeadGA enemyDeadGA)
    {
        UI_Battle uiBattle = UIManager.Instance.Get<UI_Battle>(EUIType.UI_Battle);
        StartCoroutine(uiBattle.CoDeleteToken(enemyDeadGA.Killed));

        enemyDeadGA.Killed.transform.DOScale(0, StyleManager.Instance.AnimationTimeData.CharacterDeadAnimationTime).SetEase(Ease.InBack);
        yield return new WaitForSeconds(StyleManager.Instance.AnimationTimeData.CharacterDeadAnimationTime);

        enemyDeadGA.Killed.SetTarget(false);
        _enemies.Remove(enemyDeadGA.Killed);

        Destroy(enemyDeadGA.Killed.gameObject);

        if (BattleSystem.Instance.BattleState == EBattleState.InAutoBattle)
        {
            if(_enemies.Count != 0)
            {
                // _enemies[0].SetTarget();
            }
            else
            {
                ClearBattleGA clearBattleGA = new ClearBattleGA();
                ActionSystem.Instance.AddReaction(clearBattleGA);
            }
        }
    }

    private IEnumerator ChangeEnemyActCountPerformer(ChangeEnemyActCountGA changeEnemyActCountGA)
    {
        foreach(CharacterView target in changeEnemyActCountGA.Targets)
        {
            if(target is EnemyView enemyView)
            {
                enemyView.Enemy.EnemyAI.ChangeActCount(changeEnemyActCountGA.ActCountDiff);
            }
        }

        yield return null;
    }
    public PartySnapshot CapturePartySnapshot()
    {
        PartySnapshot snapshot = new PartySnapshot
        {
            CurrentHp = PartyHealth != null ? PartyHealth.CurrentHp : 0
        };

        foreach (PlayerView playerView in _players)
        {
            if (playerView?.Player?.PlayerData == null)
                continue;

            snapshot.Players.Add (new PlayerSnapshot
            {
                SubjectKeyword = playerView.Player.PlayerData.SubjectKeyword,
                Level = playerView.Player.Level
            });
        }

        return snapshot;
    }
    public void RestorePartySnapshot(PartySnapshot snapshot)
    {
        foreach (PlayerView playerView in _players.ToArray ())
        {
            if (playerView != null)
            {
                Creator.Instance.RemoveAsset (CreateAssetName.PlayerView, playerView.gameObject);
            }
        }
        _players.Clear ();

        foreach (EnemyView enemyView in _enemies.ToArray ())
        {
            if (enemyView != null)
            {
                Creator.Instance.RemoveAsset (CreateAssetName.EnemyView, enemyView.gameObject);
            }
        }
        _enemies.Clear ();

        if (PartyHealth == null)
        {
            PartyHealth = new HealthController (0);
        }

        if (PartyStatusController == null)
        {
            PartyStatusController = new StatusController ();
        }

        PartyStatusController.ResetForLoad ();

        if (snapshot == null || snapshot.Players == null || snapshot.Players.Count == 0)
        {
            Debug.LogError ("[RestorePartySnapshot] snapshot is empty.");
            PartyHealth.ResetForLoad (0, 0, 0);
            return;
        }

        List<Player> restoredPlayers = new List<Player> ();
        int totalMaxHp = 0;

        foreach (PlayerSnapshot playerSnapshot in snapshot.Players)
        {
            SO_PlayerData playerData = DataManager.Instance.AllPlayers
                .FirstOrDefault (x => x.SubjectKeyword == playerSnapshot.SubjectKeyword);

            if (playerData == null)
            {
                Debug.LogError ($"[RestorePartySnapshot] missing player data for subject {playerSnapshot.SubjectKeyword}");
                PartyHealth.ResetForLoad (0, 0, 0);
                return;
            }

            Player player = new Player (playerData);
            player.RestoreLevelDirect (playerSnapshot.Level);

            restoredPlayers.Add (player);
            totalMaxHp += player.GetStat (EStatType.MaxHp).Value;
        }

        PartyHealth.ResetForLoad (totalMaxHp, snapshot.CurrentHp, 0);

        foreach (Player player in restoredPlayers)
        {
            PlayerView playerView = Creator.Instance.CreatAsset<PlayerView> (CreateAssetName.PlayerView);
            playerView.transform.SetParent (_characterParent, false);
            playerView.Init (player, PartyHealth, PartyStatusController);

            _players.Add (playerView);
        }

        DataManager.Instance.GameModel.SubjectKeywords.Clear ();
        foreach (PlayerView playerView in _players)
        {
            if (playerView?.Player?.PlayerData == null)
            {
                continue;
            }

            DataManager.Instance.GameModel.SubjectKeywords.Add (playerView.Player.PlayerData.SubjectKeyword);
        }

        EventBus.Publish (new StArrangePlayerEvent ());

        Debug.Log ($"[RestorePartySnapshot] restoredCount={_players.Count}, hp={PartyHealth.CurrentHp}/{PartyHealth.MaxHp}");
    }

    public void ClearTutorialCharacters()
    {
        foreach (PlayerView playerView in _players.ToArray())
        {
            if (playerView != null)
            {
                Creator.Instance.RemoveAsset(CreateAssetName.PlayerView, playerView.gameObject);
            }
        }
        _players.Clear();

        foreach (EnemyView enemyView in _enemies.ToArray())
        {
            if (enemyView != null)
            {
                Creator.Instance.RemoveAsset(CreateAssetName.EnemyView, enemyView.gameObject);
            }
        }
        _enemies.Clear();

        PartyHealth = new HealthController(0);
        PartyStatusController = new StatusController();

        DataManager.Instance.GameModel.SubjectKeywords.Clear();
        DataManager.Instance.GameModel.TempSubjectKeywords.Clear();
    }



#if UNITY_EDITOR
    private void Update()
    {
        if (AppConfig.IsCheatEnabled && Input.GetKeyDown (KeyCode.L))
        {
            Debug.Log ("<color=cyan>[치트] L키 입력: 파티 강제 1 레벨업 실행!</color>");

            LevelUpPartyGA debugLevelUpGA = new LevelUpPartyGA (1, 0);
            ActionSystem.Instance.Perform (debugLevelUpGA);
        }
    }
#endif
}

