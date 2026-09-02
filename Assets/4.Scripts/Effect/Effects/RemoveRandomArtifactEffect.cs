using System.Collections.Generic;

// 소유하고 있는 유물 중에 랜덤 1개 제거 Effect
public class RemoveRandomArtifactEffect : Effect
{
    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new RemoveRandomArtifactGA();
    }
}
