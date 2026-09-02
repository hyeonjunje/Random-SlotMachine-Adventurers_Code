#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SO_EnemyAI))]
public class SO_EnemyAI_Inspector : Editor
{
    public override void OnInspectorGUI()
    {
        var enemyAI = (SO_EnemyAI)target;

        // 노드 에디터 열기 버튼
        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(0.3f, 0.6f, 1.0f);
        if (GUILayout.Button("🔧 노드 에디터 열기", GUILayout.Height(32)))
        {
            EnemyAIGraphEditorWindow.OpenWindow(enemyAI);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(4);

        // 요약 정보
        EditorGUILayout.HelpBox(
            $"그룹 수: {enemyAI.EnemyActGroup.Count}\n" +
            $"더블클릭으로도 노드 에디터를 열 수 있습니다.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // 기본 인스펙터
        DrawDefaultInspector();
    }
}
#endif
