using System;
using System.Collections.Generic;

public enum EKeywordTypePos
{
    Subject = 0, // 첫번째 자리
    Adverb = 1,  // 두번째 자리
    Verb = 2,    // 세번째 자리
}

[Flags]
public enum EKeywordType
{
    None = 0,
    Subject = 1 << 0, // 주어
    Adverb = 1 << 1,  // 부사
    Verb = 1 << 2,    // 동사
    Curse = 1 << 3,   // 저주
}
public enum ETitleKeyword
{
    더미단어,
    지금,
    게임,
    시작
}

public enum EEventSlotMachineKeyword
{
    MaxHPIncrease,          // 최대 체력 증가
    AdventureTeamLevelUp,   // 모험단 레벨업
    Money,                  // 돈
    Jackpot                 // 잭팟
}

public enum EKeyword
{
    None = 0,
    브란 = 1, // 전사
    가렌 = 2,
    로크 = 3,

    세라 = 11, // 궁수
    노아 = 12,
    이안 = 13,

    세인 = 21, // 도적
    제드 = 22,
    실 = 23,

    도르 = 31, // 드워프
    무그 = 32,
    그람 = 33,

    니아 = 41, // 사제
    마르 = 42,
    루멘 = 43,

    그냥 = 101,
    강하게,
    냉기로,
    독성으로,
    반격으로,
    약화로,
    임시로,
    전기로,
    표식으로,

    맹독으로 = 131,
    추적으로,
    탈진으로,
    서리로,
    전격으로,
    응징으로,
    더강하게,
    재차로,

    공격해라 = 201,
    기습해라,
    내리쳐라,
    대체해라,
    쓸어라,
    연타해라,
    찔러라,
    흡수해라,
    깨트려라,

    갈라라 = 231,
    휩쓸어라,
    난타해라,
    삼켜라,
    관통해라,
    급습해라,
    깨부숴라,
    되감아라,

    방어해라 = 301,
    버텨라,
    치유해라,
    수호해라,
    막아라,
    피해라,
    정화해라,
    돌려라,

    봉쇄하라 = 331,
    치료해라,
    보존해라,
    더막아라,
    회피해라,
    더정화해라,
    굴려라,

    골절 = 401,
    점액질,
}

public enum EBingo
{
    /*
    0 1 2
    3 4 5
    6 7 8
    
    0-2 : Horizontal1
    3-5 : Horizontal2
    6-8 : Horizontal3
    0-6 : Vertical1
    1-7 : Vertical2
    2-8 : Vertical3
    0-8 : Diagonal1
    2-6 : Diagonal2
    */
    Horizontal1,
    Horizontal2,
    Horizontal3,
    Vertical1,
    Vertical2,
    Vertical3,
    Diagonal1,
    Diagonal2,
    Size,

    None = -1,
}

public enum EKeywordMatchType
{
    PerfectMatch,      // 일반 완벽한 매치
    Match,             // 일반 그냥 매치
    NonMatch,          // 일반 아무것도 아님
}

public class SlotMachineResult
{
    public EKeyword[,] reelResult;
    public BingoResult[] bingoResult;
}

public class BingoResult
{
    public EKeywordMatchType MatchType { get; private set; }
    public CharacterView Owner { get; private set; }
    public Skill Skill { get; private set; }
    public EBingo Bingo { get; private set; }

    public BingoResult(EKeywordMatchType matchType, CharacterView owner, Skill skill, EBingo bingo)
    {
        MatchType = matchType;
        Owner = owner;
        Skill = skill;
        Bingo = bingo;
    }
}

public class SlotMachineEngine
{
    private SO_SlotMachineConfig _config;

    private List<EKeyword> _subjectPools = new List<EKeyword> ();
    private List<EKeyword> _adverbPools = new List<EKeyword> ();
    private List<EKeyword> _verbPools = new List<EKeyword> ();
    private List<EKeyword> _cursePools = new List<EKeyword> ();

    private bool _isDuplicated = true; // 키워드 중복 여부 

