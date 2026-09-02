using UnityEngine;

public class GainJobArtifactGA : GameAction
{
    public PlayerView TargetPlayerView { get; private set; }
    public int ReachedLevel { get; private set; }

    public GainJobArtifactGA(PlayerView targetPlayerView, int reachedLevel)
    {
        TargetPlayerView = targetPlayerView;
        ReachedLevel = reachedLevel;
    }
}