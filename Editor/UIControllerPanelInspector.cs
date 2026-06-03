using System.Collections.Generic;
using System.Text;
using Windsmoon.UIController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Windsmoon.UIController.Editor
{
    [CustomEditor(typeof(UIControllerPanel))]
    public class UIControllerPanelInspector : UnityEditor.Editor
    {
        private const float DeleteButtonWidth = 24f;
        private const float OpenButtonWidth = 56f;

        #region fields
        private SerializedProperty _dataVersionProp;
        private SerializedProperty _controllerListProp;
        #endregion

        #region methods
        private void OnEnable()
        {
            _dataVersionProp = serializedObject.FindProperty("_dataVersion");
            _controllerListProp = serializedObject.FindProperty("_controllerList");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawOverview();
            EditorGUILayout.Space(4f);
            DrawMigrationNotice();
            EditorGUILayout.Space(6f);
            DrawControllerList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("UIController Panel", EditorStyles.boldLabel);

            StringBuilder summaryBuilder = new StringBuilder();
            summaryBuilder.Append("Data Version ");
            summaryBuilder.Append(_dataVersionProp?.intValue ?? 0);
            summaryBuilder.Append("  |  Controllers ");
            summaryBuilder.Append(_controllerListProp.arraySize);

            EditorGUILayout.LabelField(summaryBuilder.ToString(), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawMigrationNotice()
        {
            UIControllerPanel uiControllerPanel = (UIControllerPanel)target;
            if (uiControllerPanel.NeedsLegacyMigration() == false)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Legacy state-level target/property data was detected. Run manual migration before editing in the panel window.", MessageType.Warning);
            if (GUILayout.Button("Migrate Legacy Data To Controller Targets"))
            {
                serializedObject.ApplyModifiedProperties();
                RunManualMigration(uiControllerPanel);
                serializedObject.Update();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawControllerList()
        {
            DrawSectionHeader("UI Controllers", $"{_controllerListProp.arraySize} controllers");

            if (_controllerListProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No UIController has been created yet.", MessageType.Info);
            }

            for (int i = 0; i < _controllerListProp.arraySize; i++)
            {
                SerializedProperty controllerProp = _controllerListProp.GetArrayElementAtIndex(i);
                SerializedProperty controllerNameProp = controllerProp.FindPropertyRelative("_name");
                SerializedProperty targetListProp = controllerProp.FindPropertyRelative("_targetList");
                SerializedProperty stateListProp = controllerProp.FindPropertyRelative("_stateList");
                string header = GetControllerDisplayName(controllerNameProp.stringValue, i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{targetListProp.arraySize} targets  |  {stateListProp.arraySize} states", EditorStyles.miniLabel);

                if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(OpenButtonWidth)))
                {
                    serializedObject.ApplyModifiedProperties();
                    UIControllerPanelEditorWindow.OpenWindow((UIControllerPanel)target, i);
                }

                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(DeleteButtonWidth)))
                {
                    DeleteArrayElement(_controllerListProp, i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(controllerNameProp, new GUIContent("Name"));
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add UIController"))
            {
                int index = _controllerListProp.arraySize;
                _controllerListProp.InsertArrayElementAtIndex(index);
                ResetController(_controllerListProp.GetArrayElementAtIndex(index));
            }
        }

        private void ResetController(SerializedProperty controllerProp)
        {
            controllerProp.isExpanded = true;
            controllerProp.FindPropertyRelative("_name").stringValue = string.Empty;
            controllerProp.FindPropertyRelative("_targetList").arraySize = 0;
            controllerProp.FindPropertyRelative("_stateList").arraySize = 0;
        }

        private void DeleteArrayElement(SerializedProperty arrayProp, int index)
        {
            int oldSize = arrayProp.arraySize;
            arrayProp.DeleteArrayElementAtIndex(index);

            if (arrayProp.arraySize == oldSize)
            {
                arrayProp.DeleteArrayElementAtIndex(index);
            }
        }

        private void RunManualMigration(UIControllerPanel uiControllerPanel)
        {
            Undo.RecordObject(uiControllerPanel, "Migrate UIController Legacy Data");
            List<string> warningList = uiControllerPanel.MigrateLegacyDataToControllerTargets();
            EditorUtility.SetDirty(uiControllerPanel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(uiControllerPanel);
            if (uiControllerPanel.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(uiControllerPanel.gameObject.scene);
            }

            for (int i = 0; i < warningList.Count; i++)
            {
                Debug.LogWarning(warningList[i], uiControllerPanel);
            }

            string message = warningList.Count == 0
                ? "Legacy data migration completed."
                : $"Legacy data migration completed with {warningList.Count} warning(s). Check Console for details.";
            EditorUtility.DisplayDialog("UIController Migration", message, "OK");
        }

        private void DrawSectionHeader(string title, string summary)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (string.IsNullOrEmpty(summary) == false)
            {
                GUILayout.Label(summary, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private string GetControllerDisplayName(string controllerName, int index)
        {
            return string.IsNullOrWhiteSpace(controllerName) ? $"Controller {index + 1}" : controllerName;
        }
        #endregion
    }
}
