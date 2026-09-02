using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EStoreOfferCategory
{
    Artifact,
    Keyword,
    LevelUp,
    Service,
}

public sealed class StorePriceResult
{
    public string RuleId;
    public int OriginalPrice;
    public int Price;
    public bool IsDiscountable;
    public string DiscountGroup;
    public float DiscountRate;
    public int AppearWeight;
}

public sealed class StorePriceRule
{
    public string RuleId;
    public EStoreOfferCategory Category;
    public string TargetCondition;
    public int BasePrice;
    public int MinPrice;
    public int MaxPrice;
    public int AppearWeight;
    public bool IsDiscountable;
    public string DiscountGroup;
    public float DiscountRate;
}

public static class StorePricingService
{
    public const int WordRemovalBasePrice = 75;
    public const int WordRemovalPriceStep = 25;

    private const string CsvResourcePath = "StorePriceRules";
    private const string ArtifactDiscountGroup = "Artifact_Group";
    private const string KeywordDiscountGroup = "Keyword_Group";

    private static Dictionary<string, StorePriceRule> _rulesById;

    public static StorePriceResult GetArtifactPrice(SO_ArtifactData artifactData)
    {
        if (artifactData == null)
        {
            return CreateFixed("STR_ART_FALLBACK", 0, false, "None", 0f, 0);
        }

        if (IsArtifactLevelUpOrJobRule(artifactData))
        {
            return CreateFromRule("STR_ART_002", 380, 330, 430, true, ArtifactDiscountGroup, 0.5f, 400);
        }

        return CreateFromRule("STR_ART_001", 220, 180, 260, true, ArtifactDiscountGroup, 0.5f, 600);
    }

    public static StorePriceResult GetKeywordPrice(SO_KeywordData keywordData)
    {
        int rank = keywordData != null ? keywordData.Rank : 0;

        switch (rank)
        {
            case 1:
                return CreateFromRule("STR_WRD_002", 180, 150, 210, true, KeywordDiscountGroup, 0.5f, 400);
            case 2:
                return CreateFromRule("STR_WRD_003", 400, 350, 450, true, KeywordDiscountGroup, 0.5f, 100);
            default:
                return CreateFromRule("STR_WRD_001", 60, 50, 70, true, KeywordDiscountGroup, 0.5f, 500);
        }
    }

    public static StorePriceResult GetLevelUpPrice(Player player)
    {
        int level = player != null ? player.Level : 1;

        if (level <= 3)
        {
            return CreateFromRule("STR_LVL_001", 120, 100, 140, false, "None", 0f, 400);
        }

        if (level <= 6)
        {
            return CreateFromRule("STR_LVL_002", 280, 250, 310, false, "None", 0f, 400);
        }

        return CreateFromRule("STR_LVL_003", 650, 580, 720, false, "None", 0f, 200);
    }

    public static int GetWordRemovalPrice(int previousBuyCount)
    {
        StorePriceRule rule = GetRule("STR_SVC_001");
        int basePrice = rule != null ? rule.BasePrice : WordRemovalBasePrice;
        return basePrice + Mathf.Max(0, previousBuyCount) * WordRemovalPriceStep;
    }

    public static void ApplyGroupDiscounts(List<StorePriceResult> prices)
    {
        if (prices == null || prices.Count == 0)
        {
            return;
        }

        ApplyDiscount(prices, ArtifactDiscountGroup);
        ApplyDiscount(prices, KeywordDiscountGroup);
    }

    public static List<SO_ArtifactData> PickArtifactOffers(int count)
    {
        List<SO_ArtifactData> candidates = new List<SO_ArtifactData>();

        foreach (SO_ArtifactData data in DataManager.Instance.AllArtifacts)
        {
            if (data == null)
            {
                continue;
            }

            if (ArtifactSystem.Instance != null && ArtifactSystem.Instance.HasArtifact(data.ID))
            {
                continue;
            }

            if (data.OwnerJob != EPlayerJob.None && IsCurrentPartyJob(data.OwnerJob) == false)
            {
                continue;
            }

            if (IsStoreArtifactCandidate(data) == false)
            {
                continue;
            }

            candidates.Add(data);
        }

        return PickWeightedWithoutReplacement(candidates, count, data => GetArtifactPrice(data).AppearWeight);
    }

