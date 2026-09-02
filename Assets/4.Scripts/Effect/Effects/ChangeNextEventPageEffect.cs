using System.Collections.Generic;
using UnityEngine;

public class ChangeNextEventPageEffect : Effect
{
    [SerializeField] private int NextId;

    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new ChangeNextEventPageGA(NextId);
    }
}