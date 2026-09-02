using System.Collections.Generic;
using System.Linq;

public class AllPlayerTargetSelector : TargetSelector
{
    public override bool IsParty => true;

    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        if (CharacterSystem.Instance == null)
        {
            return new List<CharacterView>();
        }

        return CharacterSystem.Instance.Players
            .Where(player => player != null && !player.Character.IsDead)
            .Cast<CharacterView>()
            .ToList();
    }
}