    public static List<SO_KeywordData> PickKeywordOffers(int count)
    {
        List<SO_KeywordData> candidates = new List<SO_KeywordData>();
        candidates.AddRange(DataManager.Instance.AllAdverbKeywords.Where(x => x != null));
        candidates.AddRange(DataManager.Instance.AllVerbKeywords.Where(x => x != null));

        return PickWeightedWithoutReplacement(candidates, count, data => GetKeywordPrice(data).AppearWeight);
    }

    public static List<Player> PickLevelUpOffers(int count)
    {
        List<Player> candidates = new List<Player>();

        if (CharacterSystem.Instance != null)
        {
            foreach (PlayerView playerView in CharacterSystem.Instance.Players)
            {
                if (playerView?.Player != null)
                {
                    candidates.Add(playerView.Player);
                }
            }
        }

        return PickWeightedWithoutReplacement(candidates, count, player => GetLevelUpPrice(player).AppearWeight);
    }

    private static StorePriceResult CreateRanged(
        string ruleId,
        int basePrice,
        int minPrice,
        int maxPrice,
        bool isDiscountable,
        string discountGroup,
        float discountRate,
        int appearWeight)
    {
        int low = Mathf.Min(minPrice, maxPrice);
        int high = Mathf.Max(minPrice, maxPrice);
        if (low <= 0 || high <= 0)
        {
            low = high = Mathf.Max(0, basePrice);
        }

        int price = Random.Range(low, high + 1);

        return new StorePriceResult
        {
            RuleId = ruleId,
            OriginalPrice = price,
            Price = price,
            IsDiscountable = isDiscountable,
            DiscountGroup = discountGroup,
            DiscountRate = discountRate,
            AppearWeight = appearWeight,
        };
    }

    private static StorePriceResult CreateFromRule(
        string ruleId,
        int fallbackBasePrice,
        int fallbackMinPrice,
        int fallbackMaxPrice,
        bool fallbackDiscountable,
        string fallbackDiscountGroup,
        float fallbackDiscountRate,
        int fallbackAppearWeight)
    {
        StorePriceRule rule = GetRule(ruleId);
        if (rule == null)
        {
            return CreateRanged(
                ruleId,
                fallbackBasePrice,
                fallbackMinPrice,
                fallbackMaxPrice,
                fallbackDiscountable,
                fallbackDiscountGroup,
                fallbackDiscountRate,
                fallbackAppearWeight);
        }

        return CreateRanged(
            rule.RuleId,
            rule.BasePrice,
            rule.MinPrice,
            rule.MaxPrice,
            rule.IsDiscountable,
            rule.DiscountGroup,
            rule.DiscountRate,
            rule.AppearWeight);
    }

    private static StorePriceResult CreateFixed(
        string ruleId,
        int price,
        bool isDiscountable,
        string discountGroup,
        float discountRate,
        int appearWeight)
    {
        return new StorePriceResult
        {
            RuleId = ruleId,
            OriginalPrice = price,
            Price = price,
            IsDiscountable = isDiscountable,
            DiscountGroup = discountGroup,
            DiscountRate = discountRate,
            AppearWeight = appearWeight,
        };
    }

    private static void ApplyDiscount(List<StorePriceResult> prices, string discountGroup)
    {
        List<StorePriceResult> candidates = prices
            .Where(x => x != null && x.IsDiscountable && x.DiscountGroup == discountGroup)
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        StorePriceResult selected = candidates[Random.Range(0, candidates.Count)];
        selected.Price = Mathf.RoundToInt(selected.Price * selected.DiscountRate);
    }

    private static bool IsArtifactLevelUpOrJobRule(SO_ArtifactData artifactData)
    {
        return artifactData.OwnerJob != EPlayerJob.None ||
               (artifactData.Pools & EArtifactPool.LevelUp) != 0;
    }

    private static bool IsStoreArtifactCandidate(SO_ArtifactData artifactData)
    {
        if (artifactData == null)
        {
            return false;
        }

        if (IsArtifactLevelUpOrJobRule(artifactData))
        {
            return true;
        }

        return artifactData.OwnerJob == EPlayerJob.None &&
               (artifactData.Pools & EArtifactPool.Special) != 0;
    }

