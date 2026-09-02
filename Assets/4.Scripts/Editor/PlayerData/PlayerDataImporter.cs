using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class PlayerDataImporter : CSVToSOImporter<SO_PlayerData>
{
    public override string ImporterName => "PlayerData";
    public override string CsvDirectory => "Assets/2.Data/#TextData/PlayerData";
    public override string SoDirectory => "Assets/2.Data/Character/Player";

    // ──────────────────────────────────────────────
    //  검사 로직
    // ──────────────────────────────────────────────
    protected override List<CsvValidationError> ValidateCsvData(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        var errors = new List<CsvValidationError>();

        // 규칙 1: Id 중복 검사 (error2)
        var seenIds = new HashSet<string>();
        for (int i = 0; i < csvData.Count; i++)
        {
            int rowNum = i + 2; // 데이터는 2번째 줄부터 시작 (1:헤더, 1:0-index 보정)
            var row = csvData[i];

            if (!row.TryGetValue("Id", out string idStr) || string.IsNullOrWhiteSpace(idStr))
                continue;

            if (!seenIds.Add(idStr))
                errors.Add(new CsvValidationError(rowNum, "Id", CsvError.GetError(ECsvErrorType.DuplicateId, idStr)));
        }

        for (int i = 0; i < csvData.Count; i++)
        {
            int rowNum = i + 2; // 데이터는 2번째 줄부터 시작 (1:헤더, 1:0-index 보정)
            var row = csvData[i];

            // 규칙 2: PlayerJob → EPlayerJob enum 검사 (error5)
            if (row.TryGetValue("PlayerJob", out string jobStr) && !string.IsNullOrWhiteSpace(jobStr))
            {
                if (!Enum.IsDefined(typeof(EPlayerJob), jobStr))
                    errors.Add(new CsvValidationError(rowNum, "PlayerJob", CsvError.GetError(ECsvErrorType.TypoCheck, jobStr)));
            }

            // 규칙 3: KeywordName → EKeyword enum 검사 (error5)
            if (row.TryGetValue("KeywordName", out string keywordStr) && !string.IsNullOrWhiteSpace(keywordStr))
            {
                if (!Enum.IsDefined(typeof(EKeyword), keywordStr))
                    errors.Add(new CsvValidationError(rowNum, "KeywordName", CsvError.GetError(ECsvErrorType.TypoCheck, keywordStr)));
            }

            // 규칙 4: CharacterPrefabName (error1, error4)
            if (row.TryGetValue("CharacterPrefabName", out string prefabName))
            {
                if (string.IsNullOrWhiteSpace(prefabName))
                {
                    errors.Add(new CsvValidationError(rowNum, "CharacterPrefabName", CsvError.GetError(ECsvErrorType.EmptyValue, "공백")));
                }
                else if (FindGameObject(prefabName) == null)
                {
                    errors.Add(new CsvValidationError(rowNum, "CharacterPrefabName", CsvError.GetError(ECsvErrorType.FileNotFound, prefabName)));
                }
            }

            // 규칙 5: SkeletonGraphicName (error1, error4)
            if (row.TryGetValue("SkeletonGraphicName", out string skelName))
            {
                if (string.IsNullOrWhiteSpace(skelName))
                {
                    errors.Add(new CsvValidationError(rowNum, "SkeletonGraphicName", CsvError.GetError(ECsvErrorType.EmptyValue, "공백")));
                }
                else if (FindGameObject(skelName) == null)
                {
                    errors.Add(new CsvValidationError(rowNum, "SkeletonGraphicName", CsvError.GetError(ECsvErrorType.FileNotFound, skelName)));
                }
            }

            // 규칙 6: ColliderOffset / ColliderSize 포맷 (error3)
            if (row.TryGetValue("ColliderOffset", out string offsetStr) && !string.IsNullOrWhiteSpace(offsetStr))
            {
                if (!IsValidVector2Format(offsetStr))
                    errors.Add(new CsvValidationError(rowNum, "ColliderOffset", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{offsetStr} (기대: x/y)")));
            }

            if (row.TryGetValue("ColliderSize", out string sizeStr) && !string.IsNullOrWhiteSpace(sizeStr))
            {
                if (!IsValidVector2Format(sizeStr))
                    errors.Add(new CsvValidationError(rowNum, "ColliderSize", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{sizeStr} (기대: x/y)")));
            }

            if (row.TryGetValue("SelectionBackgroundIllustrationOffset", out string backgroundOffsetStr) && !string.IsNullOrWhiteSpace(backgroundOffsetStr))
            {
                if (!IsValidVector2Format(backgroundOffsetStr))
                    errors.Add(new CsvValidationError(rowNum, "SelectionBackgroundIllustrationOffset", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{backgroundOffsetStr} (기대: x/y)")));
            }

            if (row.TryGetValue("LevelUpBackgroundIllustrationOffset", out string levelUpBackgroundOffsetStr) && !string.IsNullOrWhiteSpace(levelUpBackgroundOffsetStr))
            {
                if (!IsValidVector2Format(levelUpBackgroundOffsetStr))
                    errors.Add(new CsvValidationError(rowNum, "LevelUpBackgroundIllustrationOffset", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{levelUpBackgroundOffsetStr} (기대: x/y)")));
            }
        }

        return errors;
    }

    /// <summary>
    /// {숫자}/{숫자} 형식인지 확인합니다. (소수점 포함, 음수 허용)
    /// </summary>
    private bool IsValidVector2Format(string str)
    {
        // 패턴: [선택적 부호][숫자].[선택적 소수점] / [선택적 부호][숫자].[선택적 소수점]
        return Regex.IsMatch(str.Trim(), @"^-?\d+(\.\d+)?/-?\d+(\.\d+)?$");
    }

    // ──────────────────────────────────────────────
    //  임포트 로직
    // ──────────────────────────────────────────────
    protected override void ProcessImport(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        foreach (var row in csvData)
        {
            if (!row.TryGetValue("Id", out string idStr) || string.IsNullOrWhiteSpace(idStr))
                continue;

            string assetName = $"SO_Player_{idStr}";
            SO_PlayerData so = GetOrCreateSO(assetName);

            var serializedObject = new SerializedObject(so);

            // 1. Id (int)
            if (int.TryParse(idStr, out int id))
            {
                serializedObject.FindProperty("<Id>k__BackingField").intValue = id;
            }

            // 2. PlayerJob (EPlayerJob)
            if (row.TryGetValue("PlayerJob", out string playerJobStr) && Enum.TryParse(playerJobStr, out EPlayerJob playerJob))
            {
                var prop = serializedObject.FindProperty("<PlayerJob>k__BackingField");
                int index = Array.IndexOf(prop.enumNames, playerJob.ToString());
                if (index >= 0) prop.enumValueIndex = index;

                string jobIconName = string.Format($"IconSet_Role_{ playerJob}");
                serializedObject.FindProperty("<JobIconName>k__BackingField").stringValue = jobIconName;
            }

            // 3. KeywordName -> SubjectKeyword (EKeyword)
            if (row.TryGetValue("KeywordName", out string keywordStr) && Enum.TryParse(keywordStr, out EKeyword keyword))
            {
                var prop = serializedObject.FindProperty("<SubjectKeyword>k__BackingField");
                int index = Array.IndexOf(prop.enumNames, keyword.ToString());
                if (index >= 0) prop.enumValueIndex = index;
            }
            // 4. CharacterPrefabName -> CharacterPrefab (GameObject)
            if (row.TryGetValue("CharacterPrefabName", out string prefabName) && !string.IsNullOrWhiteSpace(prefabName))
            {
                serializedObject.FindProperty("<CharacterPrefab>k__BackingField").objectReferenceValue = FindGameObject(prefabName);
            }

            // 5. PortraitIconName -> PortraitIcon (Sprite)
            if (row.TryGetValue("PortraitIconName", out string portraitName) && !string.IsNullOrWhiteSpace(portraitName))
            {
                serializedObject.FindProperty("<PortraitIconName>k__BackingField").stringValue = portraitName;
            }

            // 6. SubjectIconName -> SubjectIcon (Sprite)
            if (row.TryGetValue("SubjectIconName", out string subjectIconName) && !string.IsNullOrWhiteSpace(subjectIconName))
            {
                serializedObject.FindProperty("<SubjectIconName>k__BackingField").stringValue = subjectIconName;
            }

            // 7. SkeletonGraphicName -> CharacterSkeletonGraphic (GameObject)
            if (row.TryGetValue("SkeletonGraphicName", out string skeletonName) && !string.IsNullOrWhiteSpace(skeletonName))
            {
                serializedObject.FindProperty("<CharacterSkeletonGraphic>k__BackingField").objectReferenceValue = FindGameObject(skeletonName);
            }

            // 8 & 9. MaxHp, Atk -> Stats
            var statsProp = serializedObject.FindProperty("<Stats>k__BackingField");
            if (row.TryGetValue("MaxHp", out string maxHpStr) && int.TryParse(maxHpStr, out int maxHp))
            {
                statsProp.FindPropertyRelative("maxHp").intValue = maxHp;
            }
            if (row.TryGetValue("Atk", out string atkStr) && int.TryParse(atkStr, out int atk))
            {
                statsProp.FindPropertyRelative("attackPower").intValue = atk;
            }

            // 10 & 11. MaxHpDiffPerLevel, AtkDiffPerLevel -> LevelUpIncrements
            var levelUpProp = serializedObject.FindProperty("<LevelUpIncrements>k__BackingField");
            if (row.TryGetValue("MaxHpDiffPerLevel", out string maxHpDiffStr) && int.TryParse(maxHpDiffStr, out int maxHpDiff))
            {
                levelUpProp.FindPropertyRelative("maxHp").intValue = maxHpDiff;
            }
            if (row.TryGetValue("AtkDiffPerLevel", out string atkDiffStr) && int.TryParse(atkDiffStr, out int atkDiff))
            {
                levelUpProp.FindPropertyRelative("attackPower").intValue = atkDiff;
            }

            // 12 & 13. ColliderOffset, ColliderSize
            if (row.TryGetValue("ColliderOffset", out string offsetStr))
            {
                serializedObject.FindProperty("<ColliderOffset>k__BackingField").vector2Value = ParseVector2(offsetStr);
            }
            if (row.TryGetValue("ColliderSize", out string sizeStr))
            {
                serializedObject.FindProperty("<ColliderSize>k__BackingField").vector2Value = ParseVector2(sizeStr);
            }

            // 14. IllustrationIconName → IllustrationIconName
            if (row.TryGetValue("IllustrationName", out string illustName) && !string.IsNullOrWhiteSpace(illustName))
            {
                serializedObject.FindProperty("<IllustrationName>k__BackingField").stringValue = illustName;
            }

            if (row.TryGetValue("SelectionBackgroundIllustrationOffset", out string backgroundOffsetStr))
            {
                serializedObject.FindProperty("<SelectionBackgroundIllustrationOffset>k__BackingField").vector2Value = ParseVector2(backgroundOffsetStr);
            }

            if (row.TryGetValue("LevelUpBackgroundIllustrationOffset", out string levelUpBackgroundOffsetStr))
            {
                serializedObject.FindProperty("<LevelUpBackgroundIllustrationOffset>k__BackingField").vector2Value = ParseVector2(levelUpBackgroundOffsetStr);
            }

            // 15. CharacterLore → CharacterLore 
            if (row.TryGetValue("CharacterLore", out string lore) && !string.IsNullOrWhiteSpace(lore))
            {
                lore = lore.Replace("\\n", "\n");
                serializedObject.FindProperty("<CharacterLore>k__BackingField").stringValue = lore;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);
        }
    }

    private GameObject FindGameObject(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:GameObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null && asset.name == assetName) return asset;
        }
        return null;
    }

    private Vector2 ParseVector2(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return Vector2.zero;
        
        // str "1.5, 2.0" or "1.5/2.0" or "(1.5, 2.0)"
        str = str.Replace("(", "").Replace(")", "").Replace(" ", "");
        string[] parts = str.Split(new[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            float.TryParse(parts[0], out float x);
            float.TryParse(parts[1], out float y);
            return new Vector2(x, y);
        }
        return Vector2.zero;
    }

    protected override void OnPostImportAll()
    {
        // 프로젝트 내의 모든 SO_PlayerData 찾기
        string[] guids = AssetDatabase.FindAssets("t:SO_PlayerData");
        List<SO_PlayerData> allData = new List<SO_PlayerData>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_PlayerData>(path);
            if (asset != null) allData.Add(asset);
        }

        // Id 순으로 정렬
        allData.Sort((a, b) => a.Id.CompareTo(b.Id));

        // SO_DB 찾아서 업데이트
        string[] dbGuids = AssetDatabase.FindAssets("t:SO_DB");
        if (dbGuids.Length > 0)
        {
            string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
            SO_DB db = AssetDatabase.LoadAssetAtPath<SO_DB>(dbPath);
            if (db != null)
            {
                var so = new SerializedObject(db);
                var prop = so.FindProperty("<AllPlayerData>k__BackingField");
                prop.arraySize = allData.Count;
                for (int i = 0; i < allData.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = allData[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(db);
                Debug.Log($"[{ImporterName}] SO_DB의 AllPlayerData를 {allData.Count}개로 업데이트했습니다.");
            }
        }
    }
}
#endif
