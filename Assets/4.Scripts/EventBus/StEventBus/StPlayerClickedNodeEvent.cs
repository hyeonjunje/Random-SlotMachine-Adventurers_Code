public readonly struct StPlayerClickedNodeEvent
{
    public readonly MapNode TargetNode;

    public StPlayerClickedNodeEvent(MapNode targetNode)
    {
        TargetNode = targetNode;
    }
}
