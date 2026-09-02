using UnityEngine;

public abstract class UI_Base : MonoBehaviour, IInitializable
{
    [field:SerializeField] public EUIType UIType { get; private set; }

    public virtual void Initialize()
    {
        UIManager.Instance.SubscribeUI(this);
    }

    private void OnDestroy()
    {
        Dispose();
    }

    public abstract void Open();

    public abstract void Close();

    protected virtual void Dispose() { }
}
