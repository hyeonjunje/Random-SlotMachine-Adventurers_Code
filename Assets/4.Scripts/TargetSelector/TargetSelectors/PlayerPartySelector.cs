using System.Collections.Generic;

public class PlayerPartySelector : TargetSelector
{
    public override bool IsParty => true;

    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        List<CharacterView> targets = new List<CharacterView>();

        if(CharacterSystem.Instance != null && CharacterSystem.Instance.Players.Count != 0)
        {
            // 가운데 있는 애로
            int middleIndex = CharacterSystem.Instance.Players.Count / 2;
            targets.Add(CharacterSystem.Instance.Players[middleIndex]);
        }

        return targets;
    }
}
