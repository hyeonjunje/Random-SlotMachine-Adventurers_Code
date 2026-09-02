using System.Collections.Generic;

public class ChangeEnemyActCountGA : GameAction
{
    public int ActCountDiff { get; private set; }
    public List<CharacterView> Targets { get; private set; }

    public ChangeEnemyActCountGA(int actCountDiff, List<CharacterView> targets)
    {
        ActCountDiff = actCountDiff;
        Targets = new List<CharacterView>(targets);
    }

    public void SetActCountDiff(int diff)
    {
        ActCountDiff = diff;
    }

    public void MultiplyActCountDiff(float multiplier)
    {
        ActCountDiff = UnityEngine.Mathf.RoundToInt(ActCountDiff * multiplier);
    }
}
