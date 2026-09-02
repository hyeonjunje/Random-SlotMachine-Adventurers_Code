public enum ESlotMachineSuccessType
{
    Fail,
    Success,
    GreatSuccess,
    UltraSuccess
}

public class SpinSlotMachineGA : GameAction
{
    public float HigherTierWeightMultiplier { get; private set; } = 1f;
    public ESlotMachineSuccessType SuccessType { get; private set; }

    public SpinSlotMachineGA(ESlotMachineSuccessType successType = ESlotMachineSuccessType.Success)
    {
        SuccessType = successType;
    }

    public void SetHigherTierWeightMultiplier(float multiplier)
    {
        HigherTierWeightMultiplier = UnityEngine.Mathf.Max(1f, multiplier);
    }

    public void SetSuccessType(ESlotMachineSuccessType successType)
    {
        SuccessType = successType;
    }
}
