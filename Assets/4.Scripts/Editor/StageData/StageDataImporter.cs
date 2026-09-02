#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StageDataImporter : CSVToSOImporter<SO_StageData>
{
    public override string ImporterName => "StageData";
    public override string CsvDirectory => "Assets/2.Data/#TextData/StageData";
    public override string SoDirectory => "Assets/2.Data/StageData";

    // ──────────────────────────────────────────────
    //  검사 로직
    // ──────────────────────────────────────────────
    protected override List<CsvValidationError> ValidateCsvData(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        var errors = new List<CsvValidationError>();
        for (int i = 0; i < csvData.Count; i++)
        {
            int rowNum = i + 2; // 데이터는 2번째 줄부터 시작 (1:헤더, 1:0-index 보정)
            var row = csvData[i];

            // 1. PosIndex (0~6) 검사 (error6)
            if (row.TryGetValue("PosIndex", out string posStr) && !string.IsNullOrWhiteSpace(posStr))
            {
                if (int.TryParse(posStr, out int posVal))
                {
                    if (posVal < 0 || posVal > 6)
                        errors.Add(new CsvValidationError(rowNum, "PosIndex", CsvError.GetError(ECsvErrorType.OutOfRange, $"{posVal} (허용: 0~6)")));
                }
                else
                {
                    errors.Add(new CsvValidationError(rowNum, "PosIndex", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{posStr} (정수 필요)")));
                }
            }

            // 2. EnemyId 참조 검사 (error7) - SO 대신 CSV 데이터를 기준으로 검사
            if (row.TryGetValue("EnemyId", out string enemyIdStr) && !string.IsNullOrWhiteSpace(enemyIdStr))
            {
                var enemyImporter = DataImporterBase.GetImporter("EnemyData");
                if (enemyImporter != null)
                {
                    var enemyIds = enemyImporter.GetCsvIdCache();
                    if (!enemyIds.Contains(enemyIdStr))
                    {
                        errors.Add(new CsvValidationError(rowNum, "EnemyId", CsvError.GetError(ECsvErrorType.ReferenceNotFound, enemyIdStr)));
                    }
                }
            }
        }
        return errors;
    }

    protected override void ProcessImport(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        // 1. StageIndex를 기준으로 데이터를 그룹화
        var stageGroups = CSVParser.GroupBy(csvData, "StageIndex");

        foreach (var stageKvp in stageGroups)
        {
            string stageIndexStr = stageKvp.Key;
            var stageRows = stageKvp.Value;

            if (string.IsNullOrWhiteSpace(stageIndexStr))
                continue;

            // 2. StageIndex 별로 SO_StageData 에셋 생성 또는 로드
            string assetName = $"SO_StageData_{stageIndexStr}";
            SO_StageData so = GetOrCreateSO(assetName);

            var floorGroups = CSVParser.GroupBy(stageRows, "Floor");
            var serializedObject = new SerializedObject(so);

            // MapConfigData 설정 (해당 스테이지 데이터의 첫 번째 줄에서 읽기)
            if (stageRows.Count > 0 && stageRows[0].TryGetValue("MapConfig", out string mapConfigName) && !string.IsNullOrWhiteSpace(mapConfigName))
            {
                var mapConfigSO = FindMapConfigAsset(mapConfigName);
                if (mapConfigSO != null)
                {
                    serializedObject.FindProperty("<MapConfigData>k__BackingField").objectReferenceValue = mapConfigSO;
                    serializedObject.ApplyModifiedProperties(); // GetMapConfig에서 최신 정보를 쓰기 위해 미리 적용
                }
                else
                {
                    Debug.LogWarning($"[StageDataImporter] MapConfig 에셋을 찾을 수 없습니다: {mapConfigName}");
                }
            }
            
            GetMapConfig(so, out int totalFloors);

            // 일반 층 세팅
            var matchupsProp = serializedObject.FindProperty("<MatchupDatas>k__BackingField");
            matchupsProp.arraySize = totalFloors;

            for (int f = 0; f < totalFloors; f++)
            {
                string key = (f + 1).ToString();
                var floorProp = matchupsProp.GetArrayElementAtIndex(f);
                var bundlesProp = floorProp.FindPropertyRelative("<MatchupEnemyBundles>k__BackingField");

                if (floorGroups.TryGetValue(key, out var floorRows))
                {
                    var bundles = CSVParser.SubGroupByInt(floorRows, "BundleIndex");
                    bundlesProp.arraySize = bundles.Count;
                    int bIdx = 0;
                    foreach (var kvp in bundles)
                    {
                        SetBundleFromCSV(bundlesProp.GetArrayElementAtIndex(bIdx), kvp.Value);
                        bIdx++;
                    }
                }
                else
                {
                    bundlesProp.arraySize = 0;
                }
            }

            // 보스 세팅
            SetSpecialMatchup(serializedObject, "<BossMatchupData>k__BackingField", floorGroups, "Boss");

            // 엘리트 세팅
            SetSpecialMatchup(serializedObject, "<EliteMatchupData>k__BackingField", floorGroups, "Elite");

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);
        }
    }

    private void SetSpecialMatchup(SerializedObject so, string propName, Dictionary<string, List<Dictionary<string, string>>> groups, string key)
    {
        var dataProp = so.FindProperty(propName);
        var bundlesProp = dataProp.FindPropertyRelative("<MatchupEnemyBundles>k__BackingField");

        if (groups.TryGetValue(key, out var rows))
        {
            var bundles = CSVParser.SubGroupByInt(rows, "BundleIndex");
            bundlesProp.arraySize = bundles.Count;
            int bIdx = 0;
            foreach (var kvp in bundles)
            {
                SetBundleFromCSV(bundlesProp.GetArrayElementAtIndex(bIdx), kvp.Value);
                bIdx++;
            }
        }
        else
        {
            bundlesProp.arraySize = 0;
        }
    }

    protected override void OnPostImportAll()
    {
        // 프로젝트 내의 모든 SO_StageData 찾기
        string[] guids = AssetDatabase.FindAssets("t:SO_StageData");
        List<SO_StageData> allStages = new List<SO_StageData>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_StageData>(path);
            if (asset != null) allStages.Add(asset);
        }

        // SO_DB 찾아서 업데이트
        string[] dbGuids = AssetDatabase.FindAssets("t:SO_DB");
        if (dbGuids.Length > 0)
        {
            string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
            SO_DB db = AssetDatabase.LoadAssetAtPath<SO_DB>(dbPath);
            if (db != null)
            {
                var so = new SerializedObject(db);
                var prop = so.FindProperty("<AllStageData>k__BackingField");
                prop.arraySize = allStages.Count;
                for (int i = 0; i < allStages.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = allStages[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(db);
                Debug.Log($"[{ImporterName}] SO_DB의 AllStageData를 {allStages.Count}개로 업데이트했습니다.");
            }
        }
    }

    private void GetMapConfig(SO_StageData targetStage, out int totalFloors)
    {
        totalFloors = 15;
        if (targetStage.MapConfigData == null) return;

        var configSO = new SerializedObject(targetStage.MapConfigData);
        var mapSizeProp = configSO.FindProperty("<MapSize>k__BackingField");
        if (mapSizeProp != null)
            totalFloors = mapSizeProp.vector2IntValue.y;
    }

    private void SetBundleFromCSV(SerializedProperty bundleProp, List<Dictionary<string, string>> enemyRows)
    {
        var enemiesProp = bundleProp.FindPropertyRelative("<MatchupEnemies>k__BackingField");
        enemiesProp.arraySize = enemyRows.Count;

        for (int i = 0; i < enemyRows.Count; i++)
        {
            var ep = enemiesProp.GetArrayElementAtIndex(i);
            var enemyDataProp = ep.FindPropertyRelative("<Enemy>k__BackingField");
            var posProp = ep.FindPropertyRelative("<EnemyPosIndex>k__BackingField");

            string enemyIdStr = enemyRows[i].TryGetValue("EnemyId", out var idStr) ? idStr : "";
            string posStr = enemyRows[i].TryGetValue("PosIndex", out var p) ? p : "0";

            string assetName = string.IsNullOrEmpty(enemyIdStr) ? "" : $"SO_EnemyData_{enemyIdStr}";
            enemyDataProp.objectReferenceValue = FindEnemyAsset(assetName);
            posProp.intValue = int.TryParse(posStr, out int posVal) ? posVal : 0;
        }
    }

    private SO_EnemyData FindEnemyAsset(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:SO_EnemyData");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_EnemyData>(assetPath);
            if (asset != null && asset.name == assetName) return asset;
        }
        return null;
    }

    private SO_MapConfigData FindMapConfigAsset(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:SO_MapConfigData");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_MapConfigData>(assetPath);
            if (asset != null && asset.name == assetName) return asset;
        }
        return null;
    }
}
#endif
