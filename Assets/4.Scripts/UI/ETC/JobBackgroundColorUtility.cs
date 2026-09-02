using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class JobBackgroundColorUtility
{
    private static readonly Color[] ArcherColors =
    {
        FromRgb(0xA1, 0xCF, 0x7B),
        FromRgb(0x61, 0x8E, 0x82),
        FromRgb(0x46, 0x46, 0x67),
    };

    private static readonly Color[] WarriorColors =
    {
        FromRgb(0xD3, 0x38, 0x38),
        FromRgb(0x37, 0x60, 0xD3),
        FromRgb(0x37, 0xD3, 0x61),
    };

    private static readonly Color[] PriestColors =
    {
        FromRgb(0xEA, 0x54, 0x4C),
        FromRgb(0xFF, 0x69, 0x82),
        FromRgb(0xF2, 0x96, 0xFF),
    };

    private static readonly Color[] DwarfColors =
    {
        FromRgb(0xB1, 0x54, 0x29),
        FromRgb(0xFF, 0x8C, 0x38),
        FromRgb(0x4C, 0xBA, 0x35),
    };

    private static readonly Color[] RogueColors =
    {
        FromRgb(0x37, 0x4F, 0x82),
        FromRgb(0xAB, 0x25, 0x38),
        FromRgb(0x3B, 0xA7, 0x78),
    };

    public static void CacheImagesUnderBackground(Transform searchRoot, Transform backgroundRoot, List<Image> images)
    {
        images.Clear();

        Transform root = backgroundRoot != null
            ? backgroundRoot
            : FindChildRecursive(searchRoot, "Background");

        if (root == null)
        {
            return;
        }

        images.AddRange(root.GetComponentsInChildren<Image>(true));
    }

    public static void CacheImages(Image[] configuredImages, Transform searchRoot, Transform backgroundRoot, List<Image> images)
    {
        images.Clear();

        if (configuredImages != null && configuredImages.Length > 0)
        {
            for (int i = 0; i < configuredImages.Length; i++)
            {
                if (configuredImages[i] != null)
                {
                    images.Add(configuredImages[i]);
                }
            }

            if (images.Count > 0)
            {
                return;
            }
        }

        CacheImagesUnderBackground(searchRoot, backgroundRoot, images);
    }

    public static void ApplyColor(
        IReadOnlyList<Image> images,
        SO_PlayerData playerData,
        float duration,
        bool animate,
        Object tweenTarget)
    {
        if (images == null || images.Count == 0)
        {
            return;
        }

        Color targetColor = GetColor(playerData);

        for (int i = 0; i < images.Count; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            Color imageTargetColor = targetColor;
            imageTargetColor.a = image.color.a;

            image.DOKill(false);

            if (animate && duration > 0f && image.gameObject.activeInHierarchy)
            {
                image.DOColor(imageTargetColor, duration)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(tweenTarget);
            }
            else
            {
                image.color = imageTargetColor;
            }
        }
    }

    public static void KillTweens(IReadOnlyList<Image> images)
    {
        if (images == null)
        {
            return;
        }

        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null)
            {
                images[i].DOKill(false);
            }
        }
    }

    private static Color GetColor(SO_PlayerData playerData)
    {
        Color[] palette = GetPalette(playerData != null ? playerData.PlayerJob : EPlayerJob.None);
        int colorIndex = GetColorIndex(playerData);
        return palette[colorIndex];
    }

    private static Color[] GetPalette(EPlayerJob job)
    {
        return job switch
        {
            EPlayerJob.Archer => ArcherColors,
            EPlayerJob.Warrior => WarriorColors,
            EPlayerJob.Priest => PriestColors,
            EPlayerJob.Dwarf => DwarfColors,
            EPlayerJob.Rogue => RogueColors,
            _ => WarriorColors,
        };
    }

    private static int GetColorIndex(SO_PlayerData playerData)
    {
        if (playerData == null)
        {
            return 0;
        }

        string playerName = playerData.name;
        int lastUnderscoreIndex = playerName.LastIndexOf('_');
        if (lastUnderscoreIndex < 0 || lastUnderscoreIndex >= playerName.Length - 1)
        {
            return 0;
        }

        string suffix = playerName.Substring(lastUnderscoreIndex + 1);
        if (!int.TryParse(suffix, out int playerNumber))
        {
            return 0;
        }

        int oneBasedIndex = playerNumber % 10;
        return Mathf.Clamp(oneBasedIndex - 1, 0, 2);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Color FromRgb(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }
}
