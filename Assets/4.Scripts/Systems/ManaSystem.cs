using System.Collections;
using UnityEngine;

public class ManaSystem : SingletonScene<ManaSystem>
{
    [field : SerializeField] public float MaxMana { get; private set; }
    public float CurrentMana { get; private set; }

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendManaGA>(SpendMana_Performer);
        ActionSystem.AttachPerformer<FillManaGA>(FillMana_Performer);
        ActionSystem.SubscribeReaction<StartTurnGA> (SubscribeStartTurnGA, EReactionTiming.Pre);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendManaGA> ();
        ActionSystem.DetachPerformer<FillManaGA> ();
        ActionSystem.UnSubscribeReaction<StartTurnGA> (SubscribeStartTurnGA, EReactionTiming.Pre);
    }

    public bool CanSpend(int cost)
    {
        return CurrentMana >= cost;
    }

    public void ShowManaShortagegMessage()
    {
        EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_SLOTMACHINESYSTEM_020"), EMessageType.Warning));
    }

    private void Spend(float cost)
    {
        CurrentMana -= cost;

        if (CurrentMana < 0)
        {
            CurrentMana = 0;
        }

        NotifyUI ();
    }

    private void Fill(float amount)
    {
        CurrentMana += amount;

        if (CurrentMana > MaxMana)
        {
            CurrentMana = MaxMana;
        }

        NotifyUI ();
    }

    private void RefillToMax()
    {
        CurrentMana = MaxMana;

        // 탈진 상태이면 현재 마나 1감소
        if(CharacterSystem.Instance.PartyStatusController.IsStatus(EStatusType.Exhaustion))
        {
            CurrentMana -= 1;
        }

        NotifyUI ();
    }

    private void NotifyUI()
    {
        EventBus.Publish(new StChangedManaEvent(CurrentMana, MaxMana));
    }
    public void ChangeMaxMana(float amount)
    {
        MaxMana += amount;
        CurrentMana += amount; 

        NotifyUI ();
    }

    private IEnumerator SpendMana_Performer(SpendManaGA spendManaGA)
    {
        if (spendManaGA == null) yield break;
        Spend (spendManaGA.Cost);
        yield break;
    }

    private IEnumerator FillMana_Performer(FillManaGA fillManaGA)
    {
        if (fillManaGA == null) yield break;

        Fill (fillManaGA.Amount);
        yield break;
    }


    private void SubscribeStartTurnGA(StartTurnGA startTurnGA)
    {
        RefillToMax ();
    }
}
