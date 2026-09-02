using UnityEngine;

[DisallowMultipleComponent]
public abstract class SingletonScene<T> : MonoBehaviour where T : MonoBehaviour
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
            // 주의: 여기서는 DontDestroyOnLoad를 호출하지 않는다 (씬 수명)
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
