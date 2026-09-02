public class Artifact
{
    public SO_ArtifactData Data { get; private set; }
    public Player OwnerPlayer { get; private set; }
    public bool IsCharacterOwned => OwnerPlayer != null;

    public int Counter { get; set; } = 0;

    public Artifact(SO_ArtifactData data, Player ownerPlayer = null)
    {
        Data = data;
        OwnerPlayer = ownerPlayer;
    }

    public void OnEquip()
    {
        foreach (var logic in Data.Logics)
        {
            logic.Register (this);
        }
    }

    public void OnUnequip()
    {
        foreach (var logic in Data.Logics)
        {
            logic.Unregister (this);
        }
    }
}
