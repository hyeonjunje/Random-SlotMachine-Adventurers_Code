using System.Collections.Generic;

// 반격, 응징용 TargetSelector
public class RecentlyCasterSelector : TargetSelector
{
    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        return new List<CharacterView>() { BattleSystem.Instance.RecentlyCaster };
    }
}
