using System.Collections.Generic;
using UnityEngine;

public class Ability
{
    public string AbilityName { get; private set; }
    public Sprite AbilitySprite { get; private set; }
    public string AbilityExplain { get; private set; }
    public Effect[] Effects { get; private set; }
    public Condition Condition { get; private set; }

    public CharacterView Owner { get; private set; }

    public Ability(SO_AbilityData abilityData, CharacterView owner)
    {
        Setup(abilityData);
        Owner = owner;
    }

    private void Setup(SO_AbilityData abilityData)
    {
        AbilityName = LocalizationManager.Instance.Get(abilityData.AbilityName);
        AbilitySprite = abilityData.AbilitySprite;
        AbilityExplain = LocalizationManager.Instance.Get(abilityData.AbilityExplain);
        Effects = abilityData.Effects;
        Condition = abilityData.Condition.Clone();
    }

    public void Add()
    {
        Condition.SetOwner(Owner);
        Condition.SubscribeCondition(Reaction);
    }

    public void Release()
    {
        Condition.UnsubscribeCondition(Reaction);
    }

    private void Reaction(GameAction gameAction)
    {
        if(Condition.SubConditionIsMet(gameAction))
        {
            foreach(Effect effect in Effects)
            {
                List<CharacterView> targets = effect.TargetSelector?.SelectTarget(Owner);
                PerformEffectGA performEffectGA = new PerformEffectGA(effect, targets, Owner);
                ActionSystem.Instance.AddReaction(performEffectGA);
            }
        }
    }
}
