using System.Collections.Generic;

public class TriggerArtifactGA : GameAction
{
    public Artifact Artifact { get; private set; }
    public List<GameAction> Effects { get; private set; } = new List<GameAction> ();

    public TriggerArtifactGA(Artifact artifact)
    {
        Artifact = artifact;
    }

    public void AddEffect(GameAction effect)
    {
        if (effect != null)
        {
            Effects.Add (effect);
        }
    }
}