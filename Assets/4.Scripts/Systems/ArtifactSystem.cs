using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArtifactSystem : SingletonScene<ArtifactSystem>
{
    public List<Artifact> OwnedArtifacts { get; private set; } = new List<Artifact> ();
    private Dictionary<EArtifactId, SO_ArtifactData> _artifactDatabase = new Dictionary<EArtifactId, SO_ArtifactData> ();

    protected override void OnAwakeSingleton()
    {
        base.OnAwakeSingleton ();

        foreach (var data in DataManager.Instance.AllArtifacts)
        {
            if (data == null)
            {
                continue;
            }

            if (_artifactDatabase.ContainsKey (data.ID))
            {
                continue;
            }
            _artifactDatabase.Add (data.ID, data);
        }

        ActionSystem.AttachPerformer<TriggerArtifactGA> (TriggerArtifactPerformer);
        ActionSystem.AttachPerformer<ReplaceArtifactGA>(ReplaceArtifactPerformer);

        ActionSystem.AttachPerformer<AddRandomArtifactGA>(AddRandomArtifactPerformer);
        ActionSystem.AttachPerformer<RemoveRandomArtifactGA>(RemoveRandomArtifactPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<TriggerArtifactGA>();
        ActionSystem.DetachPerformer<ReplaceArtifactGA>();
    }

    public List<SO_ArtifactData> GetRandomUnownedArtifacts(int count, System.Predicate<SO_ArtifactData> predicate = null)
    {
        List<SO_ArtifactData> candidates = new List<SO_ArtifactData> ();

        foreach (var data in _artifactDatabase.Values)
        {
            if (HasArtifact (data.ID)) continue;
            if (predicate != null && predicate (data) == false) continue;

            candidates.Add (data);
        }

        if (candidates.Count <= count)
        {
            return candidates;
        }

        List<SO_ArtifactData> result = new List<SO_ArtifactData> ();
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range (0, candidates.Count);
            result.Add (candidates[randomIndex]);
            candidates.RemoveAt (randomIndex);
        }

        return result;
    }

    public List<SO_ArtifactData> GetRandomRewardArtifacts(int count)
    {
        return GetRandomUnownedArtifacts (count, IsRewardArtifactCandidate);
    }

    public List<SO_ArtifactData> GetLevelUpArtifactCandidates(Player player, int count)
    {
        if (player == null)
        {
            return new List<SO_ArtifactData> ();
        }

        return GetRandomUnownedArtifacts (count, data =>
            IsOwnedArtifactForPlayer (data, player) &&
            HasPool (data, EArtifactPool.LevelUp));
    }

    public SO_ArtifactData GetStarterArtifactFor(Player player, HashSet<EArtifactId> excludedArtifactIds = null)
    {
        if (player == null)
        {
            return null;
        }

        List<SO_ArtifactData> starterArtifacts = GetRandomUnownedArtifacts (1, data =>
            IsOwnedArtifactForPlayer (data, player) &&
            HasPool (data, EArtifactPool.Starter) &&
            (excludedArtifactIds == null || excludedArtifactIds.Contains (data.ID) == false));

        return starterArtifacts.FirstOrDefault ();
    }

    public bool HasArtifact(EArtifactId id)
    {
        return OwnedArtifacts.Exists (x => x.Data.ID == id);
    }

    public void AddArtifact(EArtifactId id, Player explicitOwner = null)
    {
        if (OwnedArtifacts.Exists (x => x.Data.ID == id))
        {
            return;
        }

        if (_artifactDatabase.TryGetValue (id, out SO_ArtifactData data))
        {
            DataManager.Instance.GameModel.GainedArtifact++;

            Player ownerPlayer = ResolveOwnerPlayer (data, explicitOwner);
            Artifact newArtifact = new Artifact (data, ownerPlayer);
            OwnedArtifacts.Add (newArtifact);

            EventBus.Publish(new StArtifactChangedEvent(newArtifact, EArtifactChangeType.Added));
            newArtifact.OnEquip ();

            EventBus.Publish (new StArtifactTriggeredEvent (newArtifact));
        }
    }

    public void RemoveArtifact(Artifact artifact)
    {
        if (OwnedArtifacts.Contains (artifact))
        {
            artifact.OnUnequip ();
            OwnedArtifacts.Remove (artifact);
            EventBus.Publish(new StArtifactChangedEvent(artifact, EArtifactChangeType.Removed));
        }
    }

    public void ClearAllArtifacts()
    {
        foreach (Artifact artifact in OwnedArtifacts.ToArray())
        {
            artifact.OnUnequip();
            EventBus.Publish(new StArtifactChangedEvent(artifact, EArtifactChangeType.Removed));
        }

        OwnedArtifacts.Clear();
        ArtifactRuntimeState.ResetAll();
    }

    private IEnumerator ReplaceArtifactPerformer(ReplaceArtifactGA replaceArtifactGA)
    {
        if (replaceArtifactGA.ArtifactToRemove != null)
        {
            RemoveArtifact(replaceArtifactGA.ArtifactToRemove);
        }

        AddArtifact(replaceArtifactGA.ArtifactIdToAdd);

        yield return null;
    }

    private IEnumerator AddRandomArtifactPerformer(AddRandomArtifactGA addRandomArtifactGA)
    {
        List<EArtifactId> unownedArtifactIds = new List<EArtifactId>();

        foreach(SO_ArtifactData artifactData in _artifactDatabase.Values)
        {
            if (artifactData != null && OwnedArtifacts.Exists(x => x.Data.ID == artifactData.ID) == false)
            {
                unownedArtifactIds.Add(artifactData.ID);
            }
        }

        if(unownedArtifactIds.Count == 0)
        {
            EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_ARTIFACTSYSTEM_003"), EMessageType.Warning));
            yield break;
        }
        else
        {
            AddArtifact(unownedArtifactIds[Random.Range(0, unownedArtifactIds.Count)]);
        }
    }

    private IEnumerator RemoveRandomArtifactPerformer(RemoveRandomArtifactGA removeRandomArtifactGA)
    {
        if(OwnedArtifacts.Count == 0)
        {
            EventBus.Publish(new StSendMessageEvent(LocalizationManager.Instance.Get("CS_ARTIFACTSYSTEM_004"), EMessageType.Warning));
            yield break;
        }

        Artifact artifact = OwnedArtifacts[Random.Range(0, OwnedArtifacts.Count)];
        RemoveArtifact(artifact);
    }

    private IEnumerator TriggerArtifactPerformer(TriggerArtifactGA triggerArtifactGA)
    {
        EventBus.Publish (new StArtifactTriggeredEvent (triggerArtifactGA.Artifact));

        foreach (var effect in triggerArtifactGA.Effects)
        {
            ActionSystem.Instance.AddReaction (effect);
        }
        yield return null;
    }

    public void TriggerArtifactByID(EArtifactId id)
    {
        Artifact target = OwnedArtifacts.Find (x => x.Data.ID == id);

        if (target != null)
        {
            EventBus.Publish (new StArtifactTriggeredEvent (target));
        }
    }

    public bool HasPool(SO_ArtifactData data, EArtifactPool pool)
    {
        if (data == null)
        {
            return false;
        }

        return (data.Pools & pool) != 0;
    }

    private bool IsRewardArtifactCandidate(SO_ArtifactData data)
    {
        if (data == null)
        {
            return false;
        }

        if (data.OwnerJob != EPlayerJob.None)
        {
            return IsCurrentPartyOwner (data.OwnerJob) &&
                HasPool (data, EArtifactPool.Special);
        }

        return HasPool (data, EArtifactPool.Special);
    }

    private bool IsOwnedArtifactForPlayer(SO_ArtifactData data, Player player)
    {
        if (data == null || player == null)
        {
            return false;
        }

        if (data.OwnerJob == EPlayerJob.None)
        {
            return true;
        }

        return data.OwnerJob == player.PlayerData.PlayerJob;
    }

    private bool IsCurrentPartyOwner(EPlayerJob ownerJob)
    {
        if (ownerJob == EPlayerJob.None || CharacterSystem.Instance == null)
        {
            return false;
        }

        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            if (playerView?.Player?.PlayerData?.PlayerJob == ownerJob)
            {
                return true;
            }
        }

        return false;
    }

    private Player ResolveOwnerPlayer(SO_ArtifactData data, Player explicitOwner)
    {
        if (data == null || data.OwnerJob == EPlayerJob.None)
        {
            return null;
        }

        if (explicitOwner != null && explicitOwner.PlayerData.PlayerJob == data.OwnerJob)
        {
            return explicitOwner;
        }

        if (CharacterSystem.Instance == null)
        {
            return null;
        }

        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            if (playerView?.Player?.PlayerData?.PlayerJob == data.OwnerJob)
            {
                return playerView.Player;
            }
        }

        Debug.LogWarning ($"[ArtifactSystem] {data.ID} 유물의 소유 직업({data.OwnerJob})을 찾지 못했습니다.");
        return null;
    }

    public List<ArtifactSnapshot> CaptureArtifactSnapshots()
    {
        List<ArtifactSnapshot> result = new List<ArtifactSnapshot> ();

        foreach (Artifact artifact in OwnedArtifacts)
        {
            if (artifact?.Data == null)
                continue;

            result.Add (new ArtifactSnapshot
            {
                ArtifactId = artifact.Data.ID,
                OwnerSubjectKeyword = artifact.OwnerPlayer?.PlayerData?.SubjectKeyword ?? EKeyword.None
            });
        }

        return result;
    }

    public void RestoreArtifactSnapshots(List<ArtifactSnapshot> snapshots)
    {
        foreach (Artifact artifact in OwnedArtifacts.ToArray ())
        {
            artifact.OnUnequip ();
            OwnedArtifacts.Remove (artifact);
        }

        if (snapshots == null)
            return;

        foreach (ArtifactSnapshot snapshot in snapshots)
        {
            SO_ArtifactData data = DataManager.Instance.AllArtifacts.FirstOrDefault (x => x.ID == snapshot.ArtifactId);
            if (data == null)
                continue;

            Player owner = CharacterSystem.Instance.Players
                .Select (x => x.Player)
                .FirstOrDefault (x => x.PlayerData.SubjectKeyword == snapshot.OwnerSubjectKeyword);

            Artifact artifact = new Artifact (data, owner);
            OwnedArtifacts.Add (artifact);
            artifact.OnEquip ();

            EventBus.Publish (new StArtifactChangedEvent (artifact, EArtifactChangeType.Added));
        }
    }
}

