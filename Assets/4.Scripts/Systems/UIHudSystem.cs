using System.Collections;
using UnityEngine;

public class UIHudSystem : SingletonScene<UIHudSystem>
{
    [SerializeField] private int _initialGold = 5;
    public int CurrentGold { get; private set; } 

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton ();

        CurrentGold = _initialGold; 

        ActionSystem.AttachPerformer<ApplyGoldDeltaGA> (ApplyGoldDeltaPerformer);
        ActionSystem.AttachPerformer<SetGoldGA> (SetGoldPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ApplyGoldDeltaGA> ();
        ActionSystem.DetachPerformer<SetGoldGA> ();
    }

    private IEnumerator ApplyGoldDeltaPerformer(ApplyGoldDeltaGA action)
    {
        if (action.delta < 0)
        {
            if (CurrentGold + action.delta < 0)
            {
                EventBus.Publish (new StSendMessageEvent ("골드가 부족합니다.", EMessageType.Warning));
                yield break; 
            }
        }

        if(action.delta > 0)
        {
            DataManager.Instance.GameModel.GainedGold += action.delta;
        }

        CurrentGold += action.delta;

        EventBus.Publish (new StGoldChangedEvent (CurrentGold, action.delta));

        yield return null;
    }

    private IEnumerator SetGoldPerformer(SetGoldGA action)
    {
        SetGold(action.Amount);

        yield return null;
    }

    public void SetGold(int amount)
    {
        int previousGold = CurrentGold;
        CurrentGold = Mathf.Max(0, amount);

        EventBus.Publish (new StGoldChangedEvent (CurrentGold, CurrentGold - previousGold));
    }

    public bool CanPayGold(int price)
    {
        return CurrentGold >= price;
    }
    public void RestoreGold(int gold)
    {
        CurrentGold = gold;
        EventBus.Publish (new StGoldChangedEvent (CurrentGold, 0));
    }
}
