#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyAIGraphEditorWindow : EditorWindow
{
    private SO_EnemyAI _targetAI;
    private EnemyAIGraphView _graphView;
    private Label _titleLabel;

    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is SO_EnemyAI enemyAI)
        {
            OpenWindow(enemyAI);
            return true;
        }
        return false;
    }

    public static void OpenWindow(SO_EnemyAI enemyAI)
    {
        var window = GetWindow<EnemyAIGraphEditorWindow>("Enemy AI Editor");
        window.minSize = new Vector2(800, 500);
        window.LoadAsset(enemyAI);
    }

    private void OnEnable()
    {
        rootVisualElement.styleSheets.Add(LoadStyleSheet());
        BuildUI();
    }

    private void OnDisable()
    {
        if (_graphView != null)
        {
            _graphView.SaveAllNodePositions();
            rootVisualElement.Remove(_graphView);
        }
    }

    private StyleSheet LoadStyleSheet()
    {
        string[] guids = AssetDatabase.FindAssets("EnemyAIGraphStyle t:StyleSheet");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }
        return null;
    }

    private void BuildUI()
    {
        // Toolbar
        var toolbar = new VisualElement();
        toolbar.AddToClassList("toolbar");

        _titleLabel = new Label("Enemy AI Editor - 에셋을 선택하세요");
        _titleLabel.AddToClassList("toolbar-title");
        toolbar.Add(_titleLabel);

        var saveButton = new Button(() => Save()) { text = "💾 저장" };
        saveButton.AddToClassList("toolbar-button");
        toolbar.Add(saveButton);

        var addButton = new Button(() => AddNewGroup()) { text = "➕ 그룹 추가" };
        addButton.AddToClassList("toolbar-button");
        toolbar.Add(addButton);

        rootVisualElement.Add(toolbar);
    }

    public void LoadAsset(SO_EnemyAI enemyAI)
    {
        _targetAI = enemyAI;
        _titleLabel.text = $"Enemy AI Editor - {enemyAI.name}";

        if (_graphView != null)
        {
            _graphView.SaveAllNodePositions();
            rootVisualElement.Remove(_graphView);
        }

        _graphView = new EnemyAIGraphView(enemyAI);
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);

        // GraphView가 toolbar 아래에 오도록 순서 조정
        _graphView.SendToBack();
        _graphView.style.marginTop = 30;
    }

    private void AddNewGroup()
    {
        if (_targetAI == null || _graphView == null) return;

        Undo.RecordObject(_targetAI, "Add EnemyActGroup");

        int newId = 1;
        foreach (var g in _targetAI.EnemyActGroup)
        {
            if (g.Id >= newId) newId = g.Id + 1;
        }

        var newGroup = new EnemyActGroup();
        // Id는 private set이라 리플렉션으로 설정
        var idField = typeof(EnemyActGroup).GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (idField != null) idField.SetValue(newGroup, newId);

        _targetAI.EnemyActGroup.Add(newGroup);
        EditorUtility.SetDirty(_targetAI);
        _graphView.Reload();
    }

    private void Save()
    {
        if (_targetAI == null || _graphView == null) return;
        _graphView.SaveAllNodePositions();
        EditorUtility.SetDirty(_targetAI);
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyAI Editor] '{_targetAI.name}' 저장 완료!");
    }
}
#endif
