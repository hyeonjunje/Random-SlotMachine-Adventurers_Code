public readonly struct StCreateMapEvent
{
    public readonly SO_MapConfigData MapConfigData;

    public StCreateMapEvent(SO_MapConfigData mapConfigData)
    {
        MapConfigData = mapConfigData;
    }
}