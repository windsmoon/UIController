using System;
using System.Collections.Generic;
using System.Text;
using Windsmoon.UIController;
using UnityEditor;
using UnityEngine;

namespace Windsmoon.UIController.Editor
{
    public class UIControllerLegacyMigrationWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        #region methods
        [MenuItem("UIController/UIController Legacy Migration")]
        private static void OpenWindow()
        {
            UIControllerLegacyMigrationWindow window = GetWindow<UIControllerLegacyMigrationWindow>("UIController Migration");
            window.minSize = new Vector2(440f, 300f);
            window.Show();
        }

        private void OnGUI()
        {
            List<string> folderPathList = GetSelectedFolderPaths();

            EditorGUILayout.LabelField("UIController Legacy Migration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select one or more folders in the Project window, then migrate all prefab UIControllerPanel components under them.", MessageType.Info);
            EditorGUILayout.Space(6f);

            DrawSelectedFolderList(folderPathList);
            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(folderPathList.Count == 0))
            {
                if (GUILayout.Button("Migrate Selected Folder Prefabs", GUILayout.Height(32f)))
                {
                    MigrateSelectedFolderPrefabs(folderPathList);
                }
            }
        }

        private void DrawSelectedFolderList(List<string> folderPathList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Selected Folders", EditorStyles.boldLabel);

            if (folderPathList.Count == 0)
            {
                EditorGUILayout.HelpBox("No Project folder selected.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinHeight(80f), GUILayout.MaxHeight(160f));
            for (int i = 0; i < folderPathList.Count; i++)
            {
                EditorGUILayout.LabelField(folderPathList[i], EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void MigrateSelectedFolderPrefabs(List<string> folderPathList)
        {
            if (folderPathList.Count == 0)
            {
                EditorUtility.DisplayDialog("UIController Migration", "Select one or more folders in the Project window first.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("UIController Migration", BuildConfirmMessage(folderPathList), "Migrate", "Cancel") == false)
            {
                return;
            }

            List<string> prefabPathList = GetPrefabPaths(folderPathList);
            int scannedPanelCount = 0;
            int migratedPanelCount = 0;
            int migratedPrefabCount = 0;
            int warningCount = 0;

            try
            {
                for (int prefabIndex = 0; prefabIndex < prefabPathList.Count; prefabIndex++)
                {
                    string prefabPath = prefabPathList[prefabIndex];
                    float progress = prefabPathList.Count == 0 ? 1f : (float)(prefabIndex + 1) / prefabPathList.Count;
                    EditorUtility.DisplayProgressBar("UIController Migration", prefabPath, progress);
                    if (MigratePrefab(prefabPath, ref scannedPanelCount, ref migratedPanelCount, ref warningCount))
                    {
                        migratedPrefabCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string resultMessage =
                $"Scanned {prefabPathList.Count} prefab(s), {scannedPanelCount} UIControllerPanel component(s).\n" +
                $"Migrated {migratedPanelCount} panel(s) in {migratedPrefabCount} prefab(s).";
            if (warningCount > 0)
            {
                resultMessage += $"\nWarnings: {warningCount}. Check Console for details.";
            }

            EditorUtility.DisplayDialog("UIController Migration", resultMessage, "OK");
        }

        private static bool MigratePrefab(string prefabPath, ref int scannedPanelCount, ref int migratedPanelCount, ref int warningCount)
        {
            GameObject prefabRoot = null;
            bool migrated = false;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                UIControllerPanel[] panelArray = prefabRoot.GetComponentsInChildren<UIControllerPanel>(true);
                scannedPanelCount += panelArray.Length;

                for (int i = 0; i < panelArray.Length; i++)
                {
                    UIControllerPanel panel = panelArray[i];
                    if (panel.NeedsLegacyMigration() == false)
                    {
                        continue;
                    }

                    List<string> warningList = panel.MigrateLegacyDataToControllerTargets();
                    for (int warningIndex = 0; warningIndex < warningList.Count; warningIndex++)
                    {
                        warningCount++;
                        Debug.LogWarning($"{prefabPath}: {warningList[warningIndex]}", panel);
                    }

                    EditorUtility.SetDirty(panel);
                    migratedPanelCount++;
                    migrated = true;
                }

                if (migrated)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to migrate UIControllerPanel data in prefab {prefabPath}.\n{exception}");
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            return migrated;
        }

        private static List<string> GetSelectedFolderPaths()
        {
            List<string> folderPathList = new List<string>();
            string[] selectedGuidArray = Selection.assetGUIDs;
            for (int i = 0; i < selectedGuidArray.Length; i++)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(selectedGuidArray[i]);
                if (AssetDatabase.IsValidFolder(folderPath) == false || folderPathList.Contains(folderPath))
                {
                    continue;
                }

                folderPathList.Add(folderPath);
            }

            return folderPathList;
        }

        private static List<string> GetPrefabPaths(List<string> folderPathList)
        {
            string[] prefabGuidArray = AssetDatabase.FindAssets("t:Prefab", folderPathList.ToArray());
            HashSet<string> prefabPathSet = new HashSet<string>();
            List<string> prefabPathList = new List<string>();
            for (int i = 0; i < prefabGuidArray.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuidArray[i]);
                if (string.IsNullOrWhiteSpace(prefabPath) || prefabPathSet.Add(prefabPath) == false)
                {
                    continue;
                }

                prefabPathList.Add(prefabPath);
            }

            return prefabPathList;
        }

        private static string BuildConfirmMessage(List<string> folderPathList)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Migrate all UIControllerPanel legacy data in prefabs under selected folder(s)?");
            builder.AppendLine();
            int visibleFolderCount = Mathf.Min(5, folderPathList.Count);
            for (int i = 0; i < visibleFolderCount; i++)
            {
                builder.AppendLine(folderPathList[i]);
            }

            if (folderPathList.Count > visibleFolderCount)
            {
                builder.AppendLine($"...and {folderPathList.Count - visibleFolderCount} more folder(s).");
            }

            return builder.ToString();
        }
        #endregion
    }
}
