using UnityEngine;

public class SpendManaGA : GameAction
{
    public float Cost { get; private set; }
    public SpendManaGA(float cost)
    {
        Cost = cost;
    }
}
