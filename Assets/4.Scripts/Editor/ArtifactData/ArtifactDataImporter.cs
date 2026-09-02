#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ArtifactDataImporter : CSVToSOImporter<SO_ArtifactData>
{
    private readonly List<SO_ArtifactData> _allArtifacts = new List<SO_ArtifactData>();

    public override string ImporterName => "ArtifactData";
    public override string CsvDirectory => "Assets/2.Data/#TextData/ArtifactData";
    public override string SoDirectory => "Assets/2.Data/ArtifactData";

    public override void ImportAll()
    {
        _allArtifacts.Clear();
        base.ImportAll();
    }

    protected override void ProcessImport(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        List<Dictionary<string, string>> filteredRows = csvData
            .Where(IsArtifactDataRow)
            .ToList();

        Dictionary<string, List<Dictionary<string, string>>> artifactGroups = CSVParser.GroupBy(filteredRows, "ArtifactId");

        foreach (KeyValuePair<string, List<Dictionary<string, string>>> group in artifactGroups)
        {
            string artifactIdText = group.Key.Trim();
            if (string.IsNullOrWhiteSpace(artifactIdText))
            {
                continue;
            }

            string artifactEnumText = NormalizeArtifactIdText(artifactIdText);
            if (!Enum.TryParse(artifactEnumText, true, out EArtifactId artifactId))
            {
                Debug.LogWarning($"[ArtifactDataImporter] Invalid ArtifactId: {artifactIdText} ({csvAssetPath})");
                continue;
            }

            string assetName = $"SO_ArtifactData_{artifactIdText}";
            SO_ArtifactData artifactAsset = GetOrCreateSO(assetName);
            List<Dictionary<string, string>> rows = group.Value;
            List<ArtifactTrigger> triggers = BuildTriggers(rows);
            if (triggers.Count == 0)
            {
                continue;
            }

            Dictionary<string, string> metadataRow = rows[0];
            ApplyArtifactData(artifactAsset, artifactId, artifactIdText, metadataRow, triggers);
        }
    }

    protected override void OnPostImportAll()
    {
        string[] dbGuids = AssetDatabase.FindAssets("t:SO_DB");
        if (dbGuids.Length == 0)
        {
            Debug.LogWarning("[ArtifactDataImporter] SO_DB asset not found.");
            return;
        }

        string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
        SO_DB db = AssetDatabase.LoadAssetAtPath<SO_DB>(dbPath);
        if (db == null)
        {
            Debug.LogWarning($"[ArtifactDataImporter] Failed to load SO_DB: {dbPath}");
            return;
        }

        var serializedObject = new SerializedObject(db);
        ApplyArtifactArray(serializedObject.FindProperty("<AllArtifacts>k__BackingField"), _allArtifacts);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
    }

    private List<ArtifactTrigger> BuildTriggers(List<Dictionary<string, string>> rows)
    {
        var triggers = new List<ArtifactTrigger>();
        var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Dictionary<string, string> row in rows)
        {
            string rowKey = string.Join("|#|",
                GetValue(row, "TriggerType"),
                GetValue(row, "TriggerArg"),
                GetValue(row, "Condition"),
                GetValue(row, "Effects"));

            if (!seenRows.Add(rowKey))
            {
                continue;
            }

            ArtifactTrigger trigger = ArtifactDSLParser.ParseTrigger(
                GetValue(row, "TriggerType"),
                GetValue(row, "TriggerArg"),
                GetValue(row, "Condition"),
                GetValue(row, "Effects"));

            if (trigger != null)
            {
                triggers.Add(trigger);
            }
        }

        return triggers;
    }

    private void ApplyArtifactData(
        SO_ArtifactData artifactAsset,
        EArtifactId artifactId,
        string artifactIdText,
        Dictionary<string, string> row,
        List<ArtifactTrigger> triggers)
    {
        var serializedObject = new SerializedObject(artifactAsset);
        serializedObject.Update();

        serializedObject.FindProperty("<ID>k__BackingField").enumValueIndex = (int)artifactId;
        serializedObject.FindProperty("<Description>k__BackingField").stringValue = GetValue(row, "Description");
        serializedObject.FindProperty("<Price>k__BackingField").intValue = ParsePrice(GetValue(row, "Price"), artifactIdText);
        serializedObject.FindProperty("<Pools>k__BackingField").intValue = (int)ParsePools(GetValue(row, "Pools"));

        SerializedProperty ownerJobProp = serializedObject.FindProperty("<OwnerJob>k__BackingField");
        EPlayerJob ownerJob = ParseOwnerJob(row, artifactIdText);
        int ownerJobIndex = Array.IndexOf(ownerJobProp.enumNames, ownerJob.ToString());
        ownerJobProp.enumValueIndex = ownerJobIndex >= 0 ? ownerJobIndex : 0;

        Sprite icon = FindArtifactIcon(artifactIdText);
        if (icon != null)
        {
            serializedObject.FindProperty("<Icon>k__BackingField").objectReferenceValue = icon;
        }

        SerializedProperty logicsProp = serializedObject.FindProperty("Logics");
        logicsProp.arraySize = triggers.Count;
        for (int i = 0; i < triggers.Count; i++)
        {
            logicsProp.GetArrayElementAtIndex(i).managedReferenceValue = triggers[i];
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(artifactAsset);

        AddUnique(_allArtifacts, artifactAsset);
    }

    private static string NormalizeArtifactIdText(string artifactIdText)
    {
        return string.IsNullOrWhiteSpace(artifactIdText)
            ? string.Empty
            : new string(artifactIdText.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private Sprite FindArtifactIcon(string artifactIdText)
    {
        string iconName = $"Artifact_{artifactIdText}";
        string[] guids = AssetDatabase.FindAssets($"{iconName} t:Sprite");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null && sprite.name == iconName)
            {
                return sprite;
            }
        }

        Debug.LogWarning($"[ArtifactDataImporter] Icon not found: {iconName}");
        return null;
    }

    private static EArtifactPool ParsePools(string poolsText)
    {
        EArtifactPool pools = EArtifactPool.None;
        if (string.IsNullOrWhiteSpace(poolsText))
        {
            return pools;
        }

        string[] tokens = poolsText.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens.Select(value => value.Trim()))
        {
            if (string.Equals(token, "Starter", StringComparison.OrdinalIgnoreCase))
            {
                pools |= EArtifactPool.Starter;
            }
            else if (string.Equals(token, "Special", StringComparison.OrdinalIgnoreCase))
            {
                pools |= EArtifactPool.Special;
            }
            else if (string.Equals(token, "LevelUp", StringComparison.OrdinalIgnoreCase))
            {
                pools |= EArtifactPool.LevelUp;
            }
        }

        return pools;
    }

    private static EPlayerJob ParseOwnerJob(Dictionary<string, string> row, string artifactIdText)
    {
        string rawJob = GetValueByAliases(row, "\uC9C1\uC5C5", "\uCE90\uB9AD\uD130", "Job", "OwnerJob", "Character", "OwnerCharacter");
        if (string.IsNullOrWhiteSpace(rawJob))
        {
            return EPlayerJob.None;
        }

        rawJob = rawJob.Trim();
        switch (rawJob)
        {
            case "전사":
                return EPlayerJob.Warrior;
            case "드워프":
                return EPlayerJob.Dwarf;
            case "궁수":
                return EPlayerJob.Archer;
            case "사제":
                return EPlayerJob.Priest;
            case "도적":
                return EPlayerJob.Rogue;
            case "공용":
            case "전체":
                return EPlayerJob.None;
        }

        if (Enum.TryParse(rawJob, true, out EPlayerJob ownerJob))
        {
            if (ownerJob == EPlayerJob.Any)
            {
                Debug.LogWarning(
                    $"[ArtifactDataImporter] {artifactIdText} row cannot use 'Any' for OwnerJob. " +
                    "Use a concrete EPlayerJob value such as Warrior, Rogue, Archer, Priest, or Dwarf.");
                return EPlayerJob.None;
            }

            return ownerJob;
        }

        Debug.LogWarning(
            $"[ArtifactDataImporter] {artifactIdText} row has an invalid 직업 value: '{rawJob}'. " +
            "Use an EPlayerJob value such as Warrior, Rogue, Archer, Priest, or Dwarf.");
        return EPlayerJob.None;
    }

    private void ApplyArtifactArray(SerializedProperty property, List<SO_ArtifactData> artifacts)
    {
        var ordered = artifacts
            .Where(asset => asset != null)
            .Distinct()
            .OrderBy(asset => (int)asset.ID)
            .ThenBy(asset => asset.name)
            .ToList();

        property.arraySize = ordered.Count;
        for (int i = 0; i < ordered.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
        }
    }

    private static void AddUnique(List<SO_ArtifactData> list, SO_ArtifactData asset)
    {
        if (asset != null && !list.Contains(asset))
        {
            list.Add(asset);
        }
    }

    private static string GetValue(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value) ? value : string.Empty;
    }

    private static string GetValueByAliases(Dictionary<string, string> row, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (row.TryGetValue(key, out string value) && string.IsNullOrWhiteSpace(value) == false)
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static bool IsArtifactDataRow(Dictionary<string, string> row)
    {
        string artifactId = GetValue(row, "ArtifactId").Trim();
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            return false;
        }

        if (string.Equals(artifactId, "ArtifactId", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string triggerType = GetValue(row, "TriggerType").Trim();
        if (string.Equals(triggerType, "TriggerType", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, out int value) ? value : fallback;
    }

    private static int ParsePrice(string text, string artifactIdText)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (int.TryParse(text, out int value))
        {
            return value;
        }

        if (text.Contains("%"))
        {
            Debug.LogWarning(
                $"[ArtifactDataImporter] {artifactIdText} row has '{text}' in Price. " +
                "Price is a gold cost field, so percent values should not be stored there. It will be imported as 0.");
            return 0;
        }

        Debug.LogWarning(
            $"[ArtifactDataImporter] {artifactIdText} row has an invalid Price value: '{text}'. " +
            "Price must be an integer gold cost. It will be imported as 0.");
        return 0;
    }
}
#endif
