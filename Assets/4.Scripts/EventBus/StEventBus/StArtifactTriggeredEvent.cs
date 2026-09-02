public struct StArtifactTriggeredEvent
{
    public Artifact Artifact { get; private set; }

    public StArtifactTriggeredEvent(Artifact artifact)
    {
        Artifact = artifact;
    }
}