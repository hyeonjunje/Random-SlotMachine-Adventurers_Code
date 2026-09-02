using System;
using System.Collections.Generic;
using UnityEngine;

public static class ArtifactGuideUtility
{
    private const string ArtifactDataPrefix = "SO_ArtifactData_";
    private const string ArtifactDescriptionPrefix = "DATA_ARTIFACT_DESCRIPTION_";

    public static void ShowArtifactGuide(Artifact artifact, Transform anchor, bool isWorldSpace = false)
    {
        if (artifact == null)
        {
            return;
        }

        ShowArtifactGuide(artifact.Data, anchor, artifact.OwnerPlayer, isWorldSpace);
    }

    public static void ShowArtifactGuide(
        SO_ArtifactData artifactData,
        Transform anchor,
        Player explicitOwner = null,
        bool isWorldSpace = false)
    {
        if (artifactData == null || anchor == null || UIManager.Instance == null)
        {
            return;
        }

        string rawDescription = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(artifactData.Description)
            : artifactData.Description ?? string.Empty;
        Player owner = explicitOwner ?? ResolveOwnerPlayer(artifactData);

        HashSet<(string name, string explanation)> keywords =
            TextParser.ParseBrackets(rawDescription, owner, out string parsedDescription);

        UIManager.Instance.ShowGuidePopup(GetDisplayName(artifactData), parsedDescription, anchor, isWorldSpace);

        foreach (var keyword in keywords)
        {
            UIManager.Instance.ShowGuidePopup(keyword.name, keyword.explanation, anchor, isWorldSpace);
        }
    }

    public static void HideArtifactGuide(Transform anchor, bool isAnimate = true)
    {
        if (anchor == null || UIManager.Instance == null)
        {
            return;
        }

        UIManager.Instance.HideGuidePopup(anchor, isAnimate);
    }

    public static string GetDisplayName(SO_ArtifactData artifactData)
    {
        if (artifactData == null)
        {
            return string.Empty;
        }

        string localizationKey = GetTitleLocalizationKey(artifactData);
        if (LocalizationManager.Instance != null && string.IsNullOrEmpty(localizationKey) == false)
        {
            return LocalizationManager.Instance.Get(localizationKey);
        }

        if (string.IsNullOrEmpty(artifactData.name) == false)
        {
            return artifactData.name.StartsWith(ArtifactDataPrefix, StringComparison.Ordinal)
                ? artifactData.name.Substring(ArtifactDataPrefix.Length)
                : artifactData.name;
        }

        return artifactData.ID.ToString();
    }

    private static string GetTitleLocalizationKey(SO_ArtifactData artifactData)
    {
        if (artifactData == null)
        {
            return string.Empty;
        }

        string descriptionKey = artifactData.Description ?? string.Empty;
        if (descriptionKey.StartsWith(ArtifactDescriptionPrefix, StringComparison.Ordinal))
        {
            return $"DATA_ARTIFACT_TITLE_{descriptionKey.Substring(ArtifactDescriptionPrefix.Length)}";
        }

        return artifactData.ID.ToString();
    }

    private static Player ResolveOwnerPlayer(SO_ArtifactData artifactData)
    {
        if (artifactData == null || artifactData.OwnerJob == EPlayerJob.None || CharacterSystem.Instance == null)
        {
            return null;
        }

        foreach (PlayerView playerView in CharacterSystem.Instance.Players)
        {
            if (playerView?.Player?.PlayerData?.PlayerJob == artifactData.OwnerJob)
            {
                return playerView.Player;
            }
        }

        return null;
    }
}
