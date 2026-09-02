using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[CustomEditor(typeof(SO_AudioCatalog_BGM))]
public class SO_AudioCatalog_BGM_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SO_AudioCatalog_BGM catalog = (SO_AudioCatalog_BGM)target;

        if (GUILayout.Button("Populate Missing Enums"))
        {
            Undo.RecordObject(catalog, "Populate Missing BGM Enums");
            bool changed = false;

            foreach (EBgmId id in Enum.GetValues(typeof(EBgmId)))
            {
                if (!catalog.Bgms.Any(entry => entry.id == id))
                {
                    catalog.Bgms.Add(new SO_AudioData.BgmEntry { id = id, defaultVolume = 1f });
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(catalog);
            }
        }
    }
}

[CustomEditor(typeof(SO_AudioCatalog_SFX))]
public class SO_AudioCatalog_SFX_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SO_AudioCatalog_SFX catalog = (SO_AudioCatalog_SFX)target;

        if (GUILayout.Button("Populate Missing Enums"))
        {
            Undo.RecordObject(catalog, "Populate Missing SFX Enums");
            bool changed = false;

            foreach (ESfxId id in Enum.GetValues(typeof(ESfxId)))
            {
                if (!catalog.Sfxs.Any(entry => entry.id == id))
                {
                    catalog.Sfxs.Add(new SO_AudioData.SfxEntry { id = id, defaultVolume = 1f });
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(catalog);
            }
        }
    }
}
