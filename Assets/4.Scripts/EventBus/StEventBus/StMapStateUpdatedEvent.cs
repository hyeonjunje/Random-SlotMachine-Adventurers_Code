public readonly struct StMapStateUpdatedEvent
{
    public readonly MapNode CurrentNode;

    public StMapStateUpdatedEvent(MapNode currentNode)
    {
        CurrentNode = currentNode;
    }
}
