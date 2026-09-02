using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TMPro;
using UnityEditor.SceneManagement;
using System.Linq;

public class LocalizationExtractorWindow : EditorWindow
{
    private class ExtractedString
    {
        public bool isSelected = true;
        public string originalText;
        public string filePath;
        public int lineNumber; // C#용
        public string componentPath; // Prefab용
        public bool isCSharp;
        public bool isInterpolated;
        public string generatedKey;
    }

    private List<ExtractedString> _extractedItems = new List<ExtractedString>();
    private Vector2 _scrollPos;
    
    private bool _showCSharp = true;
    private bool _showPrefabs = true;

    [MenuItem("Tools/Localization Extractor")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationExtractorWindow>("번역 데이터 추출기");
    }

    private void OnGUI()
    {
        GUILayout.Label("로컬라이제이션(번역) 자동화 툴", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("1. 스캔 버튼을 눌러 한국어를 찾습니다.\n2. CSV로 내보내기를 눌러 번역 시트를 생성/업데이트 합니다.\n3. 자동 치환을 눌러 실제 코드와 프리팹을 수정합니다.", MessageType.Info);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. 스캔하기 (C# 및 프리팹)", GUILayout.Height(30)))
        {
            ScanAll();
        }
        if (GUILayout.Button("2. CSV로 내보내기", GUILayout.Height(30)))
        {
            ExportToCSV();
        }
        EditorGUILayout.EndHorizontal();

        GUI.color = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("3. 체크된 항목 자동 치환 적용 (주의!)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("경고", "체크된 모든 C# 코드와 프리팹이 수정됩니다. (Git 백업 권장)\n진행하시겠습니까?", "예", "아니오"))
            {
                ApplyReplacements();
            }
        }
        GUI.color = Color.white;

        GUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        _showCSharp = EditorGUILayout.ToggleLeft("C# 결과 보기", _showCSharp, GUILayout.Width(100));
        _showPrefabs = EditorGUILayout.ToggleLeft("프리팹/씬 결과 보기", _showPrefabs, GUILayout.Width(130));
        
        if (GUILayout.Button("전체 선택", GUILayout.Width(80))) _extractedItems.ForEach(x => x.isSelected = true);
        if (GUILayout.Button("전체 해제", GUILayout.Width(80))) _extractedItems.ForEach(x => x.isSelected = false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        
        foreach (var item in _extractedItems)
        {
            if (item.isCSharp && !_showCSharp) continue;
            if (!item.isCSharp && !_showPrefabs) continue;

            EditorGUILayout.BeginHorizontal("box");
            
            if (item.isInterpolated) GUI.color = new Color(1f, 0.6f, 0.6f);
            
            item.isSelected = EditorGUILayout.Toggle(item.isSelected, GUILayout.Width(20));
            
            GUILayout.Label(item.isCSharp ? (item.isInterpolated ? "[C#/$]" : "[C#]") : "[Prefab]", GUILayout.Width(60));
            
            string displayLabel = item.originalText;
            if (item.isInterpolated) displayLabel += " (보간법 - 수동 치환 대상)";
            EditorGUILayout.SelectableLabel(displayLabel, GUILayout.Height(20), GUILayout.Width(200));
            
            item.generatedKey = EditorGUILayout.TextField(item.generatedKey, GUILayout.Width(150));
            
            string pathDisplay = item.isCSharp ? $"{Path.GetFileName(item.filePath)} : {item.lineNumber}" : item.filePath;
            GUILayout.Label(pathDisplay);
            
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;
        }

        EditorGUILayout.EndScrollView();
    }

    private Dictionary<string, string> _textToKeyMap = new Dictionary<string, string>();

    private void ScanAll()
    {
        _extractedItems.Clear();
        _textToKeyMap.Clear();
        LoadExistingKeysFromCSV();
        
        ScanCSharpFiles();
        ScanPrefabs();
        Debug.Log($"[Localization] 스캔 완료! 총 {_extractedItems.Count}개의 한국어 텍스트를 찾았습니다.");
    }

    private void LoadExistingKeysFromCSV()
    {
        string exportPath = "Assets/Resources/LocalizationData.csv";
        if (!File.Exists(exportPath)) return;

        string csvContent = File.ReadAllText(exportPath);
        string pattern = @"(((?<x>(?=[\t\r\n]+))|""""(?<x>([^""""]|"""""""")+)""""|(?<x>[^\t\r\n]+))\t?)";
        string[] rowLines = csvContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rowLines.Length; i++)
        {
            List<string> row = new List<string>();
            MatchCollection matches = Regex.Matches(rowLines[i], pattern);
            foreach (Match match in matches)
            {
                string val = match.Groups["x"].Value;
                val = val.Replace("\"\"", "\"");
                row.Add(val);
            }

            if (row.Count >= 3)
            {
                string key = row[0].Trim();
                string ko = row[1].Trim();
                
                string unescapedKo = ko.Replace("\\n", "\n");
                
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(unescapedKo))
                {
                    if (!_textToKeyMap.ContainsKey(unescapedKo))
                    {
                        _textToKeyMap[unescapedKo] = key;
                    }
                }
            }
        }
    }

    private void ScanCSharpFiles()
    {
        string[] scriptPaths = Directory.GetFiles("Assets/4.Scripts", "*.cs", SearchOption.AllDirectories);
        // 쌍따옴표로 둘러싸여 있고, 한글이 1자 이상 포함된 문자열 검색
        Regex koreanRegex = new Regex("\"[^\"]*[가-힣]+[^\"]*\"");

        int keyIndex = 1;

        foreach (string path in scriptPaths)
        {
            // 추출기나 매니저 자체는 건너뛰기
            if (path.Contains("LocalizationExtractorWindow") || path.Contains("LocalizationManager"))
                continue;

            string fileContent = File.ReadAllText(path);
            // 에디터 스크립트는 통째로 건너뛰기 (UnityEditor 네임스페이스 사용 시)
            if (fileContent.Contains("using UnityEditor;"))
                continue;

            string[] lines = File.ReadAllLines(path);
            bool inEditorBlock = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimLine = line.TrimStart();

                // 전처리기 매크로 확인 (에디터 전용 블록 건너뛰기)
                if (trimLine.StartsWith("#if") && trimLine.Contains("UNITY_EDITOR"))
                {
                    inEditorBlock = true;
                    continue;
                }
                if (inEditorBlock && (trimLine.StartsWith("#else") || trimLine.StartsWith("#elif")))
                {
                    inEditorBlock = false;
                    continue;
                }
                if (inEditorBlock && trimLine.StartsWith("#endif"))
                {
                    inEditorBlock = false;
                    continue;
                }
                
                // 에디터 블록 내부면 스캔 건너뛰기
                if (inEditorBlock) continue;

                // 안전장치: 주석, Debug.Log, 어트리뷰트가 있는 줄은 건너뜀
                if (trimLine.StartsWith("//")) continue;
                if (trimLine.StartsWith("[")) continue;
                if (line.Contains("Debug.Log")) continue;

                MatchCollection matches = koreanRegex.Matches(line);
                foreach (Match match in matches)
                {
                    string textValue = match.Value.Trim('"'); // 쌍따옴표 제거
                    bool interpolated = match.Index > 0 && line[match.Index - 1] == '$';

                    string genKey;
                    if (_textToKeyMap.TryGetValue(textValue, out string existingKey))
                    {
                        genKey = existingKey;
                    }
                    else
                    {
                        genKey = $"CS_{Path.GetFileNameWithoutExtension(path).ToUpper()}_{keyIndex++:D3}";
                        _textToKeyMap[textValue] = genKey;
                    }

                    _extractedItems.Add(new ExtractedString
                    {
                        isCSharp = true,
                        originalText = textValue,
                        filePath = path,
                        lineNumber = i,
                        isInterpolated = interpolated,
                        isSelected = !interpolated,
                        generatedKey = genKey
                    });
                }
            }
        }
    }

    private void ScanPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/3.Prefabs" });
        int keyIndex = 1;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (string.IsNullOrWhiteSpace(t.text)) continue;
                
                // 한글이 포함되어 있는지 확인
                if (Regex.IsMatch(t.text, "[가-힣]"))
                {
                    // 이미 LocalizedText가 붙어있으면 건너뛰기
                    if (t.GetComponent<LocalizedText>() != null) continue;

                    string genKey;
                    if (_textToKeyMap.TryGetValue(t.text, out string existingKey))
                    {
                        genKey = existingKey;
                    }
                    else
                    {
                        genKey = $"UI_{prefab.name.ToUpper()}_{keyIndex++:D3}";
                        _textToKeyMap[t.text] = genKey;
                    }

                    _extractedItems.Add(new ExtractedString
                    {
                        isCSharp = false,
                        originalText = t.text,
                        filePath = path,
                        componentPath = GetGameObjectPath(t.gameObject),
                        generatedKey = genKey
                    });
                }
            }
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }

    private void ExportToCSV()
    {
        string exportPath = "Assets/Resources/LocalizationData.csv";
        Directory.CreateDirectory("Assets/Resources");

        Dictionary<string, string[]> existingData = new Dictionary<string, string[]>();

        if (File.Exists(exportPath))
        {
            string csvContent = File.ReadAllText(exportPath);
            string pattern = @"(((?<x>(?=[\t\r\n]+))|""""(?<x>([^""""]|"""""""")+)""""|(?<x>[^\t\r\n]+))\t?)";
            string[] rowLines = csvContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < rowLines.Length; i++)
            {
                List<string> row = new List<string>();
                MatchCollection matches = Regex.Matches(rowLines[i], pattern);
                foreach (Match match in matches)
                {
                    string val = match.Groups["x"].Value;
                    val = val.Replace("\"\"", "\"");
                    row.Add(val);
                }

                if (row.Count >= 3)
                {
                    existingData[row[0]] = new string[] { row[1], row[2] };
                }
            }
        }

        using (StreamWriter writer = new StreamWriter(exportPath, false, new System.Text.UTF8Encoding(true)))
        {
            writer.WriteLine("Key\tKO\tEN");

            foreach (var kvp in existingData)
            {
                writer.WriteLine($"{kvp.Key}\t\"{kvp.Value[0]}\"\t\"{kvp.Value[1]}\"");
            }

            foreach (var item in _extractedItems)
            {
                if (!item.isSelected) continue;
                if (existingData.ContainsKey(item.generatedKey)) continue;

                string safeText = item.originalText.Replace("\n", "\\n").Replace("\r", "");
                writer.WriteLine($"{item.generatedKey}\t\"{safeText}\"\t\"\"");
                
                existingData[item.generatedKey] = new string[] { safeText, "" };
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Localization] {exportPath} 파일로 성공적으로 내보냈습니다.");
    }

    private void ApplyReplacements()
    {
        int csCount = 0;
        int prefabCount = 0;

        // 1. C# 파일 교체
        var csItems = _extractedItems.Where(x => x.isCSharp && x.isSelected).GroupBy(x => x.filePath);
        foreach (var fileGroup in csItems)
        {
            string path = fileGroup.Key;
            string content = File.ReadAllText(path);
            bool isModified = false;

            // 라인별로 처리하기 위해 분리
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            foreach (var item in fileGroup)
            {
                if (item.lineNumber >= lines.Length) continue;

                string line = lines[item.lineNumber];
                string newLine = line;

                if (item.isInterpolated)
                {
                    // 보간법 치환 로직: $"{var} 텍스트" -> string.Format(LocalizationManager.Instance.Get("KEY"), var)
                    // 단순한 정규식으로 변수 부분 추출 ({...} 찾기)
                    var varMatches = Regex.Matches(item.originalText, @"\{([^\}]+)\}");
                    List<string> vars = new List<string>();
                    foreach (Match m in varMatches) vars.Add(m.Groups[1].Value);

                    string originalFull = $"$\"{item.originalText}\"";
                    string replacement;
                    
                    if (vars.Count > 0)
                        replacement = $"string.Format(LocalizationManager.Instance.Get(\"{item.generatedKey}\"), {string.Join(", ", vars)})";
                    else
                        replacement = $"LocalizationManager.Instance.Get(\"{item.generatedKey}\")";

                    if (line.Contains(originalFull))
                    {
                        newLine = line.Replace(originalFull, replacement);
                    }
                }
                else
                {
                    // 일반 문자열 치환: "텍스트" -> LocalizationManager.Instance.Get("KEY")
                    string originalFull = $"\"{item.originalText}\"";
                    string replacement = $"LocalizationManager.Instance.Get(\"{item.generatedKey}\")";
                    
                    if (line.Contains(originalFull))
                    {
                        newLine = line.Replace(originalFull, replacement);
                    }
                }

                if (line != newLine)
                {
                    lines[item.lineNumber] = newLine;
                    isModified = true;
                    csCount++;
                }
            }

            if (isModified)
            {
                File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(true));
            }
        }

        // 2. 프리팹 교체 (기존 로직 유지하되 안전성 강화)
        var prefabItems = _extractedItems.Where(x => !x.isCSharp && x.isSelected).GroupBy(x => x.filePath);
        foreach (var prefabGroup in prefabItems)
        {
            string path = prefabGroup.Key;
            try
            {
                using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    GameObject root = editScope.prefabContentsRoot;
                    TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

                    foreach (var item in prefabGroup)
                    {
                        foreach (var t in texts)
                        {
                            if (GetGameObjectPath(t.gameObject) == item.componentPath && t.text == item.originalText)
                            {
                                if (t.GetComponent<LocalizedText>() == null)
                                {
                                    LocalizedText locText = t.gameObject.AddComponent<LocalizedText>();
                                    SerializedObject so = new SerializedObject(locText);
                                    so.FindProperty("_localizationKey").stringValue = item.generatedKey;
                                    so.ApplyModifiedProperties();
                                    prefabCount++;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Localization] 프리팹 치환 실패 ({path}): {ex.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Localization] 치환 완료! C# 파일 {csCount}곳, 프리팹 {prefabCount}곳이 수정되었습니다.");
        
        // 치환 완료 항목 제거
        _extractedItems.RemoveAll(x => x.isSelected);
    }
}
