public class ChangeNextEventPageGA : GameAction
{
    public int PageId { get; private set; }

    public ChangeNextEventPageGA(int pageId)
    {
        PageId = pageId;
    }
}
