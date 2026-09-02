public readonly struct StFinishedCreateMapEvent
{
    public readonly MapData MapData;

    public StFinishedCreateMapEvent(MapData mapData)
    {
        MapData = mapData;
    }
}
