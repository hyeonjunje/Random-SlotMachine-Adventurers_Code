using System.Collections;
using UnityEngine;

public class AbilitySystem : SingletonScene<AbilitySystem>
{
    protected override void OnAwakeSingleton()
    {
        ActionSystem.AttachPerformer<ApplyAbilityGA>(ApplyAbilityPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<ApplyAbilityGA>();
    }

    private IEnumerator ApplyAbilityPerformer(ApplyAbilityGA applyAbilityGA)
    {
        yield return null;
        Ability ability = new Ability(applyAbilityGA.AbilityData, applyAbilityGA.Owner);
        applyAbilityGA.Owner.Character.SetAbilty(ability);
    }
}
