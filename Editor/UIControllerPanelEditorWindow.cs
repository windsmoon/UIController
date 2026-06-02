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
        private struct PropertyAnimationBuffer
        {
            public Ease AnimationEase;
            public float AnimationDuration;
        }

        private const float DeleteButtonWidth = 24f;
        private const float ShowButtonWidth = 56f;
        private const float CaptureButtonWidth = 68f;
        private const float RowLabelWidth = 118f;
        private const float PropertyPopupWidth = 170f;
        private const float AnimationToggleWidth = 88f;

        #region fields
        private UIControllerPanel _uiControllerPanel;
        private int _currentControllerIndex = -1;
        private Vector2 _scrollPosition;
        private bool _pendingAnimatedShowDirty;
        private readonly Dictionary<string, bool> _stateExpandedDict = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _propertyValueEditingDict = new Dictionary<string, bool>();
        private readonly Dictionary<string, object> _propertyValueBufferDict = new Dictionary<string, object>();
        private readonly Dictionary<string, bool> _propertyAnimationEditingDict = new Dictionary<string, bool>();
        private readonly Dictionary<string, PropertyAnimationBuffer> _propertyAnimationBufferDict = new Dictionary<string, PropertyAnimationBuffer>();
        private readonly List<string> _lastMigrationWarningList = new List<string>();
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
            _stateExpandedDict.Clear();
            ClearAllPropertyEditStates();
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

            DrawControllerTargetList(controllerData);
            EditorGUILayout.Space(8f);
            DrawStateList(controllerData);
        }

        private void DrawControllerTargetList(UIControllerData controllerData)
        {
            List<UIControllerTargetData> targetList = controllerData.TargetList;
            DrawSectionHeader("Controller Targets", $"{targetList.Count} targets");

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

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetTargetDisplayName(targetData, targetIndex), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{targetData.PropertyNameList.Count} properties", EditorStyles.miniLabel);
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(DeleteButtonWidth)))
                {
                    int capturedIndex = targetIndex;
                    ApplyMutation("Delete UIController Target", () => DeleteControllerTarget(controllerData, capturedIndex));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    ClearAllPropertyEditStates();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                DrawTargetDefinition(controllerData, targetIndex, targetData);
                EditorGUILayout.Space(4f);
                DrawControllerPropertyList(controllerData, targetIndex, targetData);

                if (targetData.RectTransform == null && targetData.PropertyNameList.Count > 0)
                {
                    EditorGUILayout.HelpBox("This target has properties but no RectTransform.", MessageType.Error);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("+ Add Target", GUILayout.Height(28f)))
            {
                ApplyMutation("Add UIController Target", () => AddControllerTarget(controllerData));
            }
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
            if (propertyNameList.Count == 0)
            {
                EditorGUILayout.HelpBox("No controlled properties for this target.", MessageType.Info);
            }

            for (int propertyIndex = 0; propertyIndex < propertyNameList.Count; propertyIndex++)
            {
                string propertyName = propertyNameList[propertyIndex];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Property", GUILayout.Width(RowLabelWidth));

                if (DrawPropertyDefinitionPopup(propertyNameList, propertyIndex, propertyName, out string newPropertyName))
                {
                    int capturedPropertyIndex = propertyIndex;
                    string capturedPropertyName = newPropertyName;
                    ApplyMutation("Change UIController Property", () => ChangeControllerProperty(controllerData, targetIndex, capturedPropertyIndex, capturedPropertyName));
                    EditorGUILayout.EndHorizontal();
                    ClearAllPropertyEditStates();
                    return;
                }

                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(DeleteButtonWidth)))
                {
                    int capturedPropertyIndex = propertyIndex;
                    ApplyMutation("Delete UIController Property", () => DeleteControllerProperty(controllerData, targetIndex, capturedPropertyIndex));
                    EditorGUILayout.EndHorizontal();
                    ClearAllPropertyEditStates();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    EditorGUILayout.HelpBox("Property name is empty.", MessageType.Error);
                }
                else if (UIControllerPropertyFactory.Create(propertyName) == null)
                {
                    EditorGUILayout.HelpBox($"Property {propertyName} is not registered in UIControllerPropertyFactory.", MessageType.Warning);
                }
            }

            List<UIControllerPropertyDefinition> availableDefinitionList = GetAvailablePropertyDefinitionList(propertyNameList, -1);
            using (new EditorGUI.DisabledScope(availableDefinitionList.Count == 0))
            {
                if (GUILayout.Button("+ Add Property", GUILayout.Height(24f)))
                {
                    string propertyName = availableDefinitionList[0].Name;
                    ApplyMutation("Add UIController Property", () => AddControllerProperty(controllerData, targetIndex, propertyName));
                }
            }
        }

        private bool DrawPropertyDefinitionPopup(List<string> propertyNameList, int propertyIndex, string propertyName, out string newPropertyName)
        {
            newPropertyName = propertyName;
            List<UIControllerPropertyDefinition> availableDefinitionList = GetAvailablePropertyDefinitionList(propertyNameList, propertyIndex);
            int currentDefinitionIndex = GetPropertyDefinitionIndex(availableDefinitionList, propertyName);
            if (currentDefinitionIndex < 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(0, new[] { string.IsNullOrWhiteSpace(propertyName) ? "<Missing Property>" : propertyName }, GUILayout.Width(PropertyPopupWidth));
                }

                return false;
            }

            string[] options = GetPropertyOptions(availableDefinitionList);
            int newIndex = EditorGUILayout.Popup(currentDefinitionIndex, options, GUILayout.Width(PropertyPopupWidth));
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
            DrawSectionHeader("States", $"{stateList.Count} states");

            if (stateList.Count == 0)
            {
                EditorGUILayout.HelpBox("Add at least one state to edit per-state values.", MessageType.Info);
            }

            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerStateData stateData = stateList[stateIndex];
                if (stateData == null)
                {
                    int capturedIndex = stateIndex;
                    ApplyMutation("Repair UIController State", () => controllerData.StateList[capturedIndex] = new UIControllerStateData());
                    return;
                }

                string stateKey = GetStateKey(stateIndex);
                bool isExpanded = GetStateExpanded(stateKey);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                bool newExpanded = EditorGUILayout.Foldout(isExpanded, $"State {stateIndex}", true);
                if (newExpanded != isExpanded)
                {
                    _stateExpandedDict[stateKey] = newExpanded;
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(controllerData.Name)))
                {
                    if (GUILayout.Button("Show", GUILayout.Width(ShowButtonWidth)))
                    {
                        ShowState(controllerData, stateIndex);
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(BuildStateSummary(controllerData, stateData), EditorStyles.miniLabel);
                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(DeleteButtonWidth)))
                {
                    int capturedIndex = stateIndex;
                    ApplyMutation("Delete UIController State", () => controllerData.StateList.RemoveAt(capturedIndex));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    ClearAllPropertyEditStates();
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

                if (newExpanded)
                {
                    DrawStateTargetList(controllerData, stateData, stateIndex);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("+ Add State", GUILayout.Height(30f)))
            {
                ApplyMutation("Add UIController State", () =>
                {
                    UIControllerStateData stateData = new UIControllerStateData();
                    controllerData.StateList.Add(stateData);
                    SyncControllerStructure(controllerData);
                });
                _stateExpandedDict[GetStateKey(stateList.Count - 1)] = true;
            }
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
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(GetTargetDisplayName(targetData, targetIndex), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(targetData.RectTransform, typeof(RectTransform), true, GUILayout.Width(220f));
                }
                EditorGUILayout.EndHorizontal();

                DrawStatePropertyList(stateIndex, targetIndex, targetData, targetStateData);
                EditorGUILayout.EndVertical();
            }
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
                if (property == null)
                {
                    EditorGUILayout.HelpBox($"{propertyName}: property data is missing and could not be created.", MessageType.Error);
                    continue;
                }

                DrawPropertyRow(stateIndex, targetIndex, propertyIndex, propertyName, property, targetData.RectTransform);
            }
        }

        private void DrawPropertyRow(int stateIndex, int targetIndex, int propertyIndex, string propertyName, UIControllerProperty property, RectTransform rectTransform)
        {
            string propertyValueKey = GetPropertyValueKey(stateIndex, targetIndex, propertyIndex, propertyName);
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
                    if (newAnimate == false)
                    {
                        ClearPropertyAnimationEditState(propertyValueKey);
                    }
                }
            }
            else
            {
                GUILayout.Space(AnimationToggleWidth);
            }

            DrawPropertyValue(property, propertyValueKey);
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
                DrawPropertyAnimationOptions(property, propertyValueKey);
            }
        }

        private void DrawPropertyValue(UIControllerProperty property, string propertyValueKey)
        {
            if (IsPropertyValueEditing(propertyValueKey))
            {
                DrawPropertyValueEditor(property, propertyValueKey);
                if (GUILayout.Button("OK", GUILayout.Width(44f)))
                {
                    object value = GetPropertyValueBuffer(property, propertyValueKey);
                    ApplyMutation("Edit UIController Property Value", () => SetPropertyTargetValue(property, value));
                    ClearPropertyValueEditState(propertyValueKey);
                    GUI.FocusControl(null);
                    Repaint();
                }

                return;
            }

            DrawPropertyReadonlyValue(property);
            using (new EditorGUI.DisabledScope(CanEditPropertyValue(property) == false))
            {
                if (GUILayout.Button("Edit", GUILayout.Width(44f)))
                {
                    _propertyValueEditingDict[propertyValueKey] = true;
                    _propertyValueBufferDict[propertyValueKey] = GetPropertyTargetValue(property);
                }
            }
        }

        private void DrawPropertyReadonlyValue(UIControllerProperty property)
        {
            if (property is UIControllerProperty<Color> colorProperty)
            {
                GUILayout.Label("Value", GUILayout.Width(42f));
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ColorField(GUIContent.none, colorProperty.GetTargetValue(), false, true, false, GUILayout.Width(72f), GUILayout.Height(18f));
                }
                return;
            }

            GUILayout.Label($"Value  {property.GetValueText()}", GUILayout.MinWidth(120f));
        }

        private void DrawPropertyValueEditor(UIControllerProperty property, string propertyValueKey)
        {
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 38f;
            object value = GetPropertyValueBuffer(property, propertyValueKey);

            if (property is UIControllerProperty<bool>)
            {
                bool boolValue = value is bool boolBuffer && boolBuffer;
                bool newValue = EditorGUILayout.Toggle("Value", boolValue, GUILayout.Width(72f));
                if (newValue != boolValue)
                {
                    _propertyValueBufferDict[propertyValueKey] = newValue;
                }
            }
            else if (property is UIControllerProperty<string>)
            {
                string stringValue = value as string ?? string.Empty;
                string newValue = EditorGUILayout.TextField("Value", stringValue, GUILayout.MinWidth(180f));
                if (newValue != stringValue)
                {
                    _propertyValueBufferDict[propertyValueKey] = newValue;
                }
            }
            else if (property is UIControllerProperty<float>)
            {
                float floatValue = value is float floatBuffer ? floatBuffer : 0f;
                float newValue = EditorGUILayout.FloatField("Value", floatValue, GUILayout.MinWidth(120f));
                if (!Mathf.Approximately(newValue, floatValue))
                {
                    _propertyValueBufferDict[propertyValueKey] = newValue;
                }
            }
            else if (property is UIControllerProperty<Vector2>)
            {
                Vector2 vector2Value = value is Vector2 vector2Buffer ? vector2Buffer : Vector2.zero;
                Vector2 newValue = EditorGUILayout.Vector2Field("Value", vector2Value, GUILayout.MinWidth(180f));
                if (newValue != vector2Value)
                {
                    _propertyValueBufferDict[propertyValueKey] = newValue;
                }
            }
            else if (property is UIControllerProperty<Vector3>)
            {
                Vector3 vector3Value = value is Vector3 vector3Buffer ? vector3Buffer : Vector3.zero;
                Vector3 newValue = EditorGUILayout.Vector3Field("Value", vector3Value, GUILayout.MinWidth(240f));
                if (newValue != vector3Value)
                {
                    _propertyValueBufferDict[propertyValueKey] = newValue;
                }
            }
            else if (property is UIControllerProperty<Color>)
            {
                Color colorValue = value is Color colorBuffer ? colorBuffer : Color.white;
                Color newValue = EditorGUILayout.ColorField("Value", colorValue, GUILayout.MinWidth(180f));
                if (newValue != colorValue)
                {
                    _propertyValueBufferDict[propertyValueKey] = newValue;
                }
            }
            else
            {
                GUILayout.Label("Unsupported Value", EditorStyles.miniLabel);
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawPropertyAnimationOptions(UIControllerProperty property, string propertyValueKey)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(PropertyPopupWidth + AnimationToggleWidth + 8f);

            if (IsPropertyAnimationEditing(propertyValueKey))
            {
                PropertyAnimationBuffer buffer = GetPropertyAnimationBuffer(property, propertyValueKey);
                GUILayout.Label("Ease", GUILayout.Width(34f));
                buffer.AnimationEase = (Ease)EditorGUILayout.EnumPopup(buffer.AnimationEase, GUILayout.Width(150f));
                GUILayout.Label("Duration", GUILayout.Width(58f));
                buffer.AnimationDuration = EditorGUILayout.FloatField(buffer.AnimationDuration, GUILayout.Width(64f));
                GUILayout.Label("s", GUILayout.Width(12f));
                _propertyAnimationBufferDict[propertyValueKey] = buffer;

                if (GUILayout.Button("OK", GUILayout.Width(44f)))
                {
                    PropertyAnimationBuffer finalBuffer = buffer;
                    ApplyMutation("Edit UIController Property Animation", () =>
                    {
                        property.AnimationEase = finalBuffer.AnimationEase;
                        property.AnimationDuration = finalBuffer.AnimationDuration;
                    });
                    ClearPropertyAnimationEditState(propertyValueKey);
                    GUI.FocusControl(null);
                    Repaint();
                }
            }
            else
            {
                GUILayout.Label($"Ease  {property.AnimationEase}", GUILayout.Width(160f));
                GUILayout.Label($"Duration  {property.AnimationDuration:0.###}s", GUILayout.Width(130f));
                if (GUILayout.Button("Edit", GUILayout.Width(44f)))
                {
                    _propertyAnimationEditingDict[propertyValueKey] = true;
                    _propertyAnimationBufferDict[propertyValueKey] = new PropertyAnimationBuffer
                    {
                        AnimationEase = property.AnimationEase,
                        AnimationDuration = property.AnimationDuration
                    };
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private bool IsPropertyValueEditing(string propertyValueKey)
        {
            if (_propertyValueEditingDict.TryGetValue(propertyValueKey, out bool isEditing))
            {
                return isEditing;
            }

            _propertyValueEditingDict[propertyValueKey] = false;
            return false;
        }

        private void ClearPropertyValueEditState(string propertyValueKey)
        {
            _propertyValueEditingDict.Remove(propertyValueKey);
            _propertyValueBufferDict.Remove(propertyValueKey);
        }

        private bool IsPropertyAnimationEditing(string propertyValueKey)
        {
            if (_propertyAnimationEditingDict.TryGetValue(propertyValueKey, out bool isEditing))
            {
                return isEditing;
            }

            _propertyAnimationEditingDict[propertyValueKey] = false;
            return false;
        }

        private void ClearPropertyAnimationEditState(string propertyValueKey)
        {
            _propertyAnimationEditingDict.Remove(propertyValueKey);
            _propertyAnimationBufferDict.Remove(propertyValueKey);
        }

        private void ClearAllPropertyEditStates()
        {
            _propertyValueEditingDict.Clear();
            _propertyValueBufferDict.Clear();
            _propertyAnimationEditingDict.Clear();
            _propertyAnimationBufferDict.Clear();
        }

        private PropertyAnimationBuffer GetPropertyAnimationBuffer(UIControllerProperty property, string propertyValueKey)
        {
            if (_propertyAnimationBufferDict.TryGetValue(propertyValueKey, out PropertyAnimationBuffer buffer))
            {
                return buffer;
            }

            buffer = new PropertyAnimationBuffer
            {
                AnimationEase = property.AnimationEase,
                AnimationDuration = property.AnimationDuration
            };
            _propertyAnimationBufferDict[propertyValueKey] = buffer;
            return buffer;
        }

        private string GetPropertyValueKey(int stateIndex, int targetIndex, int propertyIndex, string propertyName)
        {
            return $"{_currentControllerIndex}:{stateIndex}:{targetIndex}:{propertyIndex}:{propertyName}";
        }

        private bool CanEditPropertyValue(UIControllerProperty property)
        {
            return property is UIControllerProperty<bool> ||
                   property is UIControllerProperty<string> ||
                   property is UIControllerProperty<float> ||
                   property is UIControllerProperty<Vector2> ||
                   property is UIControllerProperty<Vector3> ||
                   property is UIControllerProperty<Color>;
        }

        private object GetPropertyValueBuffer(UIControllerProperty property, string propertyValueKey)
        {
            if (_propertyValueBufferDict.TryGetValue(propertyValueKey, out object value))
            {
                return value;
            }

            value = GetPropertyTargetValue(property);
            _propertyValueBufferDict[propertyValueKey] = value;
            return value;
        }

        private object GetPropertyTargetValue(UIControllerProperty property)
        {
            if (property is UIControllerProperty<bool> boolProperty)
            {
                return boolProperty.GetTargetValue();
            }

            if (property is UIControllerProperty<float> floatProperty)
            {
                return floatProperty.GetTargetValue();
            }

            if (property is UIControllerProperty<string> stringProperty)
            {
                return stringProperty.GetTargetValue();
            }

            if (property is UIControllerProperty<Vector2> vector2Property)
            {
                return vector2Property.GetTargetValue();
            }

            if (property is UIControllerProperty<Vector3> vector3Property)
            {
                return vector3Property.GetTargetValue();
            }

            if (property is UIControllerProperty<Color> colorProperty)
            {
                return colorProperty.GetTargetValue();
            }

            return null;
        }

        private void SetPropertyTargetValue(UIControllerProperty property, object value)
        {
            if (property is UIControllerProperty<bool> boolProperty && value is bool boolValue)
            {
                boolProperty.SetTargetValue(boolValue);
            }
            else if (property is UIControllerProperty<float> floatProperty && value is float floatValue)
            {
                floatProperty.SetTargetValue(floatValue);
            }
            else if (property is UIControllerProperty<string> stringProperty && value is string stringValue)
            {
                stringProperty.SetTargetValue(stringValue);
            }
            else if (property is UIControllerProperty<Vector2> vector2Property && value is Vector2 vector2Value)
            {
                vector2Property.SetTargetValue(vector2Value);
            }
            else if (property is UIControllerProperty<Vector3> vector3Property && value is Vector3 vector3Value)
            {
                vector3Property.SetTargetValue(vector3Value);
            }
            else if (property is UIControllerProperty<Color> colorProperty && value is Color colorValue)
            {
                colorProperty.SetTargetValue(colorValue);
            }
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
                ClearAllPropertyEditStates();
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
                DOTweenEditorPreview.Stop(false, true);
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

        private string GetStateKey(int stateIndex)
        {
            return $"{_currentControllerIndex}:{stateIndex}";
        }

        private bool GetStateExpanded(string stateKey)
        {
            if (_stateExpandedDict.TryGetValue(stateKey, out bool isExpanded))
            {
                return isExpanded;
            }

            _stateExpandedDict[stateKey] = true;
            return true;
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
