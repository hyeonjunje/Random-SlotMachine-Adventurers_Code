#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class HelpDataImporter : CSVToSOImporter<SO_HelpData>
{
    public override string ImporterName => "HelpData";
    public override string CsvDirectory => "Assets/2.Data/#TextData/HelpData";
    public override string SoDirectory => "Assets/2.Data/StaticData";

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

            // 1. Key → EHelpKey enum 검사 (error5)
            if (row.TryGetValue("Key", out string keyStr) && !string.IsNullOrWhiteSpace(keyStr))
            {
                if (!System.Enum.IsDefined(typeof(EHelpKey), keyStr))
                {
                    errors.Add(new CsvValidationError(rowNum, "Key", CsvError.GetError(ECsvErrorType.TypoCheck, keyStr)));
                }
            }
        }

        return errors;
    }

    protected override void ProcessImport(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        // 2.Data/StaticData 안에 위치한 SO_HelpData를 불러오거나 생성
        SO_HelpData helpDataAsset = GetOrCreateSO("SO_HelpData", "");

        var serializedObject = new SerializedObject(helpDataAsset);

        // HelpDatas 배열 프로퍼티 가져오기 (field: SerializeField로 인한 Backing Field)
        var arrayProp = serializedObject.FindProperty("<HelpDatas>k__BackingField");
        if (arrayProp == null)
        {
            Debug.LogError($"[{ImporterName}] <HelpDatas>k__BackingField 속성을 찾을 수 없습니다.");
            return;
        }

        arrayProp.arraySize = csvData.Count;

        for (int i = 0; i < csvData.Count; i++)
        {
            var row = csvData[i];
            var elementProp = arrayProp.GetArrayElementAtIndex(i);

            // 1. Key (EHelpKey)
            if (row.TryGetValue("Key", out string keyStr) && System.Enum.TryParse<EHelpKey>(keyStr, out var helpKey))
            {
                elementProp.FindPropertyRelative("<HelpKey>k__BackingField").enumValueIndex = (int)helpKey;
            }

            // 2. Title (string)
            elementProp.FindPropertyRelative("<Title>k__BackingField").stringValue = 
                row.TryGetValue("Title", out string title) ? title : "";

            // 3. Contents (string)
            // CSV에 \n 이라고 작성된 부분을 실제 개행문자로 변경
            string contents = row.TryGetValue("Contents", out string c) ? c : "";
            contents = contents.Replace("\\n", "\n");
            elementProp.FindPropertyRelative("<Contents>k__BackingField").stringValue = contents;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(helpDataAsset);
        
        Debug.Log($"[{ImporterName}] SO_HelpData 업데이트 완료. 총 {csvData.Count}개의 데이터가 적용되었습니다.");
    }
}
#endif
