public readonly struct StLeaveNodeEvent
{
    public readonly MapNode CurrentNode;

    public StLeaveNodeEvent(MapNode currentNode)
    {
        CurrentNode = currentNode;
    }
}
