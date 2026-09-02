using System.Collections.Generic;
using System;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _subs = new Dictionary<Type, List<Delegate>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        _subs.Clear();
    }

    public static IDisposable Subscribe<T>(Action<T> handler)
    {
        if (!_subs.TryGetValue(typeof(T), out List<Delegate> list))
        {
            list = new List<Delegate>();
            _subs[typeof(T)] = list;
        }
        list.Add(handler);

        return new Subscription<T>(handler);
    }

    public static IDisposable SubscribeOnce<T>(Action<T> handler)
    {
        IDisposable token = null;

        token = Subscribe<T>(evt =>
        {
            token.Dispose();
            handler(evt);
        });

        return token;
    }

    public static void Publish<T>(T evt)
    {
        if (!_subs.TryGetValue(typeof(T), out List<Delegate> list) || list.Count == 0)
        {
            return;
        }

        Delegate[] snapshot = list.ToArray();

        for (int i = 0; i < snapshot.Length; ++i)
        {
            if (snapshot[i] is Action<T> handler)
            {
                handler(evt);
            }
        }
    }

    public static bool HasSubscribers<T>()
    {
        return _subs.TryGetValue(typeof(T), out List<Delegate> list) && list.Count > 0;
    }

    private sealed class Subscription<T> : IDisposable
    {
        private Action<T> _handler;
        private bool _disposed;

        public Subscription(Action<T> handler)
        {
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_subs.TryGetValue(typeof(T), out List<Delegate> list))
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    if (ReferenceEquals(list[i], _handler))
                    {
                        list.RemoveAt(i);
                    }
                }

                if (list.Count == 0)
                {
                    _subs.Remove(typeof(T));
                }
            }

            _handler = null;
        }
    }
}
