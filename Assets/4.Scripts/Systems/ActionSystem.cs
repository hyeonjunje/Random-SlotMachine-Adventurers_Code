using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionSystem : SingletonScene<ActionSystem>
{
    static readonly Dictionary<Type, List<Action<GameAction>>> _preSubs = new();
    static readonly Dictionary<Type, List<Action<GameAction>>> _postSubs = new();
    static readonly Dictionary<Type, Func<GameAction, IEnumerator>> Performers = new();
    static readonly Dictionary<Delegate, Action<GameAction>> _delegates = new();

    private struct ActionRequest
    {
        public GameAction action;
        public Action onFinishAction;
    }

    private Queue<ActionRequest> _actionQueue = new Queue<ActionRequest>();

    List<GameAction> _reactions = new();
    string _currentActionName = "";
    public bool IsPerforming { get; private set; } = false;
    public Stack<GameAction> ActiveActions { get; private set; } = new Stack<GameAction>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        _preSubs.Clear();
        _postSubs.Clear();
        Performers.Clear();
        _delegates.Clear();
    }

    public void Perform(GameAction action, Action onFinishAction = null)
    {
        _actionQueue.Enqueue(new ActionRequest { action = action, onFinishAction = onFinishAction });

        if (!IsPerforming)
        {
            ProcessNextAction();
        }
    }

    private void ProcessNextAction()
    {
        if (_actionQueue.Count == 0)
        {
            IsPerforming = false;
            _currentActionName = "";
            return;
        }

        IsPerforming = true;
        var request = _actionQueue.Dequeue();
        _currentActionName = request.action.GetType().Name;

        StartCoroutine(Flow(request.action, () =>
        {
            request.onFinishAction?.Invoke();
            ProcessNextAction();
        }));
    }

    public void AddReaction(GameAction action)
    {
        _reactions.Add(action);
    }

    public void CancelAllActions()
    {
        StopAllCoroutines();
        _actionQueue.Clear();
        _reactions.Clear();
        _currentActionName = "";
        IsPerforming = false;
    }
    
    public static void AttachPerformer<T>(Func<T, IEnumerator> action) where T : GameAction
    {
        IEnumerator WrappedPerformer(GameAction gameAction) => action((T)gameAction);
        Performers[typeof(T)] = WrappedPerformer;
    }

    public static void DetachPerformer<T>() where T : GameAction
    {
        Performers.Remove(typeof(T));
    }

    public static void SubscribeReaction<T>(Action<T> reaction, EReactionTiming timing) where T :  GameAction
    {
        Type type = typeof(T);
        Dictionary<Type, List<Action<GameAction>>> subs = timing == EReactionTiming.Pre ? _preSubs : _postSubs;

        if (!_delegates.ContainsKey(reaction))
        {
            void WrappedReaction(GameAction action) => reaction((T)action);
            _delegates[reaction] = WrappedReaction;
        }

        if (subs.ContainsKey(type))
        {
            subs[type].Add(_delegates[reaction]);
        }
        else
        {
            subs[type] = new List<Action<GameAction>>() { _delegates[reaction] };
        }
    }

    public static void UnSubscribeReaction<T>(Action<T> reaction, EReactionTiming timing) where T : GameAction
    {
        Type type = typeof(T);
        Dictionary<Type, List<Action<GameAction>>> subs = timing == EReactionTiming.Pre ? _preSubs : _postSubs;

        if (_delegates.ContainsKey(reaction) && subs.ContainsKey(type))
        {
            subs[type].Remove(_delegates[reaction]);
            _delegates.Remove(reaction);
        }
        else
        {
            Debug.Log("Nothing Unsubscribed Reaction");
        }
    }
    
    IEnumerator Flow(GameAction action, Action onFinishAction = null)
    {
        ActiveActions.Push(action);

        _reactions = action.PreReactions;
        PerformSubscribers(action, _preSubs);
        yield return PerformReactions();
        
        _reactions = action.PerformActions;
        yield return PerformPerformer(action);
        yield return PerformReactions();
        
        _reactions = action.PostReactions;
        PerformSubscribers(action, _postSubs);
        yield return PerformReactions();
        
        ActiveActions.Pop();

        onFinishAction?.Invoke();
    }

    void PerformSubscribers(GameAction action, Dictionary<Type, List<Action<GameAction>>> subs)
    {
        Type type = action.GetType();
        
        if (subs.TryGetValue(type, out List<Action<GameAction>> subsList))
        {
            foreach (Action<GameAction> sub in subsList)
            {
                sub(action);
            }
        }
    }

    IEnumerator PerformReactions()
    {
        foreach (GameAction reaction in _reactions)
        {
            yield return Flow(reaction);
        }
    }

    IEnumerator PerformPerformer(GameAction action)
    {
        Type type = action.GetType();
        if (Performers.TryGetValue(type, out Func<GameAction, IEnumerator> performer))
        {
            yield return performer(action);
        }
    }
}
