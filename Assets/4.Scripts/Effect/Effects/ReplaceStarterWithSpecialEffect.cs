using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReplaceStarterWithSpecialEffect : Effect
{
public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        Artifact artifactToRemove = ArtifactSystem.Instance.OwnedArtifacts
            .FirstOrDefault(art => art != null && ArtifactSystem.Instance.HasPool(art.Data, EArtifactPool.Starter));
        SO_ArtifactData bossArtifact = ArtifactSystem.Instance
            .GetRandomRewardArtifacts(1)
            .FirstOrDefault();

        if (bossArtifact == null)
        {
            return null;
        }

        return new ReplaceArtifactGA(artifactToRemove, bossArtifact.ID);
    }
}
