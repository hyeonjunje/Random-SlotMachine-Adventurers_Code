using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameAction
{
    public List<GameAction> PreReactions { get; private set; } = new();
    public List<GameAction> PerformActions { get; private set; } = new();
    public List<GameAction> PostReactions { get; private set; } = new();
}
