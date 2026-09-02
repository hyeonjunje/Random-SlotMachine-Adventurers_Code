using System;
using System.Collections.Generic;
using UnityEngine;

public enum ERunSavePointType
{
    OnMap = 0,
    InNodeEntry = 1,
}

[Serializable]
public sealed class RunSaveData
{
    public int Version = 1;
    public ERunSavePointType SavePointType = ERunSavePointType.OnMap;
    public int BossMatchupIndex;
    public RunSnapshot Snapshot = new RunSnapshot ();
    public NodeEntryCheckpoint Checkpoint;
}

[Serializable]
public sealed class RunSnapshot
{
    public GameModelSnapshot GameModel = new GameModelSnapshot ();
    public PartySnapshot Party = new PartySnapshot ();
    public InventorySnapshot Inventory = new InventorySnapshot ();
    public SerializableMapData MapData;
}

[Serializable]
public sealed class PartySnapshot
{
    public int CurrentHp;
    public List<PlayerSnapshot> Players = new List<PlayerSnapshot> ();
}

[Serializable]
public sealed class PlayerSnapshot
{
    public EKeyword SubjectKeyword;
    public int Level;
}

[Serializable]
public sealed class InventorySnapshot
{
    public int Gold;
    public List<ArtifactSnapshot> Artifacts = new List<ArtifactSnapshot> ();
}

[Serializable]
public sealed class ArtifactSnapshot
{
    public EArtifactId ArtifactId;
    public EKeyword OwnerSubjectKeyword = EKeyword.None;
}

[Serializable]
public sealed class NodeEntryCheckpoint
{
    public int GridX;
    public int GridY;
    public EMapNodeType NodeType;

    public BattleCheckpoint Battle;
    public EventCheckpoint Event;
    public ShopCheckpoint Shop;
    public TreasureCheckpoint Treasure;
}

[Serializable]
public sealed class BattleCheckpoint
{
    public EMapNodeType BattleType;
    public int MatchupIndex;
}

[Serializable]
public sealed class EventCheckpoint
{
    public int EventIndex;
}

[Serializable]
public sealed class ShopCheckpoint
{
    public List<EKeyword> CharacterSubjects = new List<EKeyword> ();
    public List<int> CharacterOfferPrices = new List<int> ();
    public List<EKeyword> KeywordOffers = new List<EKeyword> ();
    public List<int> KeywordOfferPrices = new List<int> ();
    public List<int> KeywordOfferOriginalPrices = new List<int> ();
    public List<EArtifactId> ArtifactOffers = new List<EArtifactId> ();
    public List<int> ArtifactOfferPrices = new List<int> ();
    public List<int> ArtifactOfferOriginalPrices = new List<int> ();
    public int WordRemovalPrice;
}

[Serializable]
public sealed class TreasureCheckpoint
{
    public List<EArtifactId> ArtifactRewards = new List<EArtifactId> ();
}

[Serializable]
public sealed class GameModelSnapshot
{
    public List<EKeyword> SubjectKeywords = new List<EKeyword> ();
    public List<EKeyword> TempSubjectKeywords = new List<EKeyword> ();
    public List<EKeyword> AdverbKeywords = new List<EKeyword> ();
    public List<EKeyword> TempAdverbKeywords = new List<EKeyword> ();
    public List<EKeyword> VerbKeywords = new List<EKeyword> ();
    public List<EKeyword> TempVerbKeywords = new List<EKeyword> ();
    public List<EKeyword> CurseKeywords = new List<EKeyword> ();
    public List<EKeyword> TempCurseKeywords = new List<EKeyword> ();

    public float ElapsedTime;
    public int EnteredIslandCount;
    public int GainedGold;
    public int GainedArtifact;
    public int GainedKeyword;

    public bool IsAllowDiagonal;
    public int Stage;
    public int Floor;
    public int KeywordUpgradeOptionCount;
    public int WordRemovalBuyCount;

    public float SuccessProbability;
    public float GreatSuccessProbability;
    public float UltraSuccessProbability;
    public float FailureProbability;

    public float WeakeningValue;
    public float MarkingValue;
    public float EletricValue;
    public float CounterAttackValue;
    public float PunishmentAttackValue;
    public float GuardianValue;
    public float PreservationValue;

    public float DealDamageExtraValue;
    public float AddShieldExtraValue;
    public float RestHealingValue;
    public float ApplyHealingExtraValue;
    public float EarnedMoneyAmount;

    public List<float> LevelUpRankWeights = new List<float> ();
    public int RecentlyClickedBingo;