    public IReadOnlyList<EKeyword> SubjectPools => _subjectPools;
    public IReadOnlyList<EKeyword> AdverbPools => _adverbPools;
    public IReadOnlyList<EKeyword> VerbPools => _verbPools;
    public IReadOnlyList<EKeyword> CurseVerbPools => _cursePools;

    public SlotMachineResult Result { get; private set; }

    public readonly Dictionary<EBingo, int[]> BingoPatterns = new Dictionary<EBingo, int[]>
    {
        { EBingo.Horizontal1, new[] { 0, 1, 2 } },
        { EBingo.Horizontal2, new[] { 3, 4, 5 } },
        { EBingo.Horizontal3, new[] { 6, 7, 8 } },
        { EBingo.Vertical1,   new[] { 0, 3, 6 } },
        { EBingo.Vertical2,   new[] { 1, 4, 7 } },
        { EBingo.Vertical3,   new[] { 2, 5, 8 } },
        { EBingo.Diagonal1,   new[] { 0, 4, 8 } },
        { EBingo.Diagonal2,   new[] { 2, 4, 6 } }
    };

    public SlotMachineEngine(SO_SlotMachineConfig config)
    {
        _config = config;
    }

    public void ClearPool()
    {
        _subjectPools.Clear ();
        _adverbPools.Clear ();
        _verbPools.Clear ();
        _cursePools.Clear ();
    }

    public void AddKeyword(EKeyword keyword, EKeywordType keywordType)
    {
        if(keywordType == EKeywordType.Subject)
        {
            _subjectPools.Add(keyword);
        }
        else if(keywordType == EKeywordType.Adverb)
        {
            _adverbPools.Add(keyword);
        }
        else if (keywordType == EKeywordType.Verb)
        {
            _verbPools.Add(keyword);
        }
        else if (keywordType == EKeywordType.Curse)
        {
            _cursePools.Add(keyword);
        }
    }

    public SlotMachineResult PickOne(float higherTierWeightMultiplier = 1f)
    {
        Result = new SlotMachineResult ();

        Result.reelResult = new EKeyword[SO_SlotMachineConfig.HORIZONTAL, SO_SlotMachineConfig.VERTICAL];

        List<EKeyword> tempSubjectPool = new List<EKeyword> (_subjectPools);
        List<EKeyword> tempAdverbPool = new List<EKeyword> (_adverbPools);
        List<EKeyword> tempVerbPool = new List<EKeyword> (_verbPools);
        List<EKeyword> tempCursePool = new List<EKeyword> (_cursePools);

        for (int y = 0; y < SO_SlotMachineConfig.HORIZONTAL; ++y)
        {
            EKeyword pickedSubjectKeyword = PickKeywordFromPool(tempSubjectPool, higherTierWeightMultiplier);
            Result.reelResult[y, (int)EKeywordTypePos.Subject] = pickedSubjectKeyword;

            EKeyword pickedAdverbKeyword = PickKeywordFromPool(tempAdverbPool, higherTierWeightMultiplier);
            Result.reelResult[y, (int)EKeywordTypePos.Adverb] = pickedAdverbKeyword;

            EKeyword pickedVerbKeyword = PickKeywordFromPool(tempVerbPool, higherTierWeightMultiplier);
            Result.reelResult[y, (int)EKeywordTypePos.Verb] = pickedVerbKeyword;

            //  중복 여부 판단
            if (_isDuplicated == false)
            {
                tempSubjectPool.Remove(pickedSubjectKeyword);
                tempAdverbPool.Remove(pickedAdverbKeyword);
                tempVerbPool.Remove(pickedVerbKeyword);
            }
        }

        Result.bingoResult = Judge ();

        return Result;
    }

