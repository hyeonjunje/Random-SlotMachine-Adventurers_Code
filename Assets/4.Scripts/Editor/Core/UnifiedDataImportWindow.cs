using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UnifiedDataImportWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private Vector2 _previewScrollPos;
    private string _previewText;
    private string _previewPath;
    
    // 활성화된 임포터 리스트
    private List<DataImporterBase> _importers;

    // 검사 결과 캐시: 임포터별로 파일 경로 → 결과 매핑
    private Dictionary<DataImporterBase, List<CsvValidationResult>> _validationResults
        = new Dictionary<DataImporterBase, List<CsvValidationResult>>();

    [MenuItem("Tools/통합 데이터 임포터 (Data Import)")]
    public static void ShowWindow()
    {
        var window = GetWindow<UnifiedDataImportWindow>("Data Import");
        window.minSize = new Vector2(600, 600);
        window.Show();
    }

    private void OnEnable()
    {
        // 임포터 인스턴스화. 새로운 데이터 타입을 추가하려면 해당 데이터의 Importer 클래스를 작성하면 됩니다.
        _importers = new List<DataImporterBase>();
        
        // 리플렉션을 사용하여 DataImporterBase를 상속받은 모든 클래스를 자동으로 찾아 인스턴스화합니다.
        var importerTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(DataImporterBase)));

        foreach (var type in importerTypes)
        {
            try
            {
                var instance = Activator.CreateInstance(type) as DataImporterBase;
                if (instance != null)
                {
                    _importers.Add(instance);
                    DataImporterBase.Register(instance); // 교차 검사용 레지스트리 등록
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataImport] 임포터 {type.Name}의 인스턴스를 생성하지 못했습니다: {ex.Message}");
            }
        }

        // 툴이 열릴 때 모든 CSV 파일을 자동으로 검사합니다.
        RunAllValidations();
    }

    /// <summary>
    /// 모든 임포터의 CSV 파일을 검사하고 결과를 _validationResults에 캐싱합니다.
    /// </summary>
    private void RunAllValidations()
    {
        _validationResults.Clear();
        foreach (var importer in _importers)
        {
            importer.ClearCsvIdCache(); // 검사 시작 전 캐시 초기화
        }

        foreach (var importer in _importers)
        {
            try
            {
                _validationResults[importer] = importer.ValidateAll();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataImport] {importer.ImporterName} 검사 중 오류 발생: {ex.Message}");
                _validationResults[importer] = new List<CsvValidationResult>();
            }
        }
        Repaint();
    }

    /// <summary>
    /// 하나라도 검사 실패한 파일이 있으면 true를 반환합니다.
    /// </summary>
    private bool HasAnyValidationError()
    {
        foreach (var kvp in _validationResults)
        {
            foreach (var result in kvp.Value)
            {
                if (!result.IsValid) return true;
            }
        }
        return false;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("CSV → SO 데이터 임포터", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("엑셀에서 CSV로 내보낸 데이터를 ScriptableObject로 변환합니다.\nCSV 파일은 지정된 CsvDirectory 경로에 위치해야 합니다.", MessageType.Info);
        
        EditorGUILayout.Space();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // CSV 상태 헤더 + 재검사 버튼
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CSV 파일 상태", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("↺ 재검사", GUILayout.Width(70), GUILayout.Height(18)))
        {
            RunAllValidations();
        }
        EditorGUILayout.EndHorizontal();
        
        // 각 임포터 그룹 그리기
        foreach (var importer in _importers)
        {
            _validationResults.TryGetValue(importer, out var results);
            DrawImporterGroup(importer, results);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("임포트", EditorStyles.boldLabel);
        
        // 버튼 영역
        EditorGUILayout.BeginVertical("box");
        
        bool hasError = HasAnyValidationError();

        if (hasError)
        {
            EditorGUILayout.HelpBox("⚠ 검사에 실패한 CSV 파일이 있습니다. 문제를 먼저 수정한 뒤 임포트하세요.", MessageType.Warning);
        }

        GUI.backgroundColor = hasError ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.4f, 0.7f, 0.4f);
        if (GUILayout.Button("전체 임포트", GUILayout.Height(30)))
        {
            if (hasError)
            {
                EditorUtility.DisplayDialog(
                    "임포트 불가",
                    "검사에 실패한 CSV 파일이 있습니다.\n빨간색 파일의 문제를 먼저 수정한 뒤 임포트해 주세요.",
                    "확인");
            }
            else
            {
                foreach (var importer in _importers)
                {
                    importer.ImportAll();
                }
                EditorUtility.DisplayDialog("완료", "모든 CSV 데이터를 임포트했습니다.", "확인");
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.BeginHorizontal();
        foreach (var importer in _importers)
        {
            // 해당 임포터에 오류가 있는지 확인
            bool importerHasError = _validationResults.TryGetValue(importer, out var res)
                && res.Any(r => !r.IsValid);

            GUI.backgroundColor = importerHasError ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            if (GUILayout.Button($"{importer.ImporterName} 임포트", GUILayout.Height(25)))
            {
                if (importerHasError)
                {
                    EditorUtility.DisplayDialog(
                        "임포트 불가",
                        $"{importer.ImporterName}: 검사에 실패한 파일이 있습니다.\n문제를 먼저 수정해 주세요.",
                        "확인");
                }
                else
                {
                    importer.ImportAll();
                    EditorUtility.DisplayDialog("완료", $"{importer.ImporterName} CSV 데이터를 임포트했습니다.", "확인");
                }
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 미리보기 섹션
        EditorGUILayout.LabelField("▼ CSV 미리보기", EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(_previewPath))
        {
            EditorGUILayout.LabelField(_previewPath, EditorStyles.miniLabel);
            
            _previewScrollPos = EditorGUILayout.BeginScrollView(_previewScrollPos, "box", GUILayout.Height(150));
            GUIStyle textStyle = new GUIStyle(EditorStyles.textArea);
            textStyle.wordWrap = false;
            EditorGUILayout.TextArea(_previewText, textStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("미리보기를 확인할 CSV를 선택하세요.", MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawImporterGroup(DataImporterBase importer, List<CsvValidationResult> results)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"▼ {importer.ImporterName} CSV", EditorStyles.boldLabel);

        var files = importer.GetCsvFiles();
        if (files.Count == 0)
        {
            EditorGUILayout.LabelField("   파일이 없습니다.", EditorStyles.miniLabel);
        }
        else
        {
            foreach (var file in files)
            {
                // 이 파일에 대한 검사 결과 찾기
                CsvValidationResult fileResult = results?.FirstOrDefault(r => r.FilePath == file);
                bool isValid = fileResult == null || fileResult.IsValid;

                EditorGUILayout.BeginHorizontal();
                
                // 상태 아이콘 (초록: 통과, 빨간: 실패)
                GUI.color = isValid ? Color.green : Color.red;
                GUILayout.Label("●", GUILayout.Width(20));
                GUI.color = Color.white;
                
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                GUILayout.Label(fileName, GUILayout.Width(150));
                
                GUILayout.Label(file, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button("미리보기", GUILayout.Width(80)))
                {
                    _previewPath = file;
                    _previewText = importer.PreviewCsv(System.IO.Path.GetFullPath(file));
                    _previewScrollPos = Vector2.zero;
                }
                
                EditorGUILayout.EndHorizontal();

                // 검사 실패 시 오류 목록 표시
                if (!isValid && fileResult != null && fileResult.Errors.Count > 0)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    GUIStyle errorLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                    errorLabelStyle.normal.textColor = new Color(0.85f, 0.25f, 0.25f);
                    errorLabelStyle.wordWrap = true;

                    foreach (var error in fileResult.Errors)
                    {
                        string errorText;
                        if (error.Row <= 0)
                        {
                            // 파일 수준 오류
                            errorText = $"  ✗ {error.Message}";
                        }
                        else if (string.IsNullOrEmpty(error.Column))
                        {
                            errorText = $"  ✗ 행 {error.Row}: {error.Message}";
                        }
                        else
                        {
                            errorText = $"  ✗ 행 {error.Row} [{error.Column}]: {error.Message}";
                        }
                        EditorGUILayout.LabelField(errorText, errorLabelStyle);
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
}
