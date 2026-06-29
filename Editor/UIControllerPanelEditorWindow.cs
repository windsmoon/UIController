using System;
using System.Collections.Generic;
using DG.Tweening;
using DG.DOTweenEditor;
using Windsmoon.UIController;
using Windsmoon.UIController.Properties;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Windsmoon.UIController.Editor
{
    public class UIControllerPanelEditorWindow : EditorWindow
    {
        private const float DeleteButtonWidth = 24f;
        private const float ShowButtonWidth = 56f;
        private const float CaptureButtonWidth = 68f;
        private const float RowLabelWidth = 118f;
        private const float PropertyPopupWidth = 170f;
        private const float PropertySlotWidth = 210f;
        private const float PropertySlotSpacing = 8f;
        private const float AnimationToggleWidth = 88f;

        #region fields
        private UIControllerPanel _uiControllerPanel;
        private int _currentControllerIndex = -1;
        private Vector2 _scrollPosition;
        private bool _pendingAnimatedShowDirty;
        private readonly Dictionary<int, bool> _targetSectionExpandedDict = new Dictionary<int, bool>();
        private readonly Dictionary<int, int> _currentStateIndexDict = new Dictionary<int, int>();
        private readonly List<string> _lastMigrationWarningList = new List<string>();
        private GUIStyle _sectionBoxStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _controllerTargetHeaderStyle;
        private GUIStyle _targetHeaderSummaryStyle;
        private GUIStyle _primaryAddButtonStyle;
        private GUIStyle _invalidPropertyPopupStyle;
        private GUIStyle _invalidPropertyDeleteButtonStyle;
        private GUIStyle _stateTargetBoxStyle;
        private GUIStyle _statePropertyBoxStyle;
        private GUIStyle _stateTargetHeaderStyle;
        #endregion

        #region methods
        [MenuItem("Window/Framework/UI/UIController Panel")]
        private static void OpenWindow()
        {
            OpenWindow(GetSelectedUIControllerPanel(), 0);
        }

        internal static void OpenWindow(UIControllerPanel uiControllerPanel, int controllerIndex)
        {
            UIControllerPanelEditorWindow window = GetWindow<UIControllerPanelEditorWindow>("UIController Panel");
            window.minSize = new Vector2(680f, 440f);
            window.titleContent = new GUIContent("UIController Panel");
            window.ResetWindowState();
            window.SetUIControllerPanel(uiControllerPanel, controllerIndex);
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(680f, 440f);
            titleContent = new GUIContent("UIController Panel");
            if (_uiControllerPanel == null)
            {
                SetUIControllerPanel(GetSelectedUIControllerPanel(), 0);
            }
        }

        private void OnDisable()
        {
            if (_uiControllerPanel != null)
            {
                _uiControllerPanel.PreviewAnimationCompleted -= OnPreviewAnimationCompleted;
            }

            _pendingAnimatedShowDirty = false;
        }

        private void OnSelectionChange()
        {
            if (_uiControllerPanel != null)
            {
                return;
            }

            UIControllerPanel uiControllerPanel = GetSelectedUIControllerPanel();
            if (uiControllerPanel != null)
            {
                SetUIControllerPanel(uiControllerPanel, 0);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (_uiControllerPanel == null)
            {
                EditorGUILayout.HelpBox("Select a UIControllerPanel and open a controller from the inspector.", MessageType.Info);
                return;
            }

            RefreshPanelCaches();
            List<UIControllerData> controllerList = _uiControllerPanel.ControllerList;
            if (controllerList.Count == 0)
            {
                DrawPanelHeader(controllerList);
                EditorGUILayout.HelpBox("UIControllerPanel has no controller.", MessageType.Info);
                return;
            }

            ValidateCurrentControllerIndex(controllerList);
            DrawPanelHeader(controllerList);
            DrawMigrationNotice();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawCurrentController(controllerList[_currentControllerIndex]);
            EditorGUILayout.EndScrollView();
        }

        private void SetUIControllerPanel(UIControllerPanel uiControllerPanel, int controllerIndex)
        {
            if (_uiControllerPanel == uiControllerPanel && _currentControllerIndex == controllerIndex)
            {
                return;
            }

            if (_uiControllerPanel != null)
            {
                _uiControllerPanel.PreviewAnimationCompleted -= OnPreviewAnimationCompleted;
            }

            _uiControllerPanel = uiControllerPanel;
            _currentControllerIndex = controllerIndex;
            _pendingAnimatedShowDirty = false;
            _scrollPosition = Vector2.zero;

            if (_uiControllerPanel != null)
            {
                RefreshPanelCaches();
                _uiControllerPanel.PreviewAnimationCompleted += OnPreviewAnimationCompleted;
            }

            Repaint();
        }

        private void ResetWindowState()
        {
            _scrollPosition = Vector2.zero;
            _pendingAnimatedShowDirty = false;
            _targetSectionExpandedDict.Clear();
            _currentStateIndexDict.Clear();
            _lastMigrationWarningList.Clear();
        }

        private static UIControllerPanel GetSelectedUIControllerPanel()
        {
            if (Selection.activeObject is UIControllerPanel uiControllerPanel)
            {
                return uiControllerPanel;
            }

            GameObject gameObject = Selection.activeGameObject;
            return gameObject == null ? null : gameObject.GetComponent<UIControllerPanel>();
        }

        private void DrawPanelHeader(List<UIControllerData> controllerList)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_uiControllerPanel.name, EditorStyles.boldLabel, GUILayout.MinWidth(180f));

            if (controllerList.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("No Controllers", GUILayout.MinWidth(220f));
                }
            }
            else
            {
                string[] controllerOptions = GetControllerOptions(controllerList);
                int newControllerIndex = EditorGUILayout.Popup(_currentControllerIndex, controllerOptions, GUILayout.MinWidth(220f));
                if (newControllerIndex != _currentControllerIndex)
                {
                    _currentControllerIndex = newControllerIndex;
                    _scrollPosition = Vector2.zero;
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping Panel", GUILayout.Width(90f)))
            {
                Selection.activeObject = _uiControllerPanel;
                EditorGUIUtility.PingObject(_uiControllerPanel);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawMigrationNotice()
        {
            if (_uiControllerPanel.NeedsLegacyMigration() == false)
            {
                if (_lastMigrationWarningList.Count > 0)
                {
                    EditorGUILayout.HelpBox($"Last migration completed with {_lastMigrationWarningList.Count} warning(s). Check Console for details.", MessageType.Warning);
                }

                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Legacy state-level target/property data was detected. Run manual migration before editing this controller.", MessageType.Warning);
            if (GUILayout.Button("Migrate Legacy Data To Controller Targets"))
            {
                RunManualMigration();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentController(UIControllerData controllerData)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(GetControllerDisplayName(controllerData.Name, _currentControllerIndex), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Controller targets define the shared structure. States only edit values and animation settings.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (IsLegacyShape(controllerData))
            {
                EditorGUILayout.HelpBox("This controller still uses legacy state-level target data. Migrate it before editing.", MessageType.Warning);
                return;
            }

            DrawControllerTargetSection(controllerData);
            EditorGUILayout.Space(18f);
            DrawStateSection(controllerData);
        }

        private void DrawControllerTargetSection(UIControllerData controllerData)
        {
            EditorGUILayout.BeginVertical(_sectionBoxStyle);
            DrawControllerTargetList(controllerData);
            EditorGUILayout.EndVertical();
        }

        private void DrawControllerTargetList(UIControllerData controllerData)
        {
            List<UIControllerTargetData> targetList = controllerData.TargetList;
            bool isSectionExpanded = GetTargetSectionExpanded();
            EditorGUILayout.BeginHorizontal(_sectionHeaderStyle);
            bool newSectionExpanded = EditorGUILayout.Foldout(isSectionExpanded, "Controller Targets", true);
            if (newSectionExpanded != isSectionExpanded)
            {
                _targetSectionExpandedDict[_currentControllerIndex] = newSectionExpanded;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{targetList.Count} targets", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (newSectionExpanded == false)
            {
                return;
            }

            if (targetList.Count == 0)
            {
                EditorGUILayout.HelpBox("Add targets here. Every state will then get value rows for the same target/property structure.", MessageType.Info);
            }

            for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
            {
                UIControllerTargetData targetData = targetList[targetIndex];
                if (targetData == null)
                {
                    int capturedIndex = targetIndex;
                    ApplyMutation("Repair UIController Target", () => controllerData.TargetList[capturedIndex] = new UIControllerTargetData());
                    return;
                }

                EditorGUILayout.BeginVertical(_stateTargetBoxStyle);
                if (DrawControllerTargetHeader(targetData, targetIndex))
                {
                    int capturedIndex = targetIndex;
                    ApplyMutation("Delete UIController Target", () => DeleteControllerTarget(controllerData, capturedIndex));
                    EditorGUILayout.EndVertical();
                    return;
                }

                DrawTargetDefinition(controllerData, targetIndex, targetData);
                EditorGUILayout.Space(4f);
                DrawControllerPropertyList(controllerData, targetIndex, targetData);

                if (targetData.RectTransform == null && targetData.PropertyNameList.Count > 0)
                {
                    EditorGUILayout.HelpBox("This target has properties but no RectTransform.", MessageType.Error);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Target", _primaryAddButtonStyle, GUILayout.Height(32f)))
            {
                ApplyMutation("Add UIController Target", () => AddControllerTarget(controllerData));
            }
        }

        private bool DrawControllerTargetHeader(UIControllerTargetData targetData, int targetIndex)
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 26f);
            EditorGUI.DrawRect(headerRect, GetStateTargetHeaderColor());
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 4f, headerRect.height), GetStateTargetAccentColor());

            Rect labelRect = new Rect(headerRect.x + 10f, headerRect.y + 3f, headerRect.width - 156f, 20f);
            GUI.Label(labelRect, GetTargetDisplayName(targetData, targetIndex), _controllerTargetHeaderStyle);

            Rect summaryRect = new Rect(headerRect.xMax - 142f, headerRect.y + 3f, 100f, 20f);
            GUI.Label(summaryRect, $"{targetData.PropertyNameList.Count} properties", _targetHeaderSummaryStyle);

            Rect deleteRect = new Rect(headerRect.xMax - DeleteButtonWidth - 8f, headerRect.y + 3f, DeleteButtonWidth, 20f);
            return GUI.Button(deleteRect, "X", EditorStyles.miniButton);
        }

        private void DrawTargetDefinition(UIControllerData controllerData, int targetIndex, UIControllerTargetData targetData)
        {
            string oldName = targetData.Name ?? string.Empty;
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Name", oldName);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyMutation("Edit UIController Target Name", () => targetData.Name = newName);
            }

            RectTransform oldRectTransform = targetData.RectTransform;
            EditorGUI.BeginChangeCheck();
            RectTransform newRectTransform = (RectTransform)EditorGUILayout.ObjectField("RectTransform", oldRectTransform, typeof(RectTransform), true);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyMutation("Edit UIController Target RectTransform", () =>
                {
                    targetData.RectTransform = newRectTransform;
                    if (string.IsNullOrWhiteSpace(targetData.Name) && newRectTransform != null)
                    {
                        targetData.Name = newRectTransform.name;
                    }

                    SyncControllerStructure(controllerData);
                });
            }
        }

        private void DrawControllerPropertyList(UIControllerData controllerData, int targetIndex, UIControllerTargetData targetData)
        {
            List<string> propertyNameList = targetData.PropertyNameList;
            List<UIControllerPropertyDefinition> availableDefinitionList = GetAvailablePropertyDefinitionList(propertyNameList, -1);
            int slotCount = propertyNameList.Count + (availableDefinitionList.Count > 0 ? 1 : 0);
            if (slotCount == 0)
            {
                EditorGUILayout.HelpBox("No controlled properties for this target.", MessageType.Info);
                return;
            }

            int columnCount = GetPropertyGridColumnCount();
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex += columnCount)
            {
                EditorGUILayout.BeginHorizontal();
                for (int columnIndex = 0; columnIndex < columnCount && slotIndex + columnIndex < slotCount; columnIndex++)
                {
                    if (columnIndex > 0)
                    {
                        GUILayout.Space(PropertySlotSpacing);
                    }

                    int currentSlotIndex = slotIndex + columnIndex;
                    if (currentSlotIndex < propertyNameList.Count)
                    {
                        if (DrawControllerPropertySlot(controllerData, targetIndex, targetData, propertyNameList, currentSlotIndex))
                        {
                            EditorGUILayout.EndHorizontal();
                            return;
                        }

                        continue;
                    }

                    if (DrawAddControllerPropertySlot(controllerData, targetIndex, availableDefinitionList))
                    {
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            DrawControllerPropertyValidation(propertyNameList);
        }

        private bool DrawControllerPropertySlot(UIControllerData controllerData, int targetIndex, UIControllerTargetData targetData, List<string> propertyNameList, int propertyIndex)
        {
            string propertyName = propertyNameList[propertyIndex];
            bool isInvalidOnTarget = IsControllerPropertyInvalidOnTarget(targetData, propertyName);
            EditorGUILayout.BeginHorizontal(GUILayout.Width(PropertySlotWidth));
            if (DrawPropertyDefinitionPopup(propertyNameList, propertyIndex, propertyName, isInvalidOnTarget, out string newPropertyName))
            {
                int capturedPropertyIndex = propertyIndex;
                string capturedPropertyName = newPropertyName;
                ApplyMutation("Change UIController Property", () => ChangeControllerProperty(controllerData, targetIndex, capturedPropertyIndex, capturedPropertyName));
                EditorGUILayout.EndHorizontal();
                return true;
            }

            GUIStyle deleteButtonStyle = isInvalidOnTarget ? _invalidPropertyDeleteButtonStyle : EditorStyles.miniButton;
            if (GUILayout.Button("X", deleteButtonStyle, GUILayout.Width(DeleteButtonWidth)))
            {
                int capturedPropertyIndex = propertyIndex;
                ApplyMutation("Delete UIController Property", () => DeleteControllerProperty(controllerData, targetIndex, capturedPropertyIndex));
                EditorGUILayout.EndHorizontal();
                return true;
            }

            EditorGUILayout.EndHorizontal();
            return false;
        }

        private bool IsControllerPropertyInvalidOnTarget(UIControllerTargetData targetData, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return true;
            }

            UIControllerProperty property = UIControllerPropertyFactory.Create(propertyName);
            if (property == null)
            {
                return true;
            }

            RectTransform rectTransform = targetData.RectTransform;
            return rectTransform != null && property.IsValid(rectTransform, out _) == false;
        }

        private bool DrawAddControllerPropertySlot(UIControllerData controllerData, int targetIndex, List<UIControllerPropertyDefinition> availableDefinitionList)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(PropertySlotWidth));
            if (GUILayout.Button("+ Add Property", GUILayout.Width(PropertyPopupWidth + DeleteButtonWidth), GUILayout.Height(20f)))
            {
                string propertyName = availableDefinitionList[0].Name;
                ApplyMutation("Add UIController Property", () => AddControllerProperty(controllerData, targetIndex, propertyName));
                EditorGUILayout.EndHorizontal();
                return true;
            }

            EditorGUILayout.EndHorizontal();
            return false;
        }

        private void DrawControllerPropertyValidation(List<string> propertyNameList)
        {
            for (int propertyIndex = 0; propertyIndex < propertyNameList.Count; propertyIndex++)
            {
                string propertyName = propertyNameList[propertyIndex];
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    EditorGUILayout.HelpBox("Property name is empty.", MessageType.Error);
                }
                else if (UIControllerPropertyFactory.Create(propertyName) == null)
                {
                    EditorGUILayout.HelpBox($"Property {propertyName} is not registered in UIControllerPropertyFactory.", MessageType.Warning);
                }
            }
        }

        private int GetPropertyGridColumnCount()
        {
            float availableWidth = Mathf.Max(PropertySlotWidth, EditorGUIUtility.currentViewWidth - 96f);
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + PropertySlotSpacing) / (PropertySlotWidth + PropertySlotSpacing)));
        }

        private bool DrawPropertyDefinitionPopup(List<string> propertyNameList, int propertyIndex, string propertyName, bool isInvalidOnTarget, out string newPropertyName)
        {
            newPropertyName = propertyName;
            List<UIControllerPropertyDefinition> availableDefinitionList = GetAvailablePropertyDefinitionList(propertyNameList, propertyIndex);
            int currentDefinitionIndex = GetPropertyDefinitionIndex(availableDefinitionList, propertyName);
            GUIStyle popupStyle = isInvalidOnTarget ? _invalidPropertyPopupStyle : EditorStyles.popup;
            if (currentDefinitionIndex < 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(0, new[] { string.IsNullOrWhiteSpace(propertyName) ? "<Missing Property>" : propertyName }, popupStyle, GUILayout.Width(PropertyPopupWidth));
                }

                return false;
            }

            string[] options = GetPropertyOptions(availableDefinitionList);
            int newIndex = EditorGUILayout.Popup(currentDefinitionIndex, options, popupStyle, GUILayout.Width(PropertyPopupWidth));
            if (newIndex == currentDefinitionIndex)
            {
                return false;
            }

            newPropertyName = availableDefinitionList[newIndex].Name;
            return true;
        }

        private void DrawStateList(UIControllerData controllerData)
        {
            List<UIControllerStateData> stateList = controllerData.StateList;
            EditorGUILayout.BeginHorizontal(_sectionHeaderStyle);
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{stateList.Count} states", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (stateList.Count == 0)
            {
                EditorGUILayout.HelpBox("Add at least one state to edit per-state values.", MessageType.Info);
                if (GUILayout.Button("+ Add State", _primaryAddButtonStyle, GUILayout.Height(32f)))
                {
                    ApplyMutation("Add UIController State", () =>
                    {
                        UIControllerStateData stateData = new UIControllerStateData();
                        controllerData.StateList.Add(stateData);
                        SyncControllerStructure(controllerData);
                    });
                    SetCurrentStateIndex(0);
                }

                return;
            }

            int currentStateIndex = GetCurrentStateIndex(stateList.Count);
            UIControllerStateData stateData = stateList[currentStateIndex];
            if (stateData == null)
            {
                int capturedIndex = currentStateIndex;
                ApplyMutation("Repair UIController State", () => controllerData.StateList[capturedIndex] = new UIControllerStateData());
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("State", GUILayout.Width(RowLabelWidth));
            int newStateIndex = EditorGUILayout.Popup(currentStateIndex, GetStateOptions(stateList), GUILayout.MinWidth(180f));
            if (newStateIndex != currentStateIndex)
            {
                currentStateIndex = newStateIndex;
                SetCurrentStateIndex(currentStateIndex);
                stateData = stateList[currentStateIndex];
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(controllerData.Name)))
            {
                if (GUILayout.Button("Show", GUILayout.Width(ShowButtonWidth)))
                {
                    ShowState(controllerData, currentStateIndex);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(BuildStateSummary(controllerData, stateData), EditorStyles.miniLabel);
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(DeleteButtonWidth)))
            {
                int capturedIndex = currentStateIndex;
                ApplyMutation("Delete UIController State", () =>
                {
                    controllerData.StateList.RemoveAt(capturedIndex);
                    int nextStateIndex = controllerData.StateList.Count == 0 ? 0 : Mathf.Clamp(capturedIndex, 0, controllerData.StateList.Count - 1);
                    SetCurrentStateIndex(nextStateIndex);
                });
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            string newComment = EditorGUILayout.TextField("Comment", stateData.Comment ?? string.Empty);
            if (EditorGUI.EndChangeCheck())
            {
                string capturedComment = newComment;
                ApplyMutation("Edit UIController State Comment", () => stateData.Comment = capturedComment);
            }

            DrawStateTargetList(controllerData, stateData, currentStateIndex);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("+ Add State", _primaryAddButtonStyle, GUILayout.Height(32f)))
            {
                ApplyMutation("Add UIController State", () =>
                {
                    UIControllerStateData newStateData = new UIControllerStateData();
                    controllerData.StateList.Add(newStateData);
                    SyncControllerStructure(controllerData);
                    SetCurrentStateIndex(controllerData.StateList.Count - 1);
                });
            }
        }

        private void DrawStateSection(UIControllerData controllerData)
        {
            EditorGUILayout.BeginVertical(_sectionBoxStyle);
            DrawStateList(controllerData);
            EditorGUILayout.EndVertical();
        }

        private void DrawStateTargetList(UIControllerData controllerData, UIControllerStateData stateData, int stateIndex)
        {
            List<UIControllerTargetData> targetList = controllerData.TargetList;
            if (targetList.Count == 0)
            {
                EditorGUILayout.HelpBox("This controller has no targets. Add targets in the Controller Targets section.", MessageType.Info);
                return;
            }

            for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
            {
                UIControllerTargetData targetData = targetList[targetIndex];
                UIControllerTargetStateData targetStateData = stateData.TargetStateList[targetIndex];
                EditorGUILayout.BeginVertical(_stateTargetBoxStyle);
                DrawStateTargetHeader(targetData, targetIndex);
                DrawStatePropertyList(stateIndex, targetIndex, targetData, targetStateData);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawStateTargetHeader(UIControllerTargetData targetData, int targetIndex)
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 26f);
            EditorGUI.DrawRect(headerRect, GetStateTargetHeaderColor());
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 4f, headerRect.height), GetStateTargetAccentColor());

            Rect labelRect = new Rect(headerRect.x + 10f, headerRect.y + 3f, headerRect.width - 18f, 20f);
            if (headerRect.width > 360f)
            {
                Rect objectRect = new Rect(headerRect.xMax - 228f, headerRect.y + 3f, 220f, 20f);
                labelRect.width = Mathf.Max(80f, objectRect.x - labelRect.x - 8f);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.ObjectField(objectRect, targetData?.RectTransform, typeof(RectTransform), true);
                }
            }

            GUI.Label(labelRect, GetTargetDisplayName(targetData, targetIndex), _stateTargetHeaderStyle);
        }

        private void DrawStatePropertyList(int stateIndex, int targetIndex, UIControllerTargetData targetData, UIControllerTargetStateData targetStateData)
        {
            List<string> propertyNameList = targetData.PropertyNameList;
            if (propertyNameList.Count == 0)
            {
                EditorGUILayout.LabelField("No properties configured for this target.", EditorStyles.miniLabel);
                return;
            }

            for (int propertyIndex = 0; propertyIndex < propertyNameList.Count; propertyIndex++)
            {
                string propertyName = propertyNameList[propertyIndex];
                UIControllerProperty property = targetStateData.PropertyList[propertyIndex];
                EditorGUILayout.BeginVertical(_statePropertyBoxStyle);
                if (property == null)
                {
                    EditorGUILayout.HelpBox($"{propertyName}: property data is missing and could not be created.", MessageType.Error);
                    EditorGUILayout.EndVertical();
                    continue;
                }

                DrawPropertyRow(propertyName, property, targetData.RectTransform);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2f);
            }
        }

        private void DrawPropertyRow(string propertyName, UIControllerProperty property, RectTransform rectTransform)
        {
            string errorMessage = null;
            bool isSupported = rectTransform != null && property.IsValid(rectTransform, out errorMessage);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(propertyName, GUILayout.Width(PropertyPopupWidth));
            if (property.CanAnimate)
            {
                bool newAnimate = EditorGUILayout.ToggleLeft("Animation", property.NeedAnimate, GUILayout.Width(AnimationToggleWidth));
                if (newAnimate != property.NeedAnimate)
                {
                    ApplyMutation("Toggle UIController Property Animation", () => property.NeedAnimate = newAnimate);
                }
            }
            else
            {
                GUILayout.Space(AnimationToggleWidth);
            }

            DrawPropertyValue(property);
            using (new EditorGUI.DisabledScope(isSupported == false))
            {
                if (GUILayout.Button("Capture", GUILayout.Width(CaptureButtonWidth)))
                {
                    ApplyMutation($"Capture UIController {propertyName}", () => property.Capture(rectTransform));
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (property.Name != propertyName)
            {
                EditorGUILayout.HelpBox($"Property data type mismatch. Expected {propertyName}, got {property.Name}.", MessageType.Error);
            }
            else if (isSupported == false)
            {
                string message = rectTransform == null ? "Target has no RectTransform." : errorMessage;
                if (string.IsNullOrEmpty(message) == false)
                {
                    EditorGUILayout.HelpBox($"{propertyName}: {message}", MessageType.Error);
                }
            }

            if (property.CanAnimate && property.NeedAnimate)
            {
                DrawPropertyAnimationOptions(property);
            }
        }

        private void DrawPropertyValue(UIControllerProperty property)
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 38f;

            if (property is UIControllerProperty<bool> boolProperty)
            {
                bool boolValue = boolProperty.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayout.Toggle("Value", boolValue, GUILayout.Width(72f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => boolProperty.SetTargetValue(newValue));
                }
            }
            else if (property is UIControllerProperty<string> stringProperty)
            {
                string stringValue = stringProperty.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUILayout.TextField("Value", stringValue, GUILayout.MinWidth(180f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => stringProperty.SetTargetValue(newValue));
                }
            }
            else if (property is UIControllerProperty<float> floatProperty)
            {
                float floatValue = floatProperty.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUILayout.FloatField("Value", floatValue, GUILayout.MinWidth(120f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => floatProperty.SetTargetValue(newValue));
                }
            }
            else if (property is UIControllerProperty<int> intProperty)
            {
                int intValue = intProperty.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                int newValue = EditorGUILayout.IntField("Value", intValue, GUILayout.MinWidth(120f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => intProperty.SetTargetValue(newValue));
                }
            }
            else if (property is UIControllerProperty<Vector2> vector2Property)
            {
                Vector2 vector2Value = vector2Property.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Value", GUILayout.Width(42f));
                Vector2 newValue = EditorGUILayout.Vector2Field(GUIContent.none, vector2Value, GUILayout.MinWidth(180f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => vector2Property.SetTargetValue(newValue));
                }
            }
            else if (property is UIControllerProperty<Vector3> vector3Property)
            {
                Vector3 vector3Value = vector3Property.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Value", GUILayout.Width(42f));
                Vector3 newValue = EditorGUILayout.Vector3Field(GUIContent.none, vector3Value, GUILayout.MinWidth(240f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => vector3Property.SetTargetValue(newValue));
                }
            }
            else if (property is UIControllerProperty<Vector4> vector4Property)
            {
                Vector4 vector4Value = vector4Property.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Value", GUILayout.Width(42f));
                Vector4 newValue = EditorGUILayout.Vector4Field(GUIContent.none, vector4Value, GUILayout.MinWidth(300f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => vector4Property.SetTargetValue(newValue));
                }
            }
            else if (property is UIControllerProperty<Color> colorProperty)
            {
                Color colorValue = colorProperty.GetTargetValue();
                EditorGUI.BeginChangeCheck();
                Color newValue = EditorGUILayout.ColorField("Value", colorValue, GUILayout.MinWidth(180f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController Property Value", () => colorProperty.SetTargetValue(newValue));
                }
            }
            else
            {
                GUILayout.Label("Unsupported Value", EditorStyles.miniLabel);
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawPropertyAnimationOptions(UIControllerProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(PropertyPopupWidth + AnimationToggleWidth + 8f);

            Ease animationEase = property.AnimationEase;
            float animationDuration = property.AnimationDuration;
            float animationDelay = property.AnimationDelay;
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Ease", GUILayout.Width(34f));
            Ease newAnimationEase = (Ease)EditorGUILayout.EnumPopup(animationEase, GUILayout.Width(150f));
            GUILayout.Label("Duration", GUILayout.Width(58f));
            float newAnimationDuration = EditorGUILayout.FloatField(animationDuration, GUILayout.Width(64f));
            GUILayout.Label("s", GUILayout.Width(12f));
            GUILayout.Label("Delay", GUILayout.Width(40f));
            float newAnimationDelay = EditorGUILayout.FloatField(animationDelay, GUILayout.Width(64f));
            GUILayout.Label("s", GUILayout.Width(12f));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyMutation("Edit UIController Property Animation", () =>
                {
                    property.AnimationEase = newAnimationEase;
                    property.AnimationDuration = newAnimationDuration;
                    property.AnimationDelay = newAnimationDelay;
                });
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void AddControllerTarget(UIControllerData controllerData)
        {
            controllerData.TargetList.Add(new UIControllerTargetData());
            SyncControllerStructure(controllerData);
        }

        private void DeleteControllerTarget(UIControllerData controllerData, int targetIndex)
        {
            controllerData.TargetList.RemoveAt(targetIndex);
            List<UIControllerStateData> stateList = controllerData.StateList;
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                List<UIControllerTargetStateData> targetStateList = stateList[stateIndex].TargetStateList;
                if (targetIndex < targetStateList.Count)
                {
                    targetStateList.RemoveAt(targetIndex);
                }
            }

            SyncControllerStructure(controllerData);
        }

        private void AddControllerProperty(UIControllerData controllerData, int targetIndex, string propertyName)
        {
            UIControllerTargetData targetData = controllerData.TargetList[targetIndex];
            targetData.PropertyNameList.Add(propertyName);
            List<UIControllerStateData> stateList = controllerData.StateList;
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerTargetStateData targetStateData = EnsureTargetState(stateList[stateIndex], targetIndex);
                targetStateData.PropertyList.Add(CreateProperty(propertyName, targetData.RectTransform));
            }

            SyncControllerStructure(controllerData);
        }

        private void ChangeControllerProperty(UIControllerData controllerData, int targetIndex, int propertyIndex, string propertyName)
        {
            UIControllerTargetData targetData = controllerData.TargetList[targetIndex];
            targetData.PropertyNameList[propertyIndex] = propertyName;
            List<UIControllerStateData> stateList = controllerData.StateList;
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerTargetStateData targetStateData = EnsureTargetState(stateList[stateIndex], targetIndex);
                EnsurePropertyListSize(targetStateData.PropertyList, propertyIndex + 1);
                targetStateData.PropertyList[propertyIndex] = CreateProperty(propertyName, targetData.RectTransform);
            }

            SyncControllerStructure(controllerData);
        }

        private void DeleteControllerProperty(UIControllerData controllerData, int targetIndex, int propertyIndex)
        {
            UIControllerTargetData targetData = controllerData.TargetList[targetIndex];
            targetData.PropertyNameList.RemoveAt(propertyIndex);
            List<UIControllerStateData> stateList = controllerData.StateList;
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                List<UIControllerProperty> propertyList = EnsureTargetState(stateList[stateIndex], targetIndex).PropertyList;
                if (propertyIndex < propertyList.Count)
                {
                    propertyList.RemoveAt(propertyIndex);
                }
            }

            SyncControllerStructure(controllerData);
        }

        private void SyncAllControllerStructures()
        {
            if (_uiControllerPanel == null)
            {
                return;
            }

            List<UIControllerData> controllerList = _uiControllerPanel.ControllerList;
            for (int controllerIndex = 0; controllerIndex < controllerList.Count; controllerIndex++)
            {
                UIControllerData controllerData = controllerList[controllerIndex];
                if (controllerData == null || IsLegacyShape(controllerData))
                {
                    continue;
                }

                SyncControllerStructure(controllerData);
            }
        }

        private void SyncControllerStructure(UIControllerData controllerData)
        {
            if (controllerData == null || IsLegacyShape(controllerData))
            {
                return;
            }

            List<UIControllerTargetData> targetList = controllerData.TargetList;
            for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
            {
                if (targetList[targetIndex] == null)
                {
                    targetList[targetIndex] = new UIControllerTargetData();
                }
            }

            List<UIControllerStateData> stateList = controllerData.StateList;
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerStateData stateData = stateList[stateIndex];
                if (stateData == null)
                {
                    stateData = new UIControllerStateData();
                    stateList[stateIndex] = stateData;
                }

                stateData.Index = stateIndex;
                List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
                while (targetStateList.Count < targetList.Count)
                {
                    UIControllerTargetData targetData = targetList[targetStateList.Count];
                    targetStateList.Add(CreateTargetStateData(targetData));
                }

                while (targetStateList.Count > targetList.Count)
                {
                    targetStateList.RemoveAt(targetStateList.Count - 1);
                }

                for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
                {
                    if (targetStateList[targetIndex] == null)
                    {
                        targetStateList[targetIndex] = new UIControllerTargetStateData();
                    }

                    SyncTargetStatePropertyList(targetList[targetIndex], targetStateList[targetIndex]);
                }
            }
        }

        private UIControllerTargetStateData EnsureTargetState(UIControllerStateData stateData, int targetIndex)
        {
            List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
            while (targetStateList.Count <= targetIndex)
            {
                targetStateList.Add(new UIControllerTargetStateData());
            }

            if (targetStateList[targetIndex] == null)
            {
                targetStateList[targetIndex] = new UIControllerTargetStateData();
            }

            return targetStateList[targetIndex];
        }

        private UIControllerTargetStateData CreateTargetStateData(UIControllerTargetData targetData)
        {
            UIControllerTargetStateData targetStateData = new UIControllerTargetStateData();
            List<string> propertyNameList = targetData.PropertyNameList;
            for (int propertyIndex = 0; propertyIndex < propertyNameList.Count; propertyIndex++)
            {
                targetStateData.PropertyList.Add(CreateProperty(propertyNameList[propertyIndex], targetData.RectTransform));
            }

            return targetStateData;
        }

        private void SyncTargetStatePropertyList(UIControllerTargetData targetData, UIControllerTargetStateData targetStateData)
        {
            List<string> propertyNameList = targetData.PropertyNameList;
            List<UIControllerProperty> propertyList = targetStateData.PropertyList;
            Dictionary<string, UIControllerProperty> existingPropertyDict = new Dictionary<string, UIControllerProperty>();
            for (int i = 0; i < propertyList.Count; i++)
            {
                UIControllerProperty property = propertyList[i];
                if (property == null || string.IsNullOrWhiteSpace(property.Name) || existingPropertyDict.ContainsKey(property.Name))
                {
                    continue;
                }

                existingPropertyDict.Add(property.Name, property);
            }

            List<UIControllerProperty> newPropertyList = new List<UIControllerProperty>(propertyNameList.Count);
            for (int propertyIndex = 0; propertyIndex < propertyNameList.Count; propertyIndex++)
            {
                string propertyName = propertyNameList[propertyIndex];
                if (existingPropertyDict.TryGetValue(propertyName, out UIControllerProperty existingProperty))
                {
                    newPropertyList.Add(existingProperty);
                }
                else
                {
                    newPropertyList.Add(CreateProperty(propertyName, targetData.RectTransform));
                }
            }

            propertyList.Clear();
            propertyList.AddRange(newPropertyList);
            targetStateData.RebuildCache();
        }

        private void EnsurePropertyListSize(List<UIControllerProperty> propertyList, int count)
        {
            while (propertyList.Count < count)
            {
                propertyList.Add(null);
            }
        }

        private UIControllerProperty CreateProperty(string propertyName, RectTransform rectTransform)
        {
            UIControllerProperty property = UIControllerPropertyFactory.Create(propertyName);
            CaptureProperty(property, rectTransform);
            return property;
        }

        private void CaptureProperty(UIControllerProperty property, RectTransform rectTransform)
        {
            if (property == null || rectTransform == null)
            {
                return;
            }

            if (property.IsValid(rectTransform, out _))
            {
                property.Capture(rectTransform);
            }
        }

        private List<UIControllerPropertyDefinition> GetAvailablePropertyDefinitionList(List<string> propertyNameList, int ignorePropertyIndex)
        {
            HashSet<string> usedPropertyNameSet = new HashSet<string>();
            for (int i = 0; i < propertyNameList.Count; i++)
            {
                if (i == ignorePropertyIndex)
                {
                    continue;
                }

                string propertyName = propertyNameList[i];
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    continue;
                }

                usedPropertyNameSet.Add(propertyName);
            }

            List<UIControllerPropertyDefinition> availableDefinitionList = new List<UIControllerPropertyDefinition>();
            for (int i = 0; i < UIControllerPropertyFactory.Definitions.Count; i++)
            {
                UIControllerPropertyDefinition definition = UIControllerPropertyFactory.Definitions[i];
                if (usedPropertyNameSet.Contains(definition.Name))
                {
                    continue;
                }

                availableDefinitionList.Add(definition);
            }

            return availableDefinitionList;
        }

        private string[] GetPropertyOptions(List<UIControllerPropertyDefinition> definitionList)
        {
            string[] options = new string[definitionList.Count];
            for (int i = 0; i < definitionList.Count; i++)
            {
                options[i] = definitionList[i].Name;
            }

            return options;
        }

        private int GetPropertyDefinitionIndex(List<UIControllerPropertyDefinition> definitionList, string propertyName)
        {
            for (int i = 0; i < definitionList.Count; i++)
            {
                if (definitionList[i].Name == propertyName)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshPanelCaches()
        {
            if (_uiControllerPanel == null)
            {
                return;
            }

            SyncAllControllerStructures();
            _uiControllerPanel.OnAfterDeserialize();
        }

        private void ApplyMutation(string undoName, Action mutation)
        {
            if (_uiControllerPanel == null || mutation == null)
            {
                return;
            }

            Undo.RecordObject(_uiControllerPanel, undoName);
            mutation();
            RefreshPanelCaches();
            MarkPanelDirty();
            Repaint();
        }

        private void RunManualMigration()
        {
            ApplyMutation("Migrate UIController Legacy Data", () =>
            {
                _lastMigrationWarningList.Clear();
                _lastMigrationWarningList.AddRange(_uiControllerPanel.MigrateLegacyDataToControllerTargets());
            });

            for (int i = 0; i < _lastMigrationWarningList.Count; i++)
            {
                Debug.LogWarning(_lastMigrationWarningList[i], _uiControllerPanel);
            }
        }

        private void ShowState(UIControllerData controllerData, int stateIndex)
        {
            if (string.IsNullOrWhiteSpace(controllerData.Name))
            {
                return;
            }

            bool hasAnimatedProperty = HasAnimatedProperty(controllerData, controllerData.StateList[stateIndex]);
            if (Application.isPlaying == false)
            {
                DOTweenEditorPreview.Stop(false, false);
                DOTweenEditorPreview.Start(null);
            }

            _pendingAnimatedShowDirty = hasAnimatedProperty;
            RefreshPanelCaches();
            _uiControllerPanel.SetControllerState(controllerData.Name, stateIndex);
            if (hasAnimatedProperty == false)
            {
                MarkPreviewTargetsDirty();
            }
        }

        private bool HasAnimatedProperty(UIControllerData controllerData, UIControllerStateData stateData)
        {
            List<UIControllerTargetData> targetList = controllerData.TargetList;
            List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
            for (int targetIndex = 0; targetIndex < targetList.Count && targetIndex < targetStateList.Count; targetIndex++)
            {
                List<UIControllerProperty> propertyList = targetStateList[targetIndex].PropertyList;
                for (int propertyIndex = 0; propertyIndex < propertyList.Count; propertyIndex++)
                {
                    UIControllerProperty property = propertyList[propertyIndex];
                    if (property != null && property.CanAnimate && property.NeedAnimate)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void MarkPreviewTargetsDirty()
        {
            if (_uiControllerPanel == null)
            {
                return;
            }

            EditorUtility.SetDirty(_uiControllerPanel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_uiControllerPanel);

            HashSet<RectTransform> dirtyTargetSet = new HashSet<RectTransform>();
            List<UIControllerData> controllerList = _uiControllerPanel.ControllerList;
            for (int controllerIndex = 0; controllerIndex < controllerList.Count; controllerIndex++)
            {
                List<UIControllerTargetData> targetList = controllerList[controllerIndex].TargetList;
                for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
                {
                    RectTransform rectTransform = targetList[targetIndex]?.RectTransform;
                    if (rectTransform == null || dirtyTargetSet.Add(rectTransform) == false)
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(rectTransform.gameObject);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform.gameObject);
                    Component[] components = rectTransform.GetComponents<Component>();
                    for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                    {
                        Component component = components[componentIndex];
                        if (component == null)
                        {
                            continue;
                        }

                        EditorUtility.SetDirty(component);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    }
                }
            }

            if (_uiControllerPanel.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(_uiControllerPanel.gameObject.scene);
            }
        }

        private void MarkPanelDirty()
        {
            if (_uiControllerPanel == null)
            {
                return;
            }

            EditorUtility.SetDirty(_uiControllerPanel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(_uiControllerPanel);
            if (_uiControllerPanel.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(_uiControllerPanel.gameObject.scene);
            }
        }

        private void OnPreviewAnimationCompleted()
        {
            if (_pendingAnimatedShowDirty == false)
            {
                return;
            }

            _pendingAnimatedShowDirty = false;
            MarkPreviewTargetsDirty();
        }

        private bool IsLegacyShape(UIControllerData controllerData)
        {
            if (controllerData == null || controllerData.TargetList.Count > 0)
            {
                return false;
            }

            List<UIControllerStateData> stateList = controllerData.StateList;
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                if (stateList[stateIndex] != null && stateList[stateIndex].TargetStateList.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ValidateCurrentControllerIndex(List<UIControllerData> controllerList)
        {
            if (_currentControllerIndex < 0 || _currentControllerIndex >= controllerList.Count)
            {
                _currentControllerIndex = 0;
            }
        }

        private string[] GetControllerOptions(List<UIControllerData> controllerList)
        {
            string[] options = new string[controllerList.Count];
            for (int i = 0; i < controllerList.Count; i++)
            {
                options[i] = GetControllerDisplayName(controllerList[i].Name, i);
            }

            return options;
        }

        private void DrawSectionHeader(string title, string summary)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(summary, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (_sectionBoxStyle != null)
            {
                return;
            }

            _sectionBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 8, 12),
                padding = new RectOffset(10, 10, 8, 10)
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 0, 8),
                padding = new RectOffset(8, 8, 5, 5)
            };

            _controllerTargetHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.95f, 0.97f, 1f, 1f)
                        : new Color(0.12f, 0.18f, 0.28f, 1f)
                }
            };

            _targetHeaderSummaryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.80f, 0.88f, 1f, 1f)
                        : new Color(0.16f, 0.28f, 0.44f, 1f)
                }
            };

            _primaryAddButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    background = CreateColorTexture(new Color(0.26f, 0.48f, 0.82f, 1f)),
                    textColor = Color.white
                },
                hover =
                {
                    background = CreateColorTexture(new Color(0.31f, 0.55f, 0.90f, 1f)),
                    textColor = Color.white
                },
                active =
                {
                    background = CreateColorTexture(new Color(0.20f, 0.40f, 0.72f, 1f)),
                    textColor = Color.white
                }
            };

            Color invalidColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.42f, 0.42f, 1f)
                : new Color(0.82f, 0.08f, 0.08f, 1f);
            _invalidPropertyPopupStyle = new GUIStyle(EditorStyles.popup)
            {
                normal = { textColor = invalidColor },
                hover = { textColor = invalidColor },
                active = { textColor = invalidColor },
                focused = { textColor = invalidColor },
                onNormal = { textColor = invalidColor },
                onHover = { textColor = invalidColor },
                onActive = { textColor = invalidColor },
                onFocused = { textColor = invalidColor }
            };

            _invalidPropertyDeleteButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = invalidColor },
                hover = { textColor = invalidColor },
                active = { textColor = invalidColor },
                focused = { textColor = invalidColor },
                onNormal = { textColor = invalidColor },
                onHover = { textColor = invalidColor },
                onActive = { textColor = invalidColor },
                onFocused = { textColor = invalidColor }
            };

            _stateTargetBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 8, 10),
                padding = new RectOffset(10, 10, 8, 10)
            };

            _statePropertyBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(16, 2, 4, 4),
                padding = new RectOffset(8, 8, 6, 6)
            };

            _stateTargetHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.95f, 0.97f, 1f, 1f)
                        : new Color(0.12f, 0.18f, 0.28f, 1f)
                }
            };
        }

        private Color GetStateTargetHeaderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.20f, 0.25f, 0.32f, 1f)
                : new Color(0.78f, 0.86f, 0.96f, 1f);
        }

        private Color GetStateTargetAccentColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.36f, 0.58f, 0.90f, 1f)
                : new Color(0.20f, 0.42f, 0.72f, 1f);
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private string GetControllerDisplayName(string controllerName, int index)
        {
            return string.IsNullOrWhiteSpace(controllerName) ? $"Controller {index + 1}" : controllerName;
        }

        private string GetTargetDisplayName(UIControllerTargetData targetData, int index)
        {
            if (targetData == null)
            {
                return $"Target {index + 1}";
            }

            if (string.IsNullOrWhiteSpace(targetData.Name) == false)
            {
                return targetData.Name;
            }

            return targetData.RectTransform == null ? $"Target {index + 1}" : targetData.RectTransform.name;
        }

        private bool GetTargetSectionExpanded()
        {
            if (_targetSectionExpandedDict.TryGetValue(_currentControllerIndex, out bool isExpanded))
            {
                return isExpanded;
            }

            _targetSectionExpandedDict[_currentControllerIndex] = true;
            return true;
        }

        private int GetCurrentStateIndex(int stateCount)
        {
            if (stateCount <= 0)
            {
                return 0;
            }

            if (_currentStateIndexDict.TryGetValue(_currentControllerIndex, out int stateIndex))
            {
                stateIndex = Mathf.Clamp(stateIndex, 0, stateCount - 1);
                _currentStateIndexDict[_currentControllerIndex] = stateIndex;
                return stateIndex;
            }

            _currentStateIndexDict[_currentControllerIndex] = 0;
            return 0;
        }

        private void SetCurrentStateIndex(int stateIndex)
        {
            _currentStateIndexDict[_currentControllerIndex] = Mathf.Max(0, stateIndex);
        }

        private string[] GetStateOptions(List<UIControllerStateData> stateList)
        {
            string[] options = new string[stateList.Count];
            for (int i = 0; i < stateList.Count; i++)
            {
                string comment = stateList[i]?.Comment;
                options[i] = string.IsNullOrWhiteSpace(comment) ? $"State {i}" : $"State {i} - {comment}";
            }

            return options;
        }

        private string BuildStateSummary(UIControllerData controllerData, UIControllerStateData stateData)
        {
            int propertyCount = 0;
            List<UIControllerTargetData> targetList = controllerData.TargetList;
            for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
            {
                propertyCount += targetList[targetIndex]?.PropertyNameList.Count ?? 0;
            }

            return $"{targetList.Count} targets  |  {propertyCount} controls";
        }
        #endregion
    }
}
