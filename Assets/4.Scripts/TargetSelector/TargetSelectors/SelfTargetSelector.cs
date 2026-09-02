using System.Collections.Generic;
using UnityEngine;

public class SelfTargetSelector : TargetSelector
{
    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        var list = new List<CharacterView> ();

        if (caster != null && !caster.Character.IsDead)
        {
            list.Add (caster);
        }
        return list;
    }
}
