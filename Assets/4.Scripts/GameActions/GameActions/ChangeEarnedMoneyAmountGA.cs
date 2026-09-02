// 금화 획득량 변동 GA
public class ChangeEarnedMoneyAmountGA : GameAction
{
    public float EarnedMoneyAmount { get; private set; }
    public EChangeType ChangeType { get; private set; }

    public ChangeEarnedMoneyAmountGA(float earnedMoneyAmount, EChangeType changeType)
    {
        EarnedMoneyAmount = earnedMoneyAmount;
        ChangeType = changeType;
    }
}
