public enum EArtifactChangeType { Added, Removed }

public class StArtifactChangedEvent
{
    public Artifact Artifact { get; }
    public EArtifactChangeType ChangeType { get; }

    public StArtifactChangedEvent(Artifact artifact, EArtifactChangeType changeType)
    {
        Artifact = artifact;
        ChangeType = changeType;
    }
}