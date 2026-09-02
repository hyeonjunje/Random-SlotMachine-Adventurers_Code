using System;
using UnityEditor;
using UnityEngine;

public class ColorPaletteEditor : EditorWindow
{
    private SO_ColorPaletteData palette;
    private Vector2 _scrollPos;

    [MenuItem("Tools/Color Palette Editor")]
    public static void ShowWindow()
    {
        GetWindow<ColorPaletteEditor>("Color Palette Editor");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        palette = (SO_ColorPaletteData)EditorGUILayout.ObjectField("Palette Asset", palette, typeof(SO_ColorPaletteData), false);

        if (palette == null)
        {
            if (GUILayout.Button("Create New Palette"))
            {
                palette = CreateInstance<SO_ColorPaletteData>();
                AssetDatabase.CreateAsset(palette, "Assets/NewColorPalette.asset");
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = palette;
            }
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Color Entries", EditorStyles.boldLabel);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        foreach (EColorKey key in Enum.GetValues(typeof(EColorKey)))
        {
            var entry = palette.colors.Find(e => e.key == key);
            if (entry == null)
            {
                entry = new SO_ColorPaletteData.ColorEntry { key = key, stringKey = key.ToString(), color = Color.white };
                palette.colors.Add(entry);
            }

            entry.color = EditorGUILayout.ColorField(key.ToString(), entry.color);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(palette);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        foreach (var entry in palette.colors)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.key.ToString(), GUILayout.Width(100));
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 20), entry.color);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}
