using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSV 검사에서 발견된 개별 오류 정보입니다.
/// </summary>
public class CsvValidationError
{
    /// <summary>문서 전체에서의 실제 라인 번호 (1부터 시작, 헤더 포함)</summary>
    public int Row;
    /// <summary>문제가 발생한 열 이름 (없으면 빈 문자열)</summary>
    public string Column;
    /// <summary>오류 설명</summary>
    public string Message;

    public CsvValidationError(int row, string column, string message)
    {
        Row = row;
        Column = column;
        Message = message;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Column))
            return $"행 {Row}: {Message}";
        return $"행 {Row} [{Column}]: {Message}";
    }
}

/// <summary>
/// 하나의 CSV 파일에 대한 검사 결과를 담습니다.
/// </summary>
public class CsvValidationResult
{
    public string FilePath;
    public bool IsValid => Errors == null || Errors.Count == 0;
    public List<CsvValidationError> Errors = new List<CsvValidationError>();
}

/// <summary>
/// CSV 검사 오류 유형입니다.
/// </summary>
public enum ECsvErrorType
{
    EmptyValue,    // error1: 비어있는 값
    DuplicateId,   // error2: id값 중복
    InvalidFormat, // error3: 잘못된 포맷
    FileNotFound,  // error4: 프로젝트 내 파일 존재 X
    TypoCheck,     // error5: 오타 체크
    OutOfRange,    // error6: 허용값 넘음
    ReferenceNotFound, // error7: 참조 오류 (없는 Id)
    DslUnknownName,    // error8: DSL 오류 - 없는 이름
    DslArgMismatch     // error9: DSL 오류 - 인자 개수 불일치
}

/// <summary>
/// 중앙 집중식 CSV 오류 메시지 관리 클래스입니다.
/// </summary>
public static class CsvError
{
    public static string GetError(ECsvErrorType type, string detail)
    {
        return type switch
        {
            ECsvErrorType.EmptyValue => $"비어있는 값 ({detail})",
            ECsvErrorType.DuplicateId => $"id값 중복 ({detail})",
            ECsvErrorType.InvalidFormat => $"잘못된 포맷 ({detail})",
            ECsvErrorType.FileNotFound => $"프로젝트 내 파일 존재 X ({detail})",
            ECsvErrorType.TypoCheck => $"오타 체크 ({detail})",
            ECsvErrorType.OutOfRange => $"허용값 넘음 ({detail})",
            ECsvErrorType.ReferenceNotFound => $"참조 오류 (없는 Id: {detail})",
            ECsvErrorType.DslUnknownName => $"DSL 오류 - 없는 이름 ({detail})",
            ECsvErrorType.DslArgMismatch => $"DSL 오류 - 인자 개수 불일치 ({detail})",
            _ => $"알 수 없는 에러 ({detail})"
        };
    }
}

public abstract class DataImporterBase
{
    // ──────────────────────────────────────────────
    //  임포터 레지스트리 (교차 검사용)
    // ──────────────────────────────────────────────
    private static readonly Dictionary<string, DataImporterBase> _registry = new Dictionary<string, DataImporterBase>();

    public static void Register(DataImporterBase importer)
    {
        if (string.IsNullOrEmpty(importer.ImporterName)) return;
        _registry[importer.ImporterName] = importer;
    }

    public static DataImporterBase GetImporter(string name)
    {
        return _registry.TryGetValue(name, out var importer) ? importer : null;
    }

    // ──────────────────────────────────────────────
    //  CSV ID 캐싱 로직
    // ──────────────────────────────────────────────
    private HashSet<string> _csvIdCache;

    public void ClearCsvIdCache() => _csvIdCache = null;

    public HashSet<string> GetCsvIdCache()
    {
        if (_csvIdCache != null) return _csvIdCache;

        _csvIdCache = new HashSet<string>();
        string absoluteDir = System.IO.Path.GetFullPath(CsvDirectory);
        if (!System.IO.Directory.Exists(absoluteDir)) return _csvIdCache;

        string[] files = System.IO.Directory.GetFiles(absoluteDir, "*.csv");
        foreach (var file in files)
        {
            string text = System.IO.File.ReadAllText(file);
            var data = CSVParser.Parse(text);
            foreach (var row in data)
            {
                if (row.TryGetValue("Id", out string id) && !string.IsNullOrWhiteSpace(id))
                    _csvIdCache.Add(id);
            }
        }
        return _csvIdCache;
    }

    // ──────────────────────────────────────────────
    //  추상 속성 및 메서드
    // ──────────────────────────────────────────────
    public abstract string ImporterName { get; }
    
    /// <summary>
    /// CSV 문서가 위치할 디렉토리 경로
    /// </summary>
    public abstract string CsvDirectory { get; }
    
    /// <summary>
    /// 업데이트 하거나 생성할 스크립터블 오브젝트가 위치할 디렉토리 경로
    /// </summary>
    public abstract string SoDirectory { get; }

