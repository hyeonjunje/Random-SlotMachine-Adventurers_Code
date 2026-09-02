using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class FontReplacerTool : EditorWindow
{
    // 경로 상수 (필요시 수정 용이하도록 분리)
    private const string FONT_SEARCH_PATH = "Assets/99.ETC/Font";
    private const string FONT_SEARCH_PATH2 = "Assets/14.Outsourcing/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Fonts";

    // UI 변수
    private DefaultAsset _targetFolder;
    private int _sourceFontIndex = 0;
    private int _targetFontIndex = 0;

    // 데이터 변수
    private List<TMP_FontAsset> _fontAssets = new List<TMP_FontAsset>();
    private string[] _fontNames;

    [MenuItem("Tools/Font Replacer")]
    public static void ShowWindow()
    {
        var window = GetWindow<FontReplacerTool>("Font Replacer");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshFontList();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("TMP Font Batch Replacer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 1. 타겟 폴더 선택
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("1. Select Target Folder", EditorStyles.miniLabel);
        _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Target Folder",
            _targetFolder,
            typeof(DefaultAsset),
            false
        );
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // 2. 폰트 선택 (Dropdown)
        if (_fontAssets == null || _fontAssets.Count == 0)
        {
            EditorGUILayout.HelpBox($"No fonts found in {FONT_SEARCH_PATH}. Please check the path.", MessageType.Warning);
            if (GUILayout.Button("Refresh Fonts"))
            {
                RefreshFontList();
            }
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("2. Select Fonts", EditorStyles.miniLabel);

            _sourceFontIndex = EditorGUILayout.Popup("Find (Old Font)", _sourceFontIndex, _fontNames);
            _targetFontIndex = EditorGUILayout.Popup("Replace With (New Font)", _targetFontIndex, _fontNames);

            // 동일 폰트 선택 시 경고
            if (_sourceFontIndex == _targetFontIndex)
            {
                EditorGUILayout.HelpBox("Source and Target fonts are the same.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            // 3. 실행 버튼
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Replace All Fonts", GUILayout.Height(40)))
            {
                if (_targetFolder == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select a target folder first.", "OK");
                    return;
                }

                if (EditorUtility.DisplayDialog("Confirm",
                    $"Are you sure you want to replace font from\n'{_fontNames[_sourceFontIndex]}'\nto\n'{_fontNames[_targetFontIndex]}'\nin folder '{_targetFolder.name}'?",
                    "Yes, Replace", "Cancel"))
                {
                    ReplaceFontsInFolder();
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }

    /// <summary>
    /// 지정된 경로에서 TMP_FontAsset들을 찾아 리스트를 갱신합니다.
    /// </summary>
    private void RefreshFontList()
    {
        _fontAssets.Clear();

        // 해당 경로 내의 모든 TMP_FontAsset GUID 검색
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FONT_SEARCH_PATH, FONT_SEARCH_PATH2 });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                _fontAssets.Add(font);
            }
        }

        // 드롭다운 표시용 이름 배열 생성
        _fontNames = new string[_fontAssets.Count];
        for (int i = 0; i < _fontAssets.Count; i++)
        {
            _fontNames[i] = _fontAssets[i].name;
        }
    }

    /// <summary>
    /// 실제 교체 로직을 수행합니다.
    /// </summary>
    private void ReplaceFontsInFolder()
    {
        string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
        // 시스템 경로로 변환하여 파일 검색 (재귀적)
        string[] fileEntries = Directory.GetFiles(folderPath, "*.prefab", SearchOption.AllDirectories);

        TMP_FontAsset sourceFont = _fontAssets[_sourceFontIndex];
        TMP_FontAsset targetFont = _fontAssets[_targetFontIndex];

        int changedCount = 0;
        int fileCount = 0;

        try
        {
            foreach (string filePath in fileEntries)
            {
                // 진행바 표시
                float progress = (float)fileCount / fileEntries.Length;
                if (EditorUtility.DisplayCancelableProgressBar("Replacing Fonts", $"Processing: {Path.GetFileName(filePath)}", progress))
                {
                    break;
                }
                fileCount++;

                // 시스템 경로(\)를 유니티 에셋 경로(/)로 변환
                string assetPath = filePath.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                // 위 코드는 Directory.GetFiles가 전체 경로를 반환할 경우를 대비한 보정입니다. 
                // 에디터 상 상대 경로 보정을 위해 아래 로직을 사용합니다.
                int assetIndex = assetPath.IndexOf("Assets/");
                if (assetIndex >= 0) assetPath = assetPath.Substring(assetIndex);

                // 프리팹 컨텐츠 로드 (인스턴스화 하지 않고 데이터만 로드)
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

                // 해당 프리팹 내의 모든 TMP 컴포넌트 검색
                TMP_Text[] texts = prefabContents.GetComponentsInChildren<TMP_Text>(true);
                bool isModified = false;

                foreach (TMP_Text txt in texts)
                {
                    // 조건: 현재 폰트가 '변경 대상' 폰트와 같을 경우에만 교체
                    if (txt.font == sourceFont)
                    {
                        txt.font = targetFont;
                        isModified = true;
                        changedCount++;
                    }
                }

                // 수정된 사항이 있다면 저장
                if (isModified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
                    Debug.Log($"[FontReplacer] Modified: {assetPath}");
                }

                // 메모리 해제
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FontReplacer] Error: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets(); // 변경사항 최종 저장
            Debug.Log($"<b>[FontReplacer] Complete! Changed {changedCount} text components.</b>");
        }
    }
}