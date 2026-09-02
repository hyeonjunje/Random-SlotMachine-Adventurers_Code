using UnityEngine;

public class ApplyGoldDeltaGA : GameAction
{
    public int delta { get; private set; }
    public ApplyGoldDeltaGA(int delta)
    {
        this.delta = delta;
    }
}