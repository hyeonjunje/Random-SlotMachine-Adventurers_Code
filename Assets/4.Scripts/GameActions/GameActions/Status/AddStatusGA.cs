using System.Collections.Generic;

public class AddStatusGA : GameAction
{
    public SO_StatusData Status { get; private set; }
    public int Turn { get; private set; }
    public List<CharacterView> Targets { get; private set; }
    public CharacterView Caster { get; private set; }
    public bool IsBlocked { get; private set; }

    public AddStatusGA(SO_StatusData status, int turn, List<CharacterView> targets, CharacterView caster)
    {
        Status = status;
        Turn = turn;
        Targets = new List<CharacterView>(targets);
        Caster = caster;
    }

    public void MultiplyTurn(float multiplier)
    {
        Turn = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(Turn * multiplier));
    }

    public void Block()
    {
        IsBlocked = true;
    }
}
