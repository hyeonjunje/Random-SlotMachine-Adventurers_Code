#if UNITY_EDITOR
using UnityEditor;

public static class ArtifactDataImportCommand
{
    public static void ImportArtifacts()
    {
        var importer = new ArtifactDataImporter();
        importer.ImportAll();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
