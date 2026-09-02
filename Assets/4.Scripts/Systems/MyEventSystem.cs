using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyEventSystem : SingletonScene<MyEventSystem>
{
    private System.IDisposable _onEnterEventNodeEvent;
    private HashSet<SO_EventData> _usedEvents = new HashSet<SO_EventData>();

    [SerializeField]
    private List<EventWeightSetting> _weightSettings = new List<EventWeightSetting> ()
    {
        new EventWeightSetting { Type = EEventRiskRewardType.RiskHighRewardHigh, BaseWeight = 50 },
        new EventWeightSetting { Type = EEventRiskRewardType.RiskNoneRewardLow,    BaseWeight = 35 },
        new EventWeightSetting { Type = EEventRiskRewardType.RiskHighRewardNone,       BaseWeight = 15 }
    };

    public delegate int WeightModifier(EEventRiskRewardType type, int currentWeight);
    private List<WeightModifier> _weightModifiers = new List<WeightModifier> ();

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton();
        ActionSystem.AttachPerformer<PerformEventChoiceEffectGA>(PerformEventChoiceEffectPerformer);
        ActionSystem.AttachPerformer<ChangeNextEventPageGA>(ChangeNextEventPagePerformer);

        ActionSystem.AttachPerformer<StartMiniGameGA>(StartMiniGamePerformer);

        _onEnterEventNodeEvent = EventBus.Subscribe<StEnterEventNodeEvent>(OnEnterEventNodeEvent);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<PerformEventChoiceEffectGA>();
        ActionSystem.DetachPerformer<ChangeNextEventPageGA>();

        ActionSystem.DetachPerformer<StartMiniGameGA>();

        _onEnterEventNodeEvent?.Dispose();
        _weightModifiers.Clear (); 
    }

    private void OnEnterEventNodeEvent(StEnterEventNodeEvent enterEventNode)
    {
        UIManager.Instance.Open(EUIType.UI_Event);
        UI_Event uiEvent = UIManager.Instance.Get<UI_Event>(EUIType.UI_Event);

        SO_EventData selectedEvent = GetRandomEventWithWeight ();

        if (selectedEvent != null)
        {
            uiEvent.Setup (selectedEvent);
        }
    }
    private SO_EventData GetRandomEventWithWeight()
    {
        var allEvents = DataManager.Instance.AllEvents;
        
        // 남은 이벤트 목록 구하기
        var availableEvents = allEvents.Where(evt => !_usedEvents.Contains(evt)).ToList();
        
        // 모든 이벤트가 다 나왔다면 초기화
        if (availableEvents.Count == 0 && allEvents.Count > 0)
        {
            _usedEvents.Clear();
            availableEvents = allEvents.ToList();
        }

        if (availableEvents.Count == 0) return null;

        var availableTypes = availableEvents.Select(evt => evt.EventRiskRewardType).Distinct().ToList();

        Dictionary<EEventRiskRewardType, int> finalWeights = new Dictionary<EEventRiskRewardType, int> ();
        int totalWeight = 0;

        foreach (var setting in _weightSettings)
        {
            // 남은 이벤트가 있는 타입만 가중치 계산에 포함
            if (!availableTypes.Contains(setting.Type)) continue;

            int weight = setting.BaseWeight;

            // 외부 수정 적용
            foreach (var mod in _weightModifiers)
            {
                weight = mod (setting.Type, weight);
            }

            weight = Mathf.Max (0, weight);

            finalWeights[setting.Type] = weight;
            totalWeight += weight;
        }

        SO_EventData selectedEvent = null;

        // 가중치 총합 0일 때 (또는 가중치가 설정된 타입의 이벤트가 없을 때) 랜덤 뽑기
        if (totalWeight <= 0)
        {
            selectedEvent = availableEvents[Random.Range (0, availableEvents.Count)];
        }
        else
        {
            int randomValue = Random.Range (0, totalWeight);
            EEventRiskRewardType selectedType = finalWeights.Keys.First();

            foreach (var weight in finalWeights)
            {
                if (randomValue < weight.Value)
                {
                    selectedType = weight.Key;
                    break;
                }
                randomValue -= weight.Value;
            }

            List<SO_EventData> candidates = availableEvents
                .Where (evt => evt.EventRiskRewardType == selectedType)
                .ToList ();

            selectedEvent = candidates[Random.Range (0, candidates.Count)];
        }

        _usedEvents.Add(selectedEvent);
        return selectedEvent;
    }

    private IEnumerator PerformEventChoiceEffectPerformer(PerformEventChoiceEffectGA performEventChoiceEffectGA)
    {
        float randomValue = Random.Range(0, 1f);

        // 확률 성공 시
        if(performEventChoiceEffectGA.ChoiceData.Probability == 0 || randomValue < performEventChoiceEffectGA.ChoiceData.Probability)
        {
            foreach (Effect effect in performEventChoiceEffectGA.ChoiceData.Effects)
            {
                List<CharacterView> targets = effect.TargetSelector?.SelectTarget(CharacterSystem.Instance.Players[0]);
                PerformEffectGA performEffectGA = new PerformEffectGA(effect, targets);
                ActionSystem.Instance.AddReaction(performEffectGA);
            }
        }
        else // 확률 실패 시
        {
            foreach (Effect effect in performEventChoiceEffectGA.ChoiceData.FailedEffects)
            {
                List<CharacterView> targets = effect.TargetSelector?.SelectTarget(CharacterSystem.Instance.Players[0]);
                PerformEffectGA performEffectGA = new PerformEffectGA(effect, targets);
                ActionSystem.Instance.AddReaction(performEffectGA);
            }
        }

        
        yield return null;
    }

    private IEnumerator ChangeNextEventPagePerformer(ChangeNextEventPageGA changeNextEventPageGA)
    {
        UI_Event uiEvent = UIManager.Instance.Get<UI_Event>(EUIType.UI_Event);
        uiEvent.SetPage(changeNextEventPageGA.PageId);
        yield return null;
    }

    private IEnumerator StartMiniGamePerformer(StartMiniGameGA startMiniGameGA)
    {
        UI_Event uiEvent = UIManager.Instance.Get<UI_Event>(EUIType.UI_Event);
        if (uiEvent != null && uiEvent.SlotMachineController != null)
        {
            // 이벤트 정보 비활성화
            // uiEvent.ActivePage(false);

            bool isFinished = false;

            // 결과값도 함께 콜백으로 받도록 변경된 PlayStartSequence 호출
            uiEvent.SlotMachineController.PlayStartSequence((resultKeyword) => 
            {
                switch (resultKeyword)
                {
                    case EEventSlotMachineKeyword.Jackpot:
                        RewardGainGold();
                        RewardIncreaseMaxHp();
                        RewardPartyLevelUp();
                        break;
                    case EEventSlotMachineKeyword.Money:
                        RewardGainGold();
                        break;
                    case EEventSlotMachineKeyword.MaxHPIncrease:
                        RewardIncreaseMaxHp();
                        break;
                    case EEventSlotMachineKeyword.AdventureTeamLevelUp:
                        RewardPartyLevelUp();
                        break;
                }

                isFinished = true;
            });

            // 끝나기 전까지 대기
            while (!isFinished)
            {
                yield return null;
            }

            UI_MainHud mainHUD = UIManager.Instance.Get<UI_MainHud>(EUIType.UI_MainHud);
            if (mainHUD != null)
            {
                mainHUD.SetRightButton(() =>
                {
                    // 컨트롤러 끄기
                    uiEvent.SlotMachineController.HideMiniGameSlotMachine(() =>
                    {
                        mainHUD.HideRightButton();

                        // 스테이지 시작
                        StartStageGA startStageGA = new StartStageGA(0);
                        ActionSystem.Instance.Perform(startStageGA);
                    });

                }, LocalizationManager.Instance.Get("CS_MYEVENTSYSTEM_015"));
            }
        }
        else
        {
            Debug.LogWarning("UI_Event를 찾을 수 없거나 SlotMachineController가 세팅되지 않았습니다.");
            yield return null;
        }
    }

    public void AddModifier(WeightModifier modifier)
    {
        _weightModifiers.Add (modifier);
    }
    public void RemoveModifier(WeightModifier modifier)
    {
        _weightModifiers.Remove (modifier);
    }

    private void RewardPartyLevelUp()
    {
        UI_Event uiEvent = UIManager.Instance.Get<UI_Event>(EUIType.UI_Event);

        int rewardLevel = uiEvent.SlotMachineController.GetRewardValue(EEventSlotMachineKeyword.AdventureTeamLevelUp);
        LevelUpPartyGA levelupParty = new LevelUpPartyGA(rewardLevel);
        ActionSystem.Instance.AddReaction(levelupParty);

        Debug.Log(">> 모험단 레벨업이 나왔습니다!");
    }

    private void RewardGainGold()
    {
        UI_Event uiEvent = UIManager.Instance.Get<UI_Event>(EUIType.UI_Event);

        int rewardGold = uiEvent.SlotMachineController.GetRewardValue(EEventSlotMachineKeyword.Money);
        GrantPostBattleGoldGA goldGA = new GrantPostBattleGoldGA(rewardGold);
        ActionSystem.Instance.AddReaction(goldGA);

        Debug.Log(">> 돈이 나왔습니다!");
    }

    private void RewardIncreaseMaxHp()
    {
        UI_Event uiEvent = UIManager.Instance.Get<UI_Event>(EUIType.UI_Event);

        int rewardMaxHp = uiEvent.SlotMachineController.GetRewardValue(EEventSlotMachineKeyword.MaxHPIncrease);
        List<CharacterView> targets = new List<CharacterView>() { CharacterSystem.Instance.Players[1] };
        ChangeStatValueGA changeStatValueGA = new ChangeStatValueGA(EStatType.MaxHp, EStatModType.Add, rewardMaxHp, targets, null);
        ActionSystem.Instance.AddReaction(changeStatValueGA);

        Debug.Log(">> 최대 체력 증가가 나왔습니다!");
    }
    public SO_EventData PickRandomEventForSave()
    {
        return GetRandomEventWithWeight ();
    }

}


