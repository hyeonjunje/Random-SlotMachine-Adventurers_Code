using System;
using System.Collections.Generic;

[Serializable]
public abstract class TargetSelector
{
    public virtual bool IsParty => false;
    public abstract List<CharacterView> SelectTarget(CharacterView caster);
}
