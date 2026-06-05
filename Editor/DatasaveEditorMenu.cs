using System.IO;
using UnityEditor;

namespace Dreamy.Datasave.Editor
{
    public static class DatasaveEditorMenu
    {
        private const string SaveMenuRoot = "Tools/Dreamy/Save/";

        [MenuItem(SaveMenuRoot + "Open Save Folder")]
        public static void OpenSaveFolder()
        {
            string path = DatasavePaths.GetSaveRootPath();
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem(SaveMenuRoot + "Clear Save Data")]
        public static void ClearSaveData()
        {
            string path = DatasavePaths.GetSaveRootPath();
            if (!Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("Dreamy Save", "No save folder exists.", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Save Data",
                $"Delete all files under:\n{path}",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            Directory.Delete(path, true);
            EditorUtility.DisplayDialog("Dreamy Save", "Save data cleared.", "OK");
        }
    }
}
