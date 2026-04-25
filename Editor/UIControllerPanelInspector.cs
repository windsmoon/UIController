using System.Collections.Generic;
using System.Text;
using Framework.UI.Controller;
using UnityEditor;
using UnityEngine;

namespace Framework.UI.Editor
{
    [CustomEditor(typeof(UIControllerPanel))]
    public class UIControllerPanelInspector : UnityEditor.Editor
    {
        private const float DeleteButtonWidth = 24f;
        private const float OpenButtonWidth = 56f;

        #region fields
        private SerializedProperty _controllerTargetBindingListProp;
        private SerializedProperty _controllerListProp;
        private readonly HashSet<string> _duplicateControllerTargetNameSet = new HashSet<string>();
        #endregion

        #region methods
        private void OnEnable()
        {
            _controllerTargetBindingListProp = serializedObject.FindProperty("_controllerTargetBindingList");
            _controllerListProp = serializedObject.FindProperty("_controllerList");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RefreshControllerTargetValidation();

            DrawOverview();
            EditorGUILayout.Space(4f);
            DrawControllerTargetBindingList();
            EditorGUILayout.Space(6f);
            DrawControllerList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverview()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("UIController Panel", EditorStyles.boldLabel);

            StringBuilder summaryBuilder = new StringBuilder();
            summaryBuilder.Append("Controllers ");
            summaryBuilder.Append(_controllerListProp.arraySize);
            summaryBuilder.Append("  |  Targets ");
            summaryBuilder.Append(_controllerTargetBindingListProp.arraySize);

            EditorGUILayout.LabelField(summaryBuilder.ToString(), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawControllerTargetBindingList()
        {
            DrawSectionHeader("Controller Targets", $"{_controllerTargetBindingListProp.arraySize} bindings");

            if (_duplicateControllerTargetNameSet.Count > 0)
            {
                EditorGUILayout.HelpBox($"Controller target name duplicated: {string.Join(", ", _duplicateControllerTargetNameSet)}", MessageType.Error);
            }

            if (_controllerTargetBindingListProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add controller targets first so UIControllers can bind to concrete UI nodes.", MessageType.Info);
            }

            for (int i = 0; i < _controllerTargetBindingListProp.arraySize; i++)
            {
                SerializedProperty bindingProp = _controllerTargetBindingListProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = bindingProp.FindPropertyRelative("Name");
                SerializedProperty rectTransformProp = bindingProp.FindPropertyRelative("RectTransform");
                RectTransform rectTransform = rectTransformProp.objectReferenceValue as RectTransform;
                string header = GetTargetDisplayName(nameProp.stringValue, i);
                string summary = rectTransform == null ? "No RectTransform" : rectTransform.name;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(summary, EditorStyles.miniLabel);
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(DeleteButtonWidth)))
                {
                    DeleteArrayElement(_controllerTargetBindingListProp, i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                string oldTargetName = nameProp.stringValue;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(nameProp, new GUIContent("Name"));
                if (EditorGUI.EndChangeCheck())
                {
                    RenameControllerTargetReferences(oldTargetName, nameProp.stringValue);
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(rectTransformProp, new GUIContent("RectTransform"));
                if (EditorGUI.EndChangeCheck() && string.IsNullOrEmpty(nameProp.stringValue) && rectTransformProp.objectReferenceValue is RectTransform autoNamedRectTransform)
                {
                    nameProp.stringValue = autoNamedRectTransform.name;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Controller Target"))
            {
                int index = _controllerTargetBindingListProp.arraySize;
                _controllerTargetBindingListProp.InsertArrayElementAtIndex(index);
                ResetControllerTargetBinding(_controllerTargetBindingListProp.GetArrayElementAtIndex(index));
            }
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
                string header = GetControllerDisplayName(controllerNameProp.stringValue, i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

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

        private void ResetControllerTargetBinding(SerializedProperty bindingProp)
        {
            bindingProp.FindPropertyRelative("Name").stringValue = string.Empty;
            bindingProp.FindPropertyRelative("RectTransform").objectReferenceValue = null;
        }

        private void ResetController(SerializedProperty controllerProp)
        {
            controllerProp.isExpanded = true;
            controllerProp.FindPropertyRelative("_name").stringValue = string.Empty;
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

        private void RefreshControllerTargetValidation()
        {
            _duplicateControllerTargetNameSet.Clear();

            HashSet<string> existingNameSet = new HashSet<string>();

            for (int i = 0; i < _controllerTargetBindingListProp.arraySize; i++)
            {
                SerializedProperty bindingProp = _controllerTargetBindingListProp.GetArrayElementAtIndex(i);
                string name = bindingProp.FindPropertyRelative("Name").stringValue;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (existingNameSet.Add(name))
                {
                    continue;
                }

                _duplicateControllerTargetNameSet.Add(name);
            }
        }

        private void RenameControllerTargetReferences(string oldTargetName, string newTargetName)
        {
            if (string.IsNullOrWhiteSpace(oldTargetName) || oldTargetName == newTargetName)
            {
                return;
            }

            for (int controllerIndex = 0; controllerIndex < _controllerListProp.arraySize; controllerIndex++)
            {
                SerializedProperty controllerProp = _controllerListProp.GetArrayElementAtIndex(controllerIndex);
                SerializedProperty stateListProp = controllerProp.FindPropertyRelative("_stateList");
                for (int stateIndex = 0; stateIndex < stateListProp.arraySize; stateIndex++)
                {
                    SerializedProperty stateProp = stateListProp.GetArrayElementAtIndex(stateIndex);
                    SerializedProperty targetStateListProp = stateProp.FindPropertyRelative("_targetStateList");
                    for (int targetIndex = 0; targetIndex < targetStateListProp.arraySize; targetIndex++)
                    {
                        SerializedProperty targetStateProp = targetStateListProp.GetArrayElementAtIndex(targetIndex);
                        SerializedProperty targetNameProp = targetStateProp.FindPropertyRelative("_name");
                        if (targetNameProp.stringValue == oldTargetName)
                        {
                            targetNameProp.stringValue = newTargetName;
                        }
                    }
                }
            }
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

        private string GetTargetDisplayName(string targetName, int index)
        {
            return string.IsNullOrWhiteSpace(targetName) ? $"Target {index + 1}" : targetName;
        }
        #endregion
    }
}
