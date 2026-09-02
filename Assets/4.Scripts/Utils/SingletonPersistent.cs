using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public abstract class SingletonPersistent<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _quitting;

    public static T Instance
    {
        get
        {
            if (_quitting) return null;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        OnAwakeSingleton();
    }

    protected virtual void OnAwakeSingleton() { }

    protected virtual void OnApplicationQuit() => _quitting = true;

    protected virtual void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
