using System.Collections.Generic;

public class StartStageGA : GameAction
{
    public int StageIndex { get; private set; }

    public StartStageGA(int stageIndex)
    {
        StageIndex = stageIndex;
    }
}