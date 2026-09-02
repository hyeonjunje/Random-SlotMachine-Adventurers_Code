public class PurchaseArtifactGA : GameAction
{
    public EArtifactId ArtifactId { get; private set; }
    public int Cost { get; private set; }

    public PurchaseArtifactGA(EArtifactId artifactId, int cost)
    {
        ArtifactId = artifactId;
        Cost = cost;
    }
}