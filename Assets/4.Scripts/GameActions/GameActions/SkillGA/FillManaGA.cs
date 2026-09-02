using UnityEngine;

public class FillManaGA : GameAction
{
    public float Amount { get; private set; }
    public FillManaGA(float amount)
    {
        Amount = amount;
    }
}
