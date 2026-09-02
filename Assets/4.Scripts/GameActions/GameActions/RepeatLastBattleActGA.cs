public class RepeatLastBattleActGA : GameAction
{
    public int RepeatCount { get; private set; }

    public RepeatLastBattleActGA(int repeatCount)
    {
        RepeatCount = repeatCount;
    }
}
