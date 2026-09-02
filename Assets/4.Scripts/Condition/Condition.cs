using System;
using UnityEngine;

[System.Serializable]
public abstract class Condition
{
    [SerializeField] protected EReactionTiming _reactionTiming;

    protected CharacterView _owner;

    public virtual void SetOwner(CharacterView owner)
    {
        _owner = owner;
    }

    public virtual Condition Clone()
    {
        return (Condition)MemberwiseClone();
    }

    public abstract void SubscribeCondition(Action<GameAction> reaction);
    public abstract void UnsubscribeCondition(Action<GameAction> reaction);
    public abstract bool SubConditionIsMet(GameAction gameAction);
}
