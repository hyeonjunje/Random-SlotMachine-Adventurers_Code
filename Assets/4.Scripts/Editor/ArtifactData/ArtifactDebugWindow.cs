using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ArtifactDebugWindow : EditorWindow
{
    private const float ActionButtonWidth = 72f;
    private const float PingButtonWidth = 56f;

    private string _searchText = string.Empty;
    private bool _showOwnedOnly;
    private bool _showUnownedOnly;
    private bool _showDescriptions = true;
    private Vector2 _ownedScroll;
    private Vector2 _catalogScroll;

    [MenuItem("Tools/유물 디버거 (Artifact Debug)")]
    public static void ShowWindow()
    {
        ArtifactDebugWindow window = GetWindow<ArtifactDebugWindow>("Artifact Debug");
        window.minSize = new Vector2(760f, 540f);
        window.Show();
    }

    private void OnInspectorUpdate()
    {
        if (EditorApplication.isPlaying)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        DataManager dataManager = FindRuntimeObject<DataManager>();
        ArtifactSystem artifactSystem = FindRuntimeObject<ArtifactSystem>();
        CharacterSystem characterSystem = FindRuntimeObject<CharacterSystem>();

        DrawRuntimeStatus(dataManager, artifactSystem, characterSystem);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "플레이 모드에서 열면 현재 파티 기준으로 유물을 즉시 추가/제거할 수 있습니다.",
                MessageType.Info);
            return;
        }

        if (dataManager == null || artifactSystem == null)
        {
            EditorGUILayout.HelpBox(
                "현재 씬에서 DataManager 또는 ArtifactSystem을 찾지 못했습니다. 게임 진입 후 다시 열어주세요.",
                MessageType.Warning);
            return;
        }

        DrawToolbar(artifactSystem);
        EditorGUILayout.Space();
        DrawPartySummary(characterSystem);
        EditorGUILayout.Space();
        DrawOwnedArtifacts(artifactSystem);
        EditorGUILayout.Space();
        DrawCatalog(dataManager, artifactSystem, characterSystem);
    }

    private void DrawRuntimeStatus(DataManager dataManager, ArtifactSystem artifactSystem, CharacterSystem characterSystem)
    {
        EditorGUILayout.LabelField("유물 테스트", EditorStyles.boldLabel);

        string partyStatus = "없음";
        if (characterSystem != null && characterSystem.Players.Count > 0)
        {
            partyStatus = string.Join(", ", characterSystem.Players
                .Where(x => x?.Player?.PlayerData != null)
                .Select(x => $"{x.Player.PlayerData.PlayerJob}"));
        }

        EditorGUILayout.HelpBox(
            $"PlayMode: {(EditorApplication.isPlaying ? "ON" : "OFF")}\n" +
            $"DataManager: {(dataManager != null ? "OK" : "Missing")}\n" +
            $"ArtifactSystem: {(artifactSystem != null ? "OK" : "Missing")}\n" +
            $"Party: {partyStatus}",
            MessageType.None);
    }

    private void DrawToolbar(ArtifactSystem artifactSystem)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("검색", GUILayout.Width(32f));
        _searchText = EditorGUILayout.TextField(_searchText);
        if (GUILayout.Button("지우기", GUILayout.Width(60f)))
        {
            _searchText = string.Empty;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _showOwnedOnly = EditorGUILayout.ToggleLeft("보유 유물만", _showOwnedOnly, GUILayout.Width(100f));
        _showUnownedOnly = EditorGUILayout.ToggleLeft("미보유 유물만", _showUnownedOnly, GUILayout.Width(110f));
        _showDescriptions = EditorGUILayout.ToggleLeft("설명 표시", _showDescriptions, GUILayout.Width(90f));
        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(artifactSystem.OwnedArtifacts.Count == 0))
        {
            if (GUILayout.Button("전체 제거", GUILayout.Width(90f)))
            {
                RemoveAllArtifacts(artifactSystem);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (_showOwnedOnly && _showUnownedOnly)
        {
            EditorGUILayout.HelpBox("두 필터를 동시에 켜면 결과가 비어 있을 수 있습니다.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPartySummary(CharacterSystem characterSystem)
    {
        EditorGUILayout.LabelField("현재 파티", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (characterSystem == null || characterSystem.Players.Count == 0)
        {
            EditorGUILayout.HelpBox("현재 파티를 찾지 못했습니다. 직업 전용 유물은 올바른 소유자를 못 찾을 수 있습니다.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        foreach (PlayerView playerView in characterSystem.Players)
        {
            Player player = playerView?.Player;
            SO_PlayerData playerData = player?.PlayerData;
            if (playerData == null)
            {
                continue;
            }

            EditorGUILayout.LabelField(
                $"{playerData.SubjectKeyword} | Job: {playerData.PlayerJob} | Lv.{player.Level}",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawOwnedArtifacts(ArtifactSystem artifactSystem)
    {
        EditorGUILayout.LabelField($"보유 유물 ({artifactSystem.OwnedArtifacts.Count})", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (artifactSystem.OwnedArtifacts.Count == 0)
        {
            EditorGUILayout.LabelField("현재 보유 중인 유물이 없습니다.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        _ownedScroll = EditorGUILayout.BeginScrollView(_ownedScroll, GUILayout.Height(180f));

        foreach (Artifact artifact in artifactSystem.OwnedArtifacts
                     .OrderBy(x => x.Data.OwnerJob != EPlayerJob.None)
                     .ThenBy(x => x.Data.OwnerJob.ToString())
                     .ThenBy(x => x.Data.ID.ToString()))
        {
            DrawOwnedArtifactRow(artifactSystem, artifact);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawOwnedArtifactRow(ArtifactSystem artifactSystem, Artifact artifact)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(artifact.Data.ID.ToString(), EditorStyles.boldLabel);
        GUILayout.Space(8f);
        EditorGUILayout.LabelField(GetOwnerLabel(artifact), EditorStyles.miniLabel, GUILayout.Width(220f));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Ping", GUILayout.Width(PingButtonWidth)))
        {
            EditorGUIUtility.PingObject(artifact.Data);
            Selection.activeObject = artifact.Data;
        }

        if (GUILayout.Button("제거", GUILayout.Width(ActionButtonWidth)))
        {
            artifactSystem.RemoveArtifact(artifact);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();

        if (_showDescriptions && !string.IsNullOrWhiteSpace(artifact.Data.Description))
        {
            EditorGUILayout.LabelField(artifact.Data.Description, EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCatalog(DataManager dataManager, ArtifactSystem artifactSystem, CharacterSystem characterSystem)
    {
        List<SO_ArtifactData> artifacts = dataManager.AllArtifacts
            .Where(x => x != null)
            .OrderBy(x => artifactSystem.HasArtifact(x.ID) ? 0 : 1)
            .ThenBy(x => x.OwnerJob != EPlayerJob.None)
            .ThenBy(x => x.OwnerJob.ToString())
            .ThenBy(x => x.ID.ToString())
            .ToList();

        EditorGUILayout.LabelField($"전체 유물 목록 ({artifacts.Count})", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        _catalogScroll = EditorGUILayout.BeginScrollView(_catalogScroll);

        foreach (SO_ArtifactData artifactData in artifacts)
        {
            if (ShouldSkipArtifact(artifactData, artifactSystem))
            {
                continue;
            }

            DrawCatalogRow(artifactData, artifactSystem, characterSystem);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private bool ShouldSkipArtifact(SO_ArtifactData data, ArtifactSystem artifactSystem)
    {
        bool isOwned = artifactSystem.HasArtifact(data.ID);

        if (_showOwnedOnly && !isOwned)
        {
            return true;
        }

        if (_showUnownedOnly && isOwned)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return false;
        }

        string search = _searchText.Trim().ToLowerInvariant();
        string description = data.Description ?? string.Empty;
        return !data.ID.ToString().ToLowerInvariant().Contains(search)
            && !description.ToLowerInvariant().Contains(search)
            && !data.OwnerJob.ToString().ToLowerInvariant().Contains(search)
            && !data.Pools.ToString().ToLowerInvariant().Contains(search);
    }

    private void DrawCatalogRow(SO_ArtifactData artifactData, ArtifactSystem artifactSystem, CharacterSystem characterSystem)
    {
        Artifact ownedArtifact = artifactSystem.OwnedArtifacts.FirstOrDefault(x => x.Data.ID == artifactData.ID);
        bool isOwned = ownedArtifact != null;
        Player resolvedOwner = ResolveOwner(characterSystem, artifactData.OwnerJob);
        bool ownerRequired = artifactData.OwnerJob != EPlayerJob.None;
        bool ownerMissing = ownerRequired && resolvedOwner == null;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(artifactData.ID.ToString(), EditorStyles.boldLabel);
        GUILayout.Space(8f);
        EditorGUILayout.LabelField(GetArtifactMetaLabel(artifactData), EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Ping", GUILayout.Width(PingButtonWidth)))
        {
            EditorGUIUtility.PingObject(artifactData);
            Selection.activeObject = artifactData;
        }

        using (new EditorGUI.DisabledScope(ownerMissing))
        {
            if (!isOwned)
            {
                if (GUILayout.Button("추가", GUILayout.Width(ActionButtonWidth)))
                {
                    artifactSystem.AddArtifact(artifactData.ID, resolvedOwner);
                    GUIUtility.ExitGUI();
                }
            }
            else if (GUILayout.Button("제거", GUILayout.Width(ActionButtonWidth)))
            {
                artifactSystem.RemoveArtifact(ownedArtifact);
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (_showDescriptions && !string.IsNullOrWhiteSpace(artifactData.Description))
        {
            EditorGUILayout.LabelField(artifactData.Description, EditorStyles.wordWrappedMiniLabel);
        }

        if (ownerMissing)
        {
            EditorGUILayout.HelpBox(
                $"{artifactData.OwnerJob} 직업 캐릭터가 현재 파티에 없어서 바로 추가할 수 없습니다.",
                MessageType.Warning);
        }
        else if (ownerRequired)
        {
            EditorGUILayout.LabelField(
                $"예상 소유자: {GetPlayerLabel(resolvedOwner)}",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private static void RemoveAllArtifacts(ArtifactSystem artifactSystem)
    {
        List<Artifact> snapshot = artifactSystem.OwnedArtifacts.ToList();
        foreach (Artifact artifact in snapshot)
        {
            artifactSystem.RemoveArtifact(artifact);
        }
    }

    private static T FindRuntimeObject<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>();
    }

    private static Player ResolveOwner(CharacterSystem characterSystem, EPlayerJob ownerJob)
    {
        if (ownerJob == EPlayerJob.None || characterSystem == null)
        {
            return null;
        }

        foreach (PlayerView playerView in characterSystem.Players)
        {
            if (playerView?.Player?.PlayerData?.PlayerJob == ownerJob)
            {
                return playerView.Player;
            }
        }

        return null;
    }

    private static string GetArtifactMetaLabel(SO_ArtifactData artifactData)
    {
        string ownerLabel = artifactData.OwnerJob == EPlayerJob.None
            ? "공용"
            : artifactData.OwnerJob.ToString();

        return $"Owner: {ownerLabel} | Pools: {artifactData.Pools}";
    }

    private static string GetOwnerLabel(Artifact artifact)
    {
        if (artifact.OwnerPlayer == null)
        {
            return "소유자: 공용";
        }

        return $"소유자: {GetPlayerLabel(artifact.OwnerPlayer)}";
    }

    private static string GetPlayerLabel(Player player)
    {
        if (player?.PlayerData == null)
        {
            return "없음";
        }

        return $"{player.PlayerData.SubjectKeyword} ({player.PlayerData.PlayerJob})";
    }
}
