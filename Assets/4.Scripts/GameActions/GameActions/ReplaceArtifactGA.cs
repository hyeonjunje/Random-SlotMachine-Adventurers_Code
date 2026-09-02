public class ReplaceArtifactGA : GameAction
{
    public Artifact ArtifactToRemove { get; private set; }
    public EArtifactId ArtifactIdToAdd { get; private set; }

    public ReplaceArtifactGA(Artifact artifactToRemove, EArtifactId artifactIdToAdd)
    {
        ArtifactToRemove = artifactToRemove;
        ArtifactIdToAdd = artifactIdToAdd;
    }
}