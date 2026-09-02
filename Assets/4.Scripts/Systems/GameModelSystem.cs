using System.Collections;

public class GameModelSystem : SingletonScene<GameModelSystem>
{
    protected override void OnAwakeSingleton()
    {
        ActionSystem.AttachPerformer<ChangeEarnedMoneyAmountGA>(ChangeEarnedMoneyAmountPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ChangeEarnedMoneyAmountGA>();
    }

    private IEnumerator ChangeEarnedMoneyAmountPerformer(ChangeEarnedMoneyAmountGA changeEarnedMoneyAmountGA)
    {
        yield return null;

        switch (changeEarnedMoneyAmountGA.ChangeType)
        {
            case EChangeType.Add:
                DataManager.Instance.GameModel.EarnedMoneyAmount += changeEarnedMoneyAmountGA.EarnedMoneyAmount;
                break;
            case EChangeType.Subtract:
                DataManager.Instance.GameModel.EarnedMoneyAmount -= changeEarnedMoneyAmountGA.EarnedMoneyAmount;
                break;
            case EChangeType.Set:
                DataManager.Instance.GameModel.EarnedMoneyAmount = changeEarnedMoneyAmountGA.EarnedMoneyAmount;
                break;
        }
    }
}
