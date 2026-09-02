using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// SO_KeywordData.csv를 읽어 KeywordData ScriptableObject의
/// KeywordName / KeywordExplain / KeywordSpriteName / Rank 필드를 업데이트합니다.
///
/// ▶ CSV 경로 : Assets/2.Data/#TextData/KeywordData/SO_KeywordData.csv
/// ▶ SO 저장처 : Assets/2.Data/KeywordData/
///               └ 동사/공격동사/  (Category == "공격동사")
///               └ 동사/방어동사/  (Category == "방어동사")
///               └ 부사/          (Category == "부사")
///
/// ▶ SO 파일 이름 형식 : SO_KeywordData_Verb_{Rank}_{KeywordName}
///   · 이미 존재하는 SO가 있으면 해당 SO의 필드를 덮어씁니다.
///   · 없으면 지정된 서브폴더에 새로 생성합니다.
///
/// ▶ 검증 로직 없음 (의도적)
/// </summary>
public class KeywordDataImporter : CSVToSOImporter<SO_KeywordData>
{
    public override string ImporterName => "KeywordData";
    public override string CsvDirectory  => "Assets/2.Data/#TextData/KeywordData";
    public override string SoDirectory   => "Assets/2.Data/KeywordData";

    // ─────────────────────────────────────────────
    //  카테고리 → 서브폴더 매핑
    // ─────────────────────────────────────────────
    private static readonly Dictionary<string, string> CategoryToSubFolder = new Dictionary<string, string>
    {
        { "공격동사", "동사/공격동사" },
        { "방어동사", "동사/방어동사" },
        { "부사",     "부사"          },
    };

    // ─────────────────────────────────────────────
    //  검증 없음 — 빈 구현
    // ─────────────────────────────────────────────
    protected override List<CsvValidationError> ValidateCsvData(
        string csvAssetPath,
        List<Dictionary<string, string>> csvData)
    {
        return new List<CsvValidationError>();
    }

    // ─────────────────────────────────────────────
    //  임포트 로직
    // ─────────────────────────────────────────────
    protected override void ProcessImport(
        string csvAssetPath,
        List<Dictionary<string, string>> csvData)
    {
        foreach (var row in csvData)
        {
            // ── 필수 컬럼 읽기 ──────────────────────────
            if (!row.TryGetValue("KeywordName", out string keywordName) || string.IsNullOrWhiteSpace(keywordName))
                continue;
            if (!row.TryGetValue("KeywordNameKey", out string keywordNameKey) || string.IsNullOrWhiteSpace(keywordNameKey))
                continue;
            if (!row.TryGetValue("Rank", out string rankStr) || !int.TryParse(rankStr, out int rank))
                continue;
            if (!row.TryGetValue("Id", out string idStr) || !int.TryParse(idStr, out int id) || id < 1)
            {
                Debug.LogWarning($"[KeywordDataImporter] '{keywordName}' 의 Id가 없거나 1보다 작습니다. 임포트를 건너뜁니다.");
                continue;
            }

            int upgradedId = 0;
            if (row.TryGetValue("UpgradedId", out string upgradedIdStr) && int.TryParse(upgradedIdStr, out int parsedUpgradedId))
                upgradedId = parsedUpgradedId;

            row.TryGetValue("Category",         out string category);
            row.TryGetValue("KeywordExplain",   out string explain);
            row.TryGetValue("KeywordSpriteName", out string spriteName);

            explain    ??= "";
            spriteName ??= "";

            // ── 서브폴더 결정 ───────────────────────────
            string subFolder = "";
            if (!string.IsNullOrWhiteSpace(category) && CategoryToSubFolder.TryGetValue(category.Trim(), out string mapped))
                subFolder = mapped;

            // ── SO 이름 & 검색 ──────────────────────────
            // 명명 규칙: SO_KeywordData_Verb_{Rank}_{KeywordName}
            string assetName = $"SO_KeywordData_Verb_{rank}_{keywordName}";

            SO_KeywordData so = FindExistingSO(assetName);

            if (so == null)
            {
                // 없으면 생성
                so = GetOrCreateSO(assetName, subFolder);
            }

            // ── 필드 덮어쓰기 ───────────────────────────
            var serializedObject = new SerializedObject(so);

            serializedObject.FindProperty("Id")?.SetValue(id);
            serializedObject.FindProperty("UpgradedId")?.SetValue(upgradedId);

            serializedObject.FindProperty("KeywordName")
                ?.SetValue(keywordNameKey);
            serializedObject.FindProperty("KeywordExplain")
                ?.SetValue(explain);
            serializedObject.FindProperty("KeywordSpriteName")
                ?.SetValue(spriteName);
            serializedObject.FindProperty("Rank")
                ?.SetValue(rank);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);

            Debug.Log($"[{ImporterName}] '{so.name}' 업데이트 완료. (Rank={rank}, Name={keywordName})");
        }
    }

    // ─────────────────────────────────────────────
    //  헬퍼 — 이름으로 프로젝트 전체 검색
    // ─────────────────────────────────────────────
    private SO_KeywordData FindExistingSO(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:SO_KeywordData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SO_KeywordData>(path);
            if (asset != null && asset.name == assetName)
                return asset;
        }
        return null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  SerializedProperty 확장 — string / int 모두 지원
// ─────────────────────────────────────────────────────────────────────────────
internal static class SerializedPropertyExtensions
{
    internal static void SetValue(this SerializedProperty prop, string value)
    {
        if (prop == null) return;
        prop.stringValue = value;
    }

    internal static void SetValue(this SerializedProperty prop, int value)
    {
        if (prop == null) return;
        prop.intValue = value;
    }
}
#endif
