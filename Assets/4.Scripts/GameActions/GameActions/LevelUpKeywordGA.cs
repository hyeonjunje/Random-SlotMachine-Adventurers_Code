public class LevelUpKeywordGA : GameAction
{
    public EKeyword UpgradeKeyword { get; private set; } // 업그레이드 할 키워드

    public LevelUpKeywordGA(EKeyword upgradeKeyword)
    {
        UpgradeKeyword = upgradeKeyword;
    }
}
