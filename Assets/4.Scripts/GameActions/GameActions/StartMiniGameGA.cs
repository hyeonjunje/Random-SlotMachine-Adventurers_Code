/// <summary>
/// 이벤트에서 미니게임을 시작하게 하는 GA
/// </summary>
public class StartMiniGameGA : GameAction
{
    public EMiniGameType MiniGameType { get; private set; }

    public StartMiniGameGA(EMiniGameType miniGameType)
    {
        MiniGameType = miniGameType;
    }
}
