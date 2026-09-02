using System.Collections;
using UnityEngine;

public class SkillSystem : SingletonScene<SkillSystem>
{
    protected override void Awake()
    {
        base.Awake();

        // 스킬 카드 사용
        ActionSystem.AttachPerformer<UseSkillGA> (UseSkill_Performer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<UseSkillGA>();
    }


    private IEnumerator UseSkill_Performer(UseSkillGA useSkillGA)
    {
        var skillData = useSkillGA.SkillData;
        var effect = skillData.Effect;
        var caster = useSkillGA.Caster;
        var selector = effect.TargetSelector;

        // 마나 체크
        int manaCost = skillData.ManaCost;
        if (!ManaSystem.Instance.CanSpend (manaCost))
        {
            Debug.Log ("마나 부족");
            yield break;
        }

        if (useSkillGA.ExplicitTarget != null && selector is ExplicitTargetsSelector explicitTargetsSelector)
        {
            explicitTargetsSelector.SetTarget (caster, useSkillGA.ExplicitTarget);
        }

        var targets = selector.SelectTarget (caster);
        if (targets == null || targets.Count == 0) yield break;

        // 마나 깎기 
        ActionSystem.Instance.AddReaction (new SpendManaGA (manaCost));

        ActionSystem.Instance.AddReaction (new PerformEffectGA (effect, targets, caster));

        yield break;
    }
}