    private EKeyword PickKeywordFromPool(List<EKeyword> pool, float higherTierWeightMultiplier)
    {
        if (pool == null || pool.Count == 0)
        {
            return EKeyword.None;
        }

        if (higherTierWeightMultiplier <= 1f)
        {
            return pool.GetRandomElement();
        }

        int minRank = int.MaxValue;
        foreach (EKeyword keyword in pool)
        {
            SO_KeywordData keywordData = DataManager.Instance.GetKeywordData(keyword);
            int rank = keywordData != null ? keywordData.Rank : 1;
            minRank = Math.Min(minRank, rank);
        }

        List<float> weights = new List<float>(pool.Count);
        foreach (EKeyword keyword in pool)
        {
            SO_KeywordData keywordData = DataManager.Instance.GetKeywordData(keyword);
            int rank = keywordData != null ? keywordData.Rank : minRank;
            weights.Add(rank > minRank ? higherTierWeightMultiplier : 1f);
        }

        return pool.PickWeighted(weights);
    }

    public BingoResult[] Judge()
    {
        BingoResult[] result = new BingoResult[(int)EBingo.Size];
        EKeyword[,] reelResult = Result.reelResult;

        // 초기화
        for (int i = 0; i < result.Length; ++i)
        {
            result[i] = new BingoResult (EKeywordMatchType.NonMatch, null, null, EBingo.Horizontal1);
        }

        // 일반 
        for (EBingo bingo = EBingo.Horizontal1; bingo <= EBingo.Vertical3; ++bingo)
        {
            List<EKeyword> slotMachineKeywords = new List<EKeyword> ();

            for (int i = 0; i < BingoPatterns[bingo].Length; ++i)
            {
                int x = BingoPatterns[bingo][i] % SO_SlotMachineConfig.HORIZONTAL;
                int y = BingoPatterns[bingo][i] / SO_SlotMachineConfig.VERTICAL;

                slotMachineKeywords.Add (reelResult[y, x]);
            }

            EKeywordMatchType matchType = CheckKeywordsResult (slotMachineKeywords);

            if (matchType != EKeywordMatchType.NonMatch)
            {
                List<Keyword> keywords = new List<Keyword> ();
                for (int i = 0; i < slotMachineKeywords.Count; ++i)
                {
                    keywords.Add (new Keyword (DataManager.Instance.GetKeywordData (slotMachineKeywords[i]), BingoPatterns[bingo][i]));
                }

                (Keyword, Keyword, Keyword) resultKeywords = GetKeywordsByOrder (keywords);
                PlayerView playerView = GetPlayer (resultKeywords.Item1.KeywordData.Keyword);

                Skill skill = new Skill (resultKeywords.Item3, resultKeywords.Item2, playerView);
                result[(int)bingo] = new BingoResult (matchType, playerView, skill, bingo);
            }
        }

        bool allowDiagonal = DataManager.Instance.GameModel.IsAllowDiagonal;

        // 대각선
        if (allowDiagonal)
        {
            for (EBingo bingo = EBingo.Diagonal1; bingo <= EBingo.Diagonal2; ++bingo)
            {
                List<EKeyword> slotMachineKeywords = new List<EKeyword> ();

                for (int i = 0; i < BingoPatterns[bingo].Length; ++i)
                {
                    int x = BingoPatterns[bingo][i] % SO_SlotMachineConfig.HORIZONTAL;
                    int y = BingoPatterns[bingo][i] / SO_SlotMachineConfig.VERTICAL;

                    slotMachineKeywords.Add (reelResult[y, x]);
                }

                EKeywordMatchType matchType = CheckKeywordsResult (slotMachineKeywords);

                if (matchType != EKeywordMatchType.NonMatch)
                {
                    List<Keyword> keywords = new List<Keyword> ();
                    for (int i = 0; i < slotMachineKeywords.Count; ++i)
                    {
                        keywords.Add (new Keyword (DataManager.Instance.GetKeywordData (slotMachineKeywords[i]), BingoPatterns[bingo][i]));
                    }

                    (Keyword, Keyword, Keyword) resultKeywords = GetKeywordsByOrder (keywords);
                    PlayerView playerView = GetPlayer (resultKeywords.Item1.KeywordData.Keyword);

                    Skill skill = new Skill (resultKeywords.Item3, resultKeywords.Item2, playerView);
                    result[(int)bingo] = new BingoResult (matchType, playerView, skill, bingo);
                    ArtifactSystem.Instance.TriggerArtifactByID (EArtifactId.대각선);
                }
            }
        }

        return result;
    }