    private static bool IsCurrentPartyJob(EPlayerJob job)
    {
        if (CharacterSystem.Instance == null)
        {
            return false;
        }

        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            if (playerView?.Player?.PlayerData?.PlayerJob == job)
            {
                return true;
            }
        }

        return false;
    }

    private static StorePriceRule GetRule(string ruleId)
    {
        EnsureRulesLoaded();
        return _rulesById != null && _rulesById.TryGetValue(ruleId, out StorePriceRule rule)
            ? rule
            : null;
    }

    private static void EnsureRulesLoaded()
    {
        if (_rulesById != null)
        {
            return;
        }

        _rulesById = new Dictionary<string, StorePriceRule>();

        TextAsset csv = Resources.Load<TextAsset>(CsvResourcePath);
        if (csv == null)
        {
            Debug.LogWarning($"[StorePricingService] Resources/{CsvResourcePath}.csv not found. Using fallback store price rules.");
            return;
        }

        List<Dictionary<string, string>> rows = CSVParser.Parse(csv.text);
        foreach (Dictionary<string, string> row in rows)
        {
            StorePriceRule rule = ParseRule(row);
            if (rule == null || string.IsNullOrWhiteSpace(rule.RuleId))
            {
                continue;
            }

            _rulesById[rule.RuleId] = rule;
        }
    }

    private static StorePriceRule ParseRule(Dictionary<string, string> row)
    {
        if (row == null || row.TryGetValue("Store_Rule_ID", out string ruleId) == false || string.IsNullOrWhiteSpace(ruleId))
        {
            return null;
        }

        row.TryGetValue("Category", out string categoryText);
        row.TryGetValue("Target_Condition", out string targetCondition);
        row.TryGetValue("Discount_Group", out string discountGroup);

        return new StorePriceRule
        {
            RuleId = ruleId.Trim(),
            Category = ParseCategory(categoryText),
            TargetCondition = targetCondition ?? string.Empty,
            BasePrice = ParseInt(row, "Base_Price", 0),
            MinPrice = ParseInt(row, "Min_Price", 0),
            MaxPrice = ParseInt(row, "Max_Price", 0),
            AppearWeight = ParseInt(row, "Appear_Weight", 0),
            IsDiscountable = ParseBool(row, "Is_Discountable", false),
            DiscountGroup = string.IsNullOrWhiteSpace(discountGroup) ? "None" : discountGroup.Trim(),
            DiscountRate = ParseFloat(row, "Discount_Rate", 0f),
        };
    }

    private static EStoreOfferCategory ParseCategory(string value)
    {
        if (System.Enum.TryParse(value, true, out EStoreOfferCategory category))
        {
            return category;
        }

        return EStoreOfferCategory.Service;
    }

    private static int ParseInt(Dictionary<string, string> row, string key, int fallback)
    {
        if (row.TryGetValue(key, out string value) && int.TryParse(value, out int parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static float ParseFloat(Dictionary<string, string> row, string key, float fallback)
    {
        if (row.TryGetValue(key, out string value) && float.TryParse(value, out float parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static bool ParseBool(Dictionary<string, string> row, string key, bool fallback)
    {
        if (row.TryGetValue(key, out string value) == false)
        {
            return fallback;
        }

        if (bool.TryParse(value, out bool parsed))
        {
            return parsed;
        }

        return value == "1";
    }

    private static List<T> PickWeightedWithoutReplacement<T>(List<T> candidates, int count, System.Func<T, int> getWeight)
    {
        List<T> remaining = candidates != null ? new List<T>(candidates) : new List<T>();
        List<T> result = new List<T>();

        while (result.Count < count && remaining.Count > 0)
        {
            int totalWeight = 0;
            foreach (T candidate in remaining)
            {
                totalWeight += Mathf.Max(0, getWeight(candidate));
            }

            int selectedIndex = 0;
            if (totalWeight > 0)
            {
                int roll = Random.Range(0, totalWeight);
                int cursor = 0;

                for (int i = 0; i < remaining.Count; i++)
                {
                    cursor += Mathf.Max(0, getWeight(remaining[i]));
                    if (roll < cursor)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                selectedIndex = Random.Range(0, remaining.Count);
            }

            result.Add(remaining[selectedIndex]);
            remaining.RemoveAt(selectedIndex);
        }

        return result;
    }
}