    /// <summary>
    /// CsvDirectory 내의 CSV 파일 경로 목록을 가져옵니다.
    /// </summary>
    public List<string> GetCsvFiles()
    {
        List<string> csvFiles = new List<string>();
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, CsvDirectory.Replace("Assets/", "")));
        
        if (Directory.Exists(fullPath))
        {
            var files = Directory.GetFiles(fullPath, "*.csv", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string relPath = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                csvFiles.Add(relPath);
            }
        }
        return csvFiles;
    }

    public abstract void ImportAll();
    
    public string PreviewCsv(string csvPath)
    {
        if (File.Exists(csvPath))
            return File.ReadAllText(csvPath);
        return "파일을 찾을 수 없습니다.";
    }

    /// <summary>
    /// 이 임포터가 담당하는 모든 CSV 파일을 검사하여 결과 목록을 반환합니다.
    /// 툴이 열릴 때 자동으로 호출됩니다.
    /// </summary>
    public List<CsvValidationResult> ValidateAll()
    {
        var results = new List<CsvValidationResult>();
        var csvFiles = GetCsvFiles();

        foreach (var csvFile in csvFiles)
        {
            var result = new CsvValidationResult { FilePath = csvFile };
            string fullPath = Path.GetFullPath(csvFile);

            if (!File.Exists(fullPath))
            {
                result.Errors.Add(new CsvValidationError(0, "", "파일을 찾을 수 없습니다."));
                results.Add(result);
                continue;
            }

            try
            {
                string csvText = File.ReadAllText(fullPath);
                var csvData = CSVParser.Parse(csvText);
                if (csvData == null || csvData.Count == 0)
                {
                    result.Errors.Add(new CsvValidationError(0, "", "CSV 데이터가 비어있거나 파싱에 실패했습니다."));
                }
                else
                {
                    var errors = ValidateCsvData(csvFile, csvData);
                    if (errors != null)
                        result.Errors.AddRange(errors);
                }
            }
            catch (System.Exception ex)
            {
                result.Errors.Add(new CsvValidationError(0, "", $"파일 읽기 오류: {ex.Message}"));
            }

            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 하위 클래스에서 오버라이드하여 CSV 데이터의 유효성을 검사합니다.
    /// 기본 구현은 항상 통과(빈 오류 목록)를 반환합니다.
    /// </summary>
    /// <param name="csvAssetPath">검사 중인 CSV 파일의 에셋 경로</param>
    /// <param name="csvData">파싱된 CSV 데이터 (행 목록, 각 행은 열이름→값 딕셔너리)</param>
    /// <returns>발견된 오류 목록. 문제가 없으면 빈 리스트를 반환합니다.</returns>
    protected virtual List<CsvValidationError> ValidateCsvData(string csvAssetPath, List<Dictionary<string, string>> csvData)
    {
        return new List<CsvValidationError>();
    }
}

public abstract class CSVToSOImporter<T> : DataImporterBase where T : ScriptableObject
{
    public override void ImportAll()
    {
        var csvFiles = GetCsvFiles();
        if (csvFiles.Count == 0)
        {
            Debug.LogWarning($"[{ImporterName}] {CsvDirectory} 경로에서 CSV 파일을 찾을 수 없습니다.");
            return;
        }

        if (!Directory.Exists(SoDirectory))
        {
            Directory.CreateDirectory(SoDirectory);
            AssetDatabase.Refresh();
        }

        foreach (var csvFile in csvFiles)
        {
            ImportCsvFile(csvFile);
        }

        OnPostImportAll();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[{ImporterName}] 모든 데이터 임포트 완료.");
    }

    private void ImportCsvFile(string csvAssetPath)
    {
        string fullCsvPath = Path.GetFullPath(csvAssetPath);
        if (!File.Exists(fullCsvPath)) return;

        var csvText = File.ReadAllText(fullCsvPath);
        var csvData = CSVParser.Parse(csvText);
        if (csvData == null || csvData.Count == 0) return;

        // 각 임포터에서 자유롭게 처리하도록 데이터 전달
        ProcessImport(csvAssetPath, csvData);
    }

    /// <summary>
    /// 하위 클래스에서 이 메서드를 오버라이드하여 임포트 로직을 구현합니다.
    /// </summary>
    protected abstract void ProcessImport(string csvAssetPath, List<Dictionary<string, string>> csvData);

    /// <summary>
    /// 모든 CSV 파일의 임포트가 완료된 후 호출됩니다.
    /// </summary>
    protected virtual void OnPostImportAll() { }

    /// <summary>
    /// 지정된 이름과 경로(이동 가능)의 SO를 찾거나 생성하는 헬퍼 메서드입니다.
    /// </summary>
    protected T GetOrCreateSO(string assetName, string subFolder = "")
    {
        string folderPath = string.IsNullOrEmpty(subFolder) ? SoDirectory : $"{SoDirectory}/{subFolder}";
        string fullSoPath = $"{folderPath}/{assetName}.asset";

        // 1. 프로젝트 전체에서 이름으로 먼저 찾기 (이동된 경우 대응)
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && asset.name == assetName) return asset;
        }

        // 2. 없으면 생성
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            CreateFolderRecursively(folderPath);
        }

        T newAsset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(newAsset, fullSoPath);
        return newAsset;
    }

    private void CreateFolderRecursively(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