    public EKeywordMatchType CheckKeywordsResult(List<EKeyword> resultKeywords)
    {
        // 방어 코드: 슬롯은 무조건 3개라고 가정하지만 안전을 위해 체크
        if (resultKeywords == null || resultKeywords.Count != 3)
        {
            return EKeywordMatchType.NonMatch;
        }

        // 가독성과 캐싱을 위해 변수로 할당 (프로퍼티 접근 비용 최소화)
        SO_KeywordData keywordData1 = DataManager.Instance.GetKeywordData (resultKeywords[0]);
        SO_KeywordData keywordData2 = DataManager.Instance.GetKeywordData (resultKeywords[1]);
        SO_KeywordData keywordData3 = DataManager.Instance.GetKeywordData (resultKeywords[2]);
        List<SO_KeywordData> keywordDatas = new List<SO_KeywordData> () { keywordData1, keywordData2, keywordData3 };

        if (keywordData1 == null || keywordData2 == null || keywordData3 == null)
        {
            return EKeywordMatchType.NonMatch;
        }

        // 1. PerfectMatch 판별 (순서가 정확해야 함: 주어 -> 부사 -> 동사)
        if (keywordData1.KeywordType == EKeywordType.Subject &&
            keywordData2.KeywordType == EKeywordType.Adverb &&
            keywordData3.KeywordType == EKeywordType.Verb)
        {
            return EKeywordMatchType.PerfectMatch;
        }

        // 2. Match 판별 (순서 상관없이 구성 요소가 하나씩 다 있는지)
        // 리스트가 3개뿐이므로 루프를 돌며 체크
        bool hasSubject = false;
        bool hasAdverb = false;
        bool hasVerb = false;

        for (int i = 0; i < 3; i++)
        {
            switch (keywordDatas[i].KeywordType)
            {
                case EKeywordType.Subject: hasSubject = true; break;
                case EKeywordType.Adverb: hasAdverb = true; break;
                case EKeywordType.Verb: hasVerb = true; break;
            }
        }

        // 3개가 다 켜져있다면, 순서는 섞였어도 구성은 완벽하다는 뜻
        if (hasSubject && hasAdverb && hasVerb)
        {
            return EKeywordMatchType.Match;
        }

        // 3. 그 외 (재료가 중복되거나 빠짐)
        return EKeywordMatchType.NonMatch;
    }

    // 주어, 부사, 동사 순으로 반환
    public (Keyword, Keyword, Keyword) GetKeywordsByOrder(List<Keyword> resultKeywords)
    {
        (Keyword, Keyword, Keyword) result = (null, null, null);

        for (int i = 0; i < resultKeywords.Count; ++i)
        {
            Keyword resultKeyword = resultKeywords[i];

            if (resultKeyword.KeywordData.KeywordType == EKeywordType.Subject)
            {
                result.Item1 = resultKeyword;
            }
            else if (resultKeyword.KeywordData.KeywordType == EKeywordType.Adverb)
            {
                result.Item2 = resultKeyword;
            }
            else if (resultKeyword.KeywordData.KeywordType == EKeywordType.Verb)
            {
                result.Item3 = resultKeyword;
            }
        }

        return result;
    }

    public PlayerView GetPlayer(EKeyword subjectKeyword)
    {
        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            if (playerView.Player.PlayerData.SubjectKeyword == subjectKeyword)
            {
                return playerView;
            }
        }
        return null;
    }

    public List<EKeyword> GetKeywordsByX(EKeywordTypePos keywordTypePos)
    {
        if(keywordTypePos == EKeywordTypePos.Subject)
        {
            return _subjectPools;
        }

        if(keywordTypePos == EKeywordTypePos.Adverb)
        {
            return _adverbPools;
        }

        if(keywordTypePos == EKeywordTypePos.Verb)
        {
            return _verbPools;
        }

        return null;
    }
}
