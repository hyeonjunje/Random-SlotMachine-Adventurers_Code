#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EventDataImporter : CSVToSOImporter<SO_EventData>
{
    public override string ImporterName => "EventData";
    public override string CsvDirectory => "Assets/2.Data/#TextData/EventData";
    public override string SoDirectory => "Assets/2.Data/EventData";
    
    // 복잡한(조건, 확률, 효과) 파싱 및 검증 무시 설정 (기본값 false)
    private bool ParseComplexData => true;
    
    private static readonly Dictionary<string, string> RiskRewardFolders = new()
    {
        { "RiskHighRewardHigh", "RiskHighRewardHigh" },
        { "RiskNoneRewardLow",  "RiskNoneRewardLow" },
        { "RiskHighRewardNone", "RiskHighRewardNone" },
    };

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

            // 1. RiskRewardType → EEventRiskRewardType enum 검사 (error5)
            if (row.TryGetValue("RiskRewardType", out string rrStr) && !string.IsNullOrWhiteSpace(rrStr))
            {
                if (!System.Enum.IsDefined(typeof(EEventRiskRewardType), rrStr))
                    errors.Add(new CsvValidationError(rowNum, "RiskRewardType", CsvError.GetError(ECsvErrorType.TypoCheck, rrStr)));
            }

            // 2. IsStartPage → "TRUE" 또는 "FALSE" 대소문자 구분 (error5)
            if (row.TryGetValue("IsStartPage", out string isStartStr) && !string.IsNullOrWhiteSpace(isStartStr))
            {
                if (isStartStr != "TRUE" && isStartStr != "FALSE")
                    errors.Add(new CsvValidationError(rowNum, "IsStartPage", CsvError.GetError(ECsvErrorType.TypoCheck, $"{isStartStr} (TRUE 또는 FALSE만 허용)")));
            }

            if (ParseComplexData)
            {
                // 3. Condition DSL 검사 (error3, error8, error9)
                if (row.TryGetValue("Condition", out string condStr) && !string.IsNullOrWhiteSpace(condStr))
                {
                    foreach (var issue in GameDSLParser.ValidateCondition(condStr))
                    {
                        if (issue.IsInvalidFormat)
                            errors.Add(new CsvValidationError(rowNum, "Condition", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{issue.FuncName} (해당 형식: FuncName())")));
                        else if (issue.IsUnknown)
                            errors.Add(new CsvValidationError(rowNum, "Condition", CsvError.GetError(ECsvErrorType.DslUnknownName, issue.FuncName)));
                        else if (issue.IsArgMismatch)
                            errors.Add(new CsvValidationError(rowNum, "Condition", CsvError.GetError(ECsvErrorType.DslArgMismatch, $"{issue.FuncName} (전달: {issue.Args.Length}개)")));
                    }
                }

                // 4. Effects DSL 검사 (error3, error8, error9)
                if (row.TryGetValue("Effects", out string effStr) && !string.IsNullOrWhiteSpace(effStr))
                {
                    foreach (var issue in GameDSLParser.ValidateEffects(effStr))
                    {
                        if (issue.IsInvalidFormat)
                            errors.Add(new CsvValidationError(rowNum, "Effects", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{issue.FuncName} (해당 형식: FuncName())")));
                        else if (issue.IsUnknown)
                            errors.Add(new CsvValidationError(rowNum, "Effects", CsvError.GetError(ECsvErrorType.DslUnknownName, issue.FuncName)));
                        else if (issue.IsArgMismatch)
                            errors.Add(new CsvValidationError(rowNum, "Effects", CsvError.GetError(ECsvErrorType.DslArgMismatch, $"{issue.FuncName} (전달: {issue.Args.Length}개)")));
                    }
                }

                // 5. FailedEffects DSL 검사 (error3, error8, error9)
                if (row.TryGetValue("FailedEffects", out string failStr) && !string.IsNullOrWhiteSpace(failStr))
                {
                    foreach (var issue in GameDSLParser.ValidateEffects(failStr))
                    {
                        if (issue.IsInvalidFormat)
                            errors.Add(new CsvValidationError(rowNum, "FailedEffects", CsvError.GetError(ECsvErrorType.InvalidFormat, $"{issue.FuncName} (해당 형식: FuncName())")));
                        else if (issue.IsUnknown)
                            errors.Add(new CsvValidationError(rowNum, "FailedEffects", CsvError.GetError(ECsvErrorType.DslUnknownName, issue.FuncName)));
                        else if (issue.IsArgMismatch)
                            errors.Add(new CsvValidationError(rowNum, "FailedEffects", CsvError.GetError(ECsvErrorType.DslArgMismatch, $"{issue.FuncName} (전달: {issue.Args.Length}개)")));
                    }
                }
            }
        }

        return errors;
    }

    protected override void ProcessImport(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        // 단일 문서에서 여러 EventId로 쪼개기 (1:N)
        var eventGroups = CSVParser.GroupBy(csvData, "EventId");
        
        foreach (var kvp in eventGroups)
        {
            string eventIdStr = kvp.Key;
            if (!int.TryParse(eventIdStr, out int eventId)) continue;

            var eventRows = kvp.Value;
            string riskReward = GetFirstNonEmpty(eventRows, "RiskRewardType");
            string subfolder = RiskRewardFolders.ContainsKey(riskReward) ? RiskRewardFolders[riskReward] : "Uncategorized";
            
            string assetName = $"SO_EventData_{eventId}";
            SO_EventData eventAsset = GetOrCreateSO(assetName, subfolder);

            PopulateEventData(eventAsset, eventId, eventRows);
        }
    }

    private void PopulateEventData(SO_EventData so, int eventId, List<Dictionary<string, string>> csvData)
    {
        var serializedObject = new SerializedObject(so);

        serializedObject.FindProperty("<Id>k__BackingField").intValue = eventId;
        serializedObject.FindProperty("<EventName>k__BackingField").stringValue = GetFirstNonEmpty(csvData, "EventName");

        string rrStr = GetFirstNonEmpty(csvData, "RiskRewardType");
        if (System.Enum.TryParse<EEventRiskRewardType>(rrStr, out var rrType))
        {
            serializedObject.FindProperty("<EventRiskRewardType>k__BackingField").enumValueIndex = (int)rrType;
        }

        // PageId로 그룹핑
        var pageGroups = new SortedDictionary<int, List<Dictionary<string, string>>>();
        foreach (var row in csvData)
        {
            if (!row.TryGetValue("PageId", out string pidStr) || !int.TryParse(pidStr, out int pid)) continue;
            if (!pageGroups.ContainsKey(pid)) pageGroups[pid] = new List<Dictionary<string, string>>();
            pageGroups[pid].Add(row);
        }

        var pagesProp = serializedObject.FindProperty("<PageDatas>k__BackingField");
        pagesProp.arraySize = pageGroups.Count;

        int pageIdx = 0;
        foreach (var pageKvp in pageGroups)
        {
            int pageId = pageKvp.Key;
            var pageRows = pageKvp.Value;
            var pageProp = pagesProp.GetArrayElementAtIndex(pageIdx);

            pageProp.FindPropertyRelative("<Id>k__BackingField").intValue = pageId;

            string isStartStr = GetFirstNonEmpty(pageRows, "IsStartPage");
            pageProp.FindPropertyRelative("<IsStartPage>k__BackingField").boolValue =
                isStartStr.Equals("TRUE", System.StringComparison.OrdinalIgnoreCase);

            pageProp.FindPropertyRelative("<EventExplain>k__BackingField").stringValue =
                GetFirstNonEmpty(pageRows, "EventExplain");

            string eventImageName = GetFirstNonEmpty(pageRows, "EventImage");
            Sprite foundSprite = null;
            if (!string.IsNullOrWhiteSpace(eventImageName))
            {
                string[] spriteGuids = AssetDatabase.FindAssets(eventImageName + " t:Sprite");
                foreach (string guid in spriteGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path) == eventImageName)
                    {
                        foundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        break;
                    }
                }
                
                if (foundSprite == null)
                {
                    Debug.LogWarning($"[{ImporterName}] '{eventImageName}' 이름과 일치하는 Sprite를 찾을 수 없습니다.");
                }
            }
            pageProp.FindPropertyRelative("<EventSprite>k__BackingField").objectReferenceValue = foundSprite;

            var choicesProp = pageProp.FindPropertyRelative("<Choices>k__BackingField");
            choicesProp.arraySize = pageRows.Count;

            for (int ci = 0; ci < pageRows.Count; ci++)
            {
                var row = pageRows[ci];
                var choiceProp = choicesProp.GetArrayElementAtIndex(ci);

                choiceProp.FindPropertyRelative("<ChoiceExplain>k__BackingField").stringValue = row.TryGetValue("ChoiceExplain", out var ce) ? ce : "";

                if (ParseComplexData)
                {
                    string probStr = row.TryGetValue("Probability", out var ps) ? ps : "0";
                    float.TryParse(probStr, out float prob);
                    choiceProp.FindPropertyRelative("<Probability>k__BackingField").floatValue = prob;

                    // DSL Parsing
                    choiceProp.FindPropertyRelative("<Condition>k__BackingField").managedReferenceValue = 
                        GameDSLParser.ParseCondition(row.TryGetValue("Condition", out var cd) ? cd : "");
                    
                    ApplyEffects(choiceProp.FindPropertyRelative("<Effects>k__BackingField"), row.TryGetValue("Effects", out var ed) ? ed : "");
                    ApplyEffects(choiceProp.FindPropertyRelative("<FailedEffects>k__BackingField"), row.TryGetValue("FailedEffects", out var fd) ? fd : "");
                }
            }
            pageIdx++;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(so);
    }

    private void ApplyEffects(SerializedProperty effectsProp, string dsl)
    {
        Effect[] effects = GameDSLParser.ParseEffects(dsl);
        if (effects != null)
        {
            effectsProp.arraySize = effects.Length;
            for (int i = 0; i < effects.Length; i++)
                effectsProp.GetArrayElementAtIndex(i).managedReferenceValue = effects[i];
        }
        else effectsProp.arraySize = 0;
    }

    protected override void OnPostImportAll()
    {
        // 프로젝트 내의 모든 SO_EventData 찾기
        string[] guids = AssetDatabase.FindAssets("t:SO_EventData");
        List<SO_EventData> allEvents = new List<SO_EventData>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_EventData>(path);
            if (asset != null && asset.Id != 0) allEvents.Add(asset);
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
                var prop = so.FindProperty("<AllEventData>k__BackingField");
                prop.arraySize = allEvents.Count;
                for (int i = 0; i < allEvents.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = allEvents[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(db);
                Debug.Log($"[{ImporterName}] SO_DB의 AllEventData를 {allEvents.Count}개로 업데이트했습니다.");
            }
        }
    }

    private string GetFirstNonEmpty(List<Dictionary<string, string>> rows, string column)
    {
        foreach (var row in rows)
        {
            if (row.TryGetValue(column, out string val) && !string.IsNullOrWhiteSpace(val)) return val;
        }
        return "";
    }
}
#endif
