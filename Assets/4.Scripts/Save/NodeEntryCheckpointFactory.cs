using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class NodeEntryCheckpointFactory
{
    public static NodeEntryCheckpoint Create(SO_StageData stageData, MapNode node, int bossMatchupIndex)
    {
        NodeEntryCheckpoint checkpoint = new NodeEntryCheckpoint
        {
            GridX = node.GridPosition.x,
            GridY = node.GridPosition.y,
            NodeType = node.NodeType
        };

        switch (node.NodeType)
        {
            case EMapNodeType.Monster:
            case EMapNodeType.Elite:
            case EMapNodeType.Boss:
                checkpoint.Battle = CreateBattle (stageData, node, bossMatchupIndex);
                break;

            case EMapNodeType.Event:
                checkpoint.Event = CreateEvent ();
                break;

            case EMapNodeType.Shop:
                checkpoint.Shop = CreateShop ();
                break;

            case EMapNodeType.Treasure:
                checkpoint.Treasure = CreateTreasure ();
                break;
        }

        return checkpoint;
    }

    private static BattleCheckpoint CreateBattle(SO_StageData stageData, MapNode node, int bossMatchupIndex)
    {
        if (node.NodeType == EMapNodeType.Boss)
        {
            return new BattleCheckpoint
            {
                BattleType = EMapNodeType.Boss,
                MatchupIndex = bossMatchupIndex
            };
        }

        MatchupEnemyBundle[] bundles = node.NodeType == EMapNodeType.Elite
            ? stageData.EliteMatchupData.MatchupEnemyBundles
            : stageData.MatchupDatas[node.GridPosition.y].MatchupEnemyBundles;

        return new BattleCheckpoint
        {
            BattleType = node.NodeType,
            MatchupIndex = Random.Range (0, bundles.Length)
        };
    }

    private static EventCheckpoint CreateEvent()
    {
        SO_EventData picked = MyEventSystem.Instance.PickRandomEventForSave ();
        int eventIndex = DataManager.Instance.AllEvents.ToList ().IndexOf (picked);

        return new EventCheckpoint
        {
            EventIndex = eventIndex
        };
    }

    private static ShopCheckpoint CreateShop()
    {
        ShopCheckpoint checkpoint = new ShopCheckpoint ();

        List<StorePriceResult> discountablePrices = new List<StorePriceResult> ();

        foreach (Player player in StorePricingService.PickLevelUpOffers (3))
        {
            StorePriceResult price = StorePricingService.GetLevelUpPrice (player);
            checkpoint.CharacterSubjects.Add (player.PlayerData.SubjectKeyword);
            checkpoint.CharacterOfferPrices.Add (price.Price);
        }

        foreach (SO_KeywordData keyword in StorePricingService.PickKeywordOffers (3))
        {
            StorePriceResult price = StorePricingService.GetKeywordPrice (keyword);
            checkpoint.KeywordOffers.Add (keyword.Keyword);
            checkpoint.KeywordOfferOriginalPrices.Add (price.OriginalPrice);
            checkpoint.KeywordOfferPrices.Add (price.Price);
            discountablePrices.Add (price);
        }

        foreach (SO_ArtifactData artifact in StorePricingService.PickArtifactOffers (3))
        {
            StorePriceResult price = StorePricingService.GetArtifactPrice (artifact);
            checkpoint.ArtifactOffers.Add (artifact.ID);
            checkpoint.ArtifactOfferOriginalPrices.Add (price.OriginalPrice);
            checkpoint.ArtifactOfferPrices.Add (price.Price);
            discountablePrices.Add (price);
        }

        StorePricingService.ApplyGroupDiscounts (discountablePrices);
        CopyDiscountedPrices (checkpoint, discountablePrices);

        int wordRemovalBuyCount = DataManager.Instance?.GameModel != null
            ? DataManager.Instance.GameModel.WordRemovalBuyCount
            : 0;
        checkpoint.WordRemovalPrice = StorePricingService.GetWordRemovalPrice (wordRemovalBuyCount);

        return checkpoint;
    }

    private static void CopyDiscountedPrices(ShopCheckpoint checkpoint, List<StorePriceResult> discountablePrices)
    {
        int cursor = 0;

        for (int i = 0; i < checkpoint.KeywordOfferPrices.Count && cursor < discountablePrices.Count; i++, cursor++)
        {
            checkpoint.KeywordOfferPrices[i] = discountablePrices[cursor].Price;
        }

        for (int i = 0; i < checkpoint.ArtifactOfferPrices.Count && cursor < discountablePrices.Count; i++, cursor++)
        {
            checkpoint.ArtifactOfferPrices[i] = discountablePrices[cursor].Price;
        }
    }

    private static TreasureCheckpoint CreateTreasure()
    {
        TreasureCheckpoint checkpoint = new TreasureCheckpoint ();

        foreach (SO_ArtifactData artifact in ArtifactSystem.Instance.GetRandomRewardArtifacts (3))
        {
            checkpoint.ArtifactRewards.Add (artifact.ID);
        }

        return checkpoint;
    }
}
