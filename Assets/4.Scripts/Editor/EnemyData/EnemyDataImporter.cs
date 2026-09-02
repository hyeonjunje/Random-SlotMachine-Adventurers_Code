using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class EnemyDataImporter : CSVToSOImporter<SO_EnemyData>
{
    public override string ImporterName => "EnemyData";
    public override string CsvDirectory => "Assets/2.Data/#TextData/EnemyData";
    public override string SoDirectory => "Assets/2.Data/Character/Enemy";

    // ──────────────────────────────────────────────
    //  검사 로직
    // ──────────────────────────────────────────────
    protected override List<CsvValidationError> ValidateCsvData(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        var errors = new List<CsvValidationError>();

        // 규칙 1: ID 중복 검사 (error2)
        var seenIds = new HashSet<string>();
        for (int i = 0; i < csvData.Count; i++)
        {
            if (!csvData[i].TryGetValue("Id", out string idStr) || string.IsNullOrWhiteSpace(idStr))
                continue;
            if (!seenIds.Add(idStr))
                errors.Add(new CsvValidationError(i + 2, "Id", CsvError.GetError(ECsvErrorType.DuplicateId, idStr))); // i+2: 2번째 줄부터 데이터 시작
        }

        for (int i = 0; i < csvData.Count; i++)
        {
            int rowNum = i + 2; // 데이터는 2번째 줄부터 시작 (1:헤더, 1:0-index 보정)
            var row = csvData[i];

            // 규칙 2: CharacterPrefabName (error1, error4)
            if (row.TryGetValue("CharacterPrefabName", out string prefabName))
            {
                if (string.IsNullOrWhiteSpace(prefabName))
                    errors.Add(new CsvValidationError(rowNum, "CharacterPrefabName", CsvError.GetError(ECsvErrorType.EmptyValue, "공백")));
                else if (FindGameObject(prefabName) == null)
                    errors.Add(new CsvValidationError(rowNum, "CharacterPrefabName", CsvError.GetError(ECsvErrorType.FileNotFound, prefabName)));
            }

            // 규칙 3: ColliderOffset (error3)
            if (row.TryGetValue("ColliderOffset", out string offsetStr) && !string.IsNullOrWhiteSpace(offsetStr))
            {
                if (!IsValidVector2Format(offsetStr))
                    errors.Add(new CsvValidationError(rowNum, "ColliderOffset", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{offsetStr} (기대: x/y)")));
            }

            // 규칙 4: ColliderSize (error3)
            if (row.TryGetValue("ColliderSize", out string sizeStr) && !string.IsNullOrWhiteSpace(sizeStr))
            {
                if (!IsValidVector2Format(sizeStr))
                    errors.Add(new CsvValidationError(rowNum, "ColliderSize", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{sizeStr} (기대: x/y)")));
            }

            // 규칙 5: EnemyAI (error1, error4)
            if (row.TryGetValue("EnemyAI", out string enemyAiName))
            {
                if (string.IsNullOrWhiteSpace(enemyAiName))
                    errors.Add(new CsvValidationError(rowNum, "EnemyAI", CsvError.GetError(ECsvErrorType.EmptyValue, "공백")));
                else if (FindEnemyAI(enemyAiName) == null)
                    errors.Add(new CsvValidationError(rowNum, "EnemyAI", CsvError.GetError(ECsvErrorType.FileNotFound, enemyAiName)));
            }
        }

        return errors;
    }

    private bool IsValidVector2Format(string str)
    {
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

            // SO 에셋 이름 결정 (CharacterPrefabName이 SO_EnemyData_ 로 시작하면 그걸 이름으로 쓰고, 아니면 Id 기반)
            string assetName = $"SO_EnemyData_{idStr}";
            if (row.TryGetValue("CharacterPrefabName", out string pName) && pName.StartsWith("SO_EnemyData_"))
            {
                assetName = pName;
            }
            
            SO_EnemyData so = GetOrCreateSO(assetName);
            var serializedObject = new SerializedObject(so);

            // 1. Id (int)
            if (int.TryParse(idStr, out int id))
            {
                serializedObject.FindProperty("<Id>k__BackingField").intValue = id;
            }

            // 2. Name (string)
            if (row.TryGetValue("Name", out string name))
            {
                serializedObject.FindProperty("<Name>k__BackingField").stringValue = name;
            }

            // 3. CharacterPrefabName -> CharacterPrefab (GameObject)
            if (row.TryGetValue("CharacterPrefabName", out string prefabName) && !string.IsNullOrWhiteSpace(prefabName))
            {
                serializedObject.FindProperty("<CharacterPrefab>k__BackingField").objectReferenceValue = FindGameObject(prefabName);
            }

            // 4. PortraitIconName -> PortraitIconName (string)
            if (row.TryGetValue("PortraitIconName", out string portraitName) && !string.IsNullOrWhiteSpace(portraitName))
            {
                serializedObject.FindProperty("<PortraitIconName>k__BackingField").stringValue = portraitName;
            }

            // 5. SubjectIconName -> SubjectIconName (string)
            if (row.TryGetValue("SubjectIconName", out string subjectIconName) && !string.IsNullOrWhiteSpace(subjectIconName))
            {
                serializedObject.FindProperty("<SubjectIconName>k__BackingField").stringValue = subjectIconName;
            }

            // 6 & 7. MaxHp, Atk -> Stats
            var statsProp = serializedObject.FindProperty("<Stats>k__BackingField");
            if (row.TryGetValue("MaxHp", out string maxHpStr) && int.TryParse(maxHpStr, out int maxHp))
            {
                statsProp.FindPropertyRelative("maxHp").intValue = maxHp;
            }
            if (row.TryGetValue("Atk", out string atkStr) && int.TryParse(atkStr, out int atk))
            {
                statsProp.FindPropertyRelative("attackPower").intValue = atk;
            }

            // 8 & 9. MaxHpDiffPerLevel, AtkDiffPerLevel -> LevelUpIncrements
            var levelUpProp = serializedObject.FindProperty("<LevelUpIncrements>k__BackingField");
            if (row.TryGetValue("MaxHpDiffPerLevel", out string maxHpDiffStr) && int.TryParse(maxHpDiffStr, out int maxHpDiff))
            {
                levelUpProp.FindPropertyRelative("maxHp").intValue = maxHpDiff;
            }
            if (row.TryGetValue("AtkDiffPerLevel", out string atkDiffStr) && int.TryParse(atkDiffStr, out int atkDiff))
            {
                levelUpProp.FindPropertyRelative("attackPower").intValue = atkDiff;
            }

            // 10 & 11. ColliderOffset, ColliderSize
            if (row.TryGetValue("ColliderOffset", out string offsetStr))
            {
                serializedObject.FindProperty("<ColliderOffset>k__BackingField").vector2Value = ParseVector2(offsetStr);
            }
            if (row.TryGetValue("ColliderSize", out string sizeStr))
            {
                serializedObject.FindProperty("<ColliderSize>k__BackingField").vector2Value = ParseVector2(sizeStr);
            }

            // 12. EnemyAI -> EnemyAI (SO_EnemyAI)
            if (row.TryGetValue("EnemyAI", out string enemyAiName) && !string.IsNullOrWhiteSpace(enemyAiName))
            {
                serializedObject.FindProperty("<EnemyAI>k__BackingField").objectReferenceValue = FindEnemyAI(enemyAiName);
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

    private SO_EnemyAI FindEnemyAI(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:SO_EnemyAI");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_EnemyAI>(path);
            if (asset != null && asset.name == assetName) return asset;
        }
        return null;
    }

    private Vector2 ParseVector2(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return Vector2.zero;
        
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
        // 프로젝트 내의 모든 SO_EnemyData 찾기
        string[] guids = AssetDatabase.FindAssets("t:SO_EnemyData");
        List<SO_EnemyData> allData = new List<SO_EnemyData>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_EnemyData>(path);
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
                var prop = so.FindProperty("<AllEnemyData>k__BackingField");
                prop.arraySize = allData.Count;
                for (int i = 0; i < allData.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = allData[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(db);
                Debug.Log($"[{ImporterName}] SO_DB의 AllEnemyData를 {allData.Count}개로 업데이트했습니다.");
            }
        }
    }
}
#endif
