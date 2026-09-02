using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleView : MonoBehaviour, IInitializable
{
    [Header("Env")]
    [SerializeField] private GameObject[] _envStages;

    [Header("Party Hp, Status")]
    [SerializeField] private HpBar _hpBar;
    [SerializeField] private StatusView _statusView;

    [SerializeField] private float _highlightSpace = 1f;
    [SerializeField] private Transform[] _pivotHeros;
    [SerializeField] private Transform[] _pivotEnemies;

    [SerializeField] private Transform _pivotHighlight;

    private IDisposable _onEnterBattleNodeEvent;
    private IDisposable _onArrangePlayerEvent;
    private IDisposable _onArrangeEnemyEvent;
    private IDisposable _onArrangeBattleActEvent;
    private IDisposable _onLeaveNodeEvent;

    public void Initialize()
    {
        _onEnterBattleNodeEvent = EventBus.Subscribe<StEnterBattleNodeEvent>(OnEnterBattleNodeEvent);

        _hpBar.Init(CharacterSystem.Instance.PartyHealth);
        _statusView.Init(CharacterSystem.Instance.PartyStatusController);
    }

    private void OnDestroy()
    {
        _onEnterBattleNodeEvent?.Dispose();
    }

    private void OnEnable()
    {
        _onArrangePlayerEvent = EventBus.Subscribe<StArrangePlayerEvent>(OnArrangePlayerEvent);
        _onArrangeEnemyEvent = EventBus.Subscribe<StArrangeEnemyEvent>(StArrangeEnemyEvent);
        _onArrangeBattleActEvent = EventBus.Subscribe<StArrangeBattleActEvent>(StArrangeBattleActEvent);

        _onLeaveNodeEvent = EventBus.Subscribe<StLeaveNodeEvent>(OnLeaveNodeEvent);
    }

    private void OnDisable()
    {
        _onArrangePlayerEvent?.Dispose();
        _onArrangeEnemyEvent?.Dispose();
        _onArrangeBattleActEvent?.Dispose();

        _onLeaveNodeEvent?.Dispose();
    }

    private void OnEnterBattleNodeEvent(StEnterBattleNodeEvent enterBattleNodeEvent)
    {
        gameObject.SetActive(true);

        _hpBar.Init(CharacterSystem.Instance.PartyHealth);
        _statusView.Init(CharacterSystem.Instance.PartyStatusController);

        // 배경 세팅
        for(int i = 0; i < _envStages.Length; ++i)
        {
            _envStages[i].SetActive(i == DataManager.Instance.GameModel.Stage);
        }

        // 플레이어 세팅
        for (int i = 0; i < GameDefine.MAXPLAYERCOUNT; ++i)
        {
            CharacterView player = CharacterSystem.Instance.GetPlayer(i);
            if (player != null)
            {
                player.transform.SetParent(_pivotHeros[GetPlayerPivotIndex(i)], false);
            }
        }
    }

    private void OnArrangePlayerEvent(StArrangePlayerEvent arrangePlayerEvent)
    {
        for (int i = 0; i < GameDefine.MAXPLAYERCOUNT; ++i)
        {
            CharacterView player = CharacterSystem.Instance.GetPlayer(i);
            if (player != null)
            {
                player.transform.SetParent(_pivotHeros[GetPlayerPivotIndex(i)], false);
                player.transform.localPosition = Vector3.zero;
            }
        }
    }

    private int GetPlayerPivotIndex(int playerIndex)
    {
        if (CharacterSystem.Instance.Players.Count == 1)
        {
            return Mathf.Min(1, _pivotHeros.Length - 1);
        }

        if (CharacterSystem.Instance.Players.Count == 2)
        {
            return playerIndex == 0 ? 1 : 0;
        }

        if (CharacterSystem.Instance.Players.Count >= 3)
        {
            return playerIndex switch
            {
                0 => 1,
                1 => 0,
                2 => 2,
                _ => playerIndex,
            };
        }

        return playerIndex;
    }
    private void StArrangeEnemyEvent(StArrangeEnemyEvent arrangeEnemyEvent)
    {
        foreach(EnemyView enemyView in CharacterSystem.Instance.Enemies)
        {
            enemyView.transform.SetParent(_pivotEnemies[enemyView.Enemy.PosIndex], false);
            enemyView.transform.localPosition = Vector3.zero;
        }
    }

    private void StArrangeBattleActEvent(StArrangeBattleActEvent arrangeBattleActEvent)
    {
        CharacterView caster = arrangeBattleActEvent.Caster;
        List<CharacterView> targets = arrangeBattleActEvent.Targets;

        List<CharacterView> allActors = new List<CharacterView>();
        allActors.Add(caster);
        allActors.AddRange(targets);
        allActors = allActors.Distinct().ToList();

        foreach(CharacterView actor in allActors)
        {
            actor.transform.SetParent(_pivotHighlight);
            actor.transform.localPosition = Vector3.zero;
            actor.transform.localRotation = Quaternion.identity;
        }

        // 진영별로 분리
        List<CharacterView> ourSideActors = allActors.Where(actor => actor.Character.BattleSideType == EBattleSideType.OurSide).ToList();
        List<CharacterView> enemySideActors = allActors.Where(actor => actor.Character.BattleSideType == EBattleSideType.EnemySide).ToList();

        bool hasOurSide = ourSideActors.Count > 0;
        bool hasEnemySide = enemySideActors.Count > 0;

        if(hasOurSide && hasEnemySide) // 섞여있을 때
        {
            float halfGap = _highlightSpace * 0.5f;
            float currentEdgeX = halfGap;

            foreach(CharacterView enemy in enemySideActors)
            {
                float halfWidth = enemy.Collider.size.x * 0.5f;

                float centerX = currentEdgeX + halfWidth;
                enemy.transform.localPosition = new Vector3(centerX, 0, 0);

                currentEdgeX = centerX + halfWidth + _highlightSpace;
            }

            currentEdgeX = -halfGap;

            for(int i = ourSideActors.Count - 1; i >= 0; --i)
            {
                CharacterView playerView = ourSideActors[i];
                float halfWidth = playerView.Collider.size.x * 0.5f;

                float centerX = currentEdgeX - halfWidth;
                playerView.transform.localPosition = new Vector3(centerX, 0, 0);

                currentEdgeX = centerX - halfWidth - _highlightSpace;
            }
        }
        else // 한쪽 진영만 있을 때
        {
            ArrangeCentered(allActors);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnLeaveNodeEvent(StLeaveNodeEvent leaveNodeEvent)
    {
        Hide();
    }

    private void ArrangeCentered(List<CharacterView> actors)
    {
        if(actors.Count == 0)
        {
            return;
        }

        float totalWidth = 0f;
        for(int i = 0; i < actors.Count; ++i)
        {
            totalWidth += actors[i].Collider.size.x;
        }

        if(actors.Count > 1)
        {
            totalWidth += (actors.Count - 1) * _highlightSpace;
        }

        float currentEdgeX = -totalWidth * 0.5f;

        foreach(CharacterView actor in actors)
        {
            float halfWidth = actor.Collider.size.x * 0.5f;

            float centerX = currentEdgeX + halfWidth;
            actor.transform.localPosition = new Vector3(centerX, 0, 0);

            currentEdgeX = centerX + halfWidth + _highlightSpace;
        }
    }
}