    public static GameModelSnapshot Capture(SO_GameModel model)
    {
        return new GameModelSnapshot
        {
            SubjectKeywords = new List<EKeyword>(model.SubjectKeywords),
            TempSubjectKeywords = new List<EKeyword>(model.TempSubjectKeywords),
            AdverbKeywords = new List<EKeyword>(model.AdverbKeywords),
            TempAdverbKeywords = new List<EKeyword>(model.TempAdverbKeywords),
            VerbKeywords = new List<EKeyword>(model.VerbKeywords),
            TempVerbKeywords = new List<EKeyword>(model.TempVerbKeywords),
            CurseKeywords = new List<EKeyword>(model.CurseKeywords),
            TempCurseKeywords = new List<EKeyword>(model.TempCurseKeywords),

            ElapsedTime = model.ElapsedTime,
            EnteredIslandCount = model.EnteredIslandCount,
            GainedGold = model.GainedGold,
            GainedArtifact = model.GainedArtifact,
            GainedKeyword = model.GainedKeyword,

            IsAllowDiagonal = model.IsAllowDiagonal,
            Stage = model.Stage,
            Floor = model.Floor,
            KeywordUpgradeOptionCount = model.KeywordUpgradeOptionCount,
            WordRemovalBuyCount = model.WordRemovalBuyCount,

            SuccessProbability = model.SuccessProbability,
            GreatSuccessProbability = model.GreatSuccessProbability,
            UltraSuccessProbability = model.UltraSuccessProbability,
            FailureProbability = model.FailureProbability,

            WeakeningValue = model.WeakeningValue,
            MarkingValue = model.MarkingValue,
            EletricValue = model.EletricValue,
            CounterAttackValue = model.CounterAttackValue,
            PunishmentAttackValue = model.PunishmentAttackValue,
            GuardianValue = model.GuardianValue,
            PreservationValue = model.PreservationValue,

            DealDamageExtraValue = model.DealDamageExtraValue,
            AddShieldExtraValue = model.AddShieldExtraValue,
            RestHealingValue = model.RestHealingValue,
            ApplyHealingExtraValue = model.ApplyHealingExtraValue,
            EarnedMoneyAmount = model.EarnedMoneyAmount,

            LevelUpRankWeights = new List<float>(model.LevelUpRankWeights),
            RecentlyClickedBingo = (int)model.RecentlyClickedBingo
        };
    }

    public void ApplyTo(SO_GameModel model)
    {
        model.SubjectKeywords.Clear ();
        model.SubjectKeywords.AddRange (SubjectKeywords);

        model.TempSubjectKeywords.Clear ();
        model.TempSubjectKeywords.AddRange (TempSubjectKeywords);

        model.AdverbKeywords.Clear ();
        model.AdverbKeywords.AddRange (AdverbKeywords);

        model.TempAdverbKeywords.Clear ();
        model.TempAdverbKeywords.AddRange (TempAdverbKeywords);

        model.VerbKeywords.Clear ();
        model.VerbKeywords.AddRange (VerbKeywords);

        model.TempVerbKeywords.Clear ();
        model.TempVerbKeywords.AddRange (TempVerbKeywords);

        model.CurseKeywords.Clear ();
        model.CurseKeywords.AddRange (CurseKeywords);

        model.TempCurseKeywords.Clear ();
        model.TempCurseKeywords.AddRange (TempCurseKeywords);

        model.ElapsedTime = ElapsedTime;
        model.EnteredIslandCount = EnteredIslandCount;
        model.GainedGold = GainedGold;
        model.GainedArtifact = GainedArtifact;
        model.GainedKeyword = GainedKeyword;

        model.IsAllowDiagonal = IsAllowDiagonal;
        model.Stage = Stage;
        model.Floor = Floor;
        model.KeywordUpgradeOptionCount = KeywordUpgradeOptionCount;
        model.WordRemovalBuyCount = WordRemovalBuyCount;

        if (HasSavedSlotMachineProbabilities())
        {
            model.SuccessProbability = SuccessProbability;
            model.GreatSuccessProbability = GreatSuccessProbability;
            model.UltraSuccessProbability = UltraSuccessProbability;
            model.FailureProbability = FailureProbability;
            EventBus.Publish(new StSlotMachineProbabilityChangedEvent());
        }

        model.WeakeningValue = WeakeningValue;
        model.MarkingValue = MarkingValue;
        model.EletricValue = EletricValue;
        model.CounterAttackValue = CounterAttackValue;
        model.PunishmentAttackValue = PunishmentAttackValue;
        model.GuardianValue = GuardianValue;
        model.PreservationValue = PreservationValue;

        model.DealDamageExtraValue = DealDamageExtraValue;
        model.AddShieldExtraValue = AddShieldExtraValue;
        model.RestHealingValue = RestHealingValue;
        model.ApplyHealingExtraValue = ApplyHealingExtraValue;
        model.EarnedMoneyAmount = EarnedMoneyAmount;

        model.LevelUpRankWeights = new List<float> (LevelUpRankWeights);
        model.RecentlyClickedBingo = (EBingo)RecentlyClickedBingo;
        model.ClickedKeywords.Clear ();

        DataManager.Instance.GameModel.CountElapsedTime();
    }

    private bool HasSavedSlotMachineProbabilities()
    {
        return SuccessProbability > 0f ||
               GreatSuccessProbability > 0f ||
               UltraSuccessProbability > 0f ||
               FailureProbability > 0f;
    }
}
