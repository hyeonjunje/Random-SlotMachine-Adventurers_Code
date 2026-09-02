using System.Collections.Generic;

// 내가 미보유한 랜덤한 유물 추가하는 Effect
public class AddRandomArtifactEffect : Effect
{
    public override GameAction GetGameAction(List<CharacterView> targets, CharacterView caster)
    {
        return new AddRandomArtifactGA();
    }
}
