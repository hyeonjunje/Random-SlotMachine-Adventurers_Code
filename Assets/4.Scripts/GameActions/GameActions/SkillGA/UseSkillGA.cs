using UnityEngine;

public class UseSkillGA : GameAction
{
    public SO_SkillData SkillData;
    public CharacterView Caster;
    public CharacterView ExplicitTarget;

    public UseSkillGA(SO_SkillData skillData, CharacterView caster, CharacterView target)
    {
        SkillData = skillData;
        Caster = caster;
        ExplicitTarget = target;
    }
}
