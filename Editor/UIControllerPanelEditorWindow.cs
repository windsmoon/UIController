using System;
using System.Collections.Generic;
using DG.DOTweenEditor;
using Framework.UI.Controller;
using Framework.UI.Controller.Properties;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Framework.UI.Editor
{
    public class UIControllerPanelEditorWindow : EditorWindow
    {
        private const float DeleteButtonWidth = 24f;
        private const float ShowButtonWidth = 56f;
        private const float CaptureButtonWidth = 58f;
        private const float CommentButtonWidth = 40f;
        private const float PropertyPopupWidth = 160f;
        private const float AnimationToggleWidth = 88f;
        private const float RowLabelWidth = 110f;
        private const float StateBlockSpacing = 10f;
        private const float TargetBlockSpacing = 8f;
        private const float ExpandedStateContentSpacing = 6f;

        #region fields
        private UIControllerPanel _uiControllerPanel;
        private int _currentControllerIndex = -1;
        private Vector2 _scrollPosition;
        private bool _pendingAnimatedShowDirty;
        private readonly Dictionary<string, bool> _stateExpandedDict = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _stateCommentEditingDict = new Dictionary<string, bool>();
        private readonly Dictionary<string, string> _stateCommentBufferDict = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _propertyValueEditingDict = new Dictionary<string, bool>();
        private readonly Dictionary<string, object> _propertyValueBufferDict = new Dictionary<string, object>();
        private readonly List<string> _controllerTargetNameList = new List<string>();
        private GUIStyle _toolbarCardStyle;
        private GUIStyle _headerCardStyle;
        private GUIStyle _stateCardStyle;
        private GUIStyle _targetCardStyle;
        private GUIStyle _headerTitleStyle;
        private GUIStyle _headerSubtitleStyle;
        private GUIStyle _toolbarFieldStyle;
        private GUIStyle _toolbarPopupStyle;
        private GUIStyle _stateFoldoutStyle;
        private GUIStyle _mutedLabelStyle;
        private GUIStyle _rowLabelStyle;
        private GUIStyle _summaryLabelStyle;
        private GUIStyle _inlineValueLabelStyle;
        private GUIStyle _showButtonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIStyle _outlineButtonStyle;
        private GUIStyle _outlineButtonDisabledStyle;
        private GUIStyle _primaryAddButtonStyle;
        private GUIStyle _iconButtonStyle;
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
            window.minSize = new Vector2(640f, 420f);
            window.titleContent = new GUIContent("UIController Panel");
            window.ResetWindowState();
            window.SetUIControllerPanel(uiControllerPanel, controllerIndex);
            window.Show();
        }

        private void OnEnable()
        {
            minSize = new Vector2(640f, 420f);
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
            if (uiControllerPanel == null)
            {
                return;
            }

            SetUIControllerPanel(uiControllerPanel, 0);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawWindowBackground();

            if (_uiControllerPanel == null)
            {
                EditorGUILayout.HelpBox("Select a UIControllerPanel and open a controller from the inspector.", MessageType.Info);
                return;
            }

            RefreshPanelCaches();
            List<UIControllerData> controllerList = _uiControllerPanel.ControllerList;
            if (controllerList == null || controllerList.Count == 0)
            {
                EditorGUILayout.HelpBox("UIControllerPanel has no controller.", MessageType.Info);
                return;
            }

            ValidateCurrentControllerIndex(controllerList);
            RefreshControllerTargetNames();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10f);
            DrawToolbar(controllerList);
            EditorGUILayout.Space(10f);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawCurrentController(controllerList[_currentControllerIndex]);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.Space(12f);
            EditorGUILayout.EndHorizontal();
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
            _stateCommentEditingDict.Clear();
            _stateCommentBufferDict.Clear();
            _propertyValueEditingDict.Clear();
            _propertyValueBufferDict.Clear();
        }

        private static UIControllerPanel GetSelectedUIControllerPanel()
        {
            if (Selection.activeObject is UIControllerPanel uiControllerPanel)
            {
                return uiControllerPanel;
            }

            GameObject gameObject = Selection.activeGameObject;
            if (gameObject == null)
            {
                return null;
            }

            return gameObject.GetComponent<UIControllerPanel>();
        }

        private void DrawToolbar(List<UIControllerData> controllerList)
        {
            EditorGUILayout.BeginVertical(_toolbarCardStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GetIconContent(_uiControllerPanel.name, "Prefab Icon", typeof(GameObject)), _toolbarFieldStyle, GUILayout.Width(220f), GUILayout.Height(28f));
            GUILayout.Space(8f);

            string[] controllerOptions = GetControllerOptions(controllerList);
            int newControllerIndex = EditorGUILayout.Popup(_currentControllerIndex, controllerOptions, _toolbarPopupStyle, GUILayout.MinWidth(220f), GUILayout.Height(28f));
            if (newControllerIndex != _currentControllerIndex)
            {
                _currentControllerIndex = newControllerIndex;
                _scrollPosition = Vector2.zero;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Ping Panel", _secondaryButtonStyle, GUILayout.Width(92f), GUILayout.Height(28f)))
            {
                Selection.activeObject = _uiControllerPanel;
                EditorGUIUtility.PingObject(_uiControllerPanel);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentController(UIControllerData controllerData)
        {
            EditorGUILayout.BeginVertical(_headerCardStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GetIconContent(string.Empty, "Prefab Icon", typeof(GameObject)), GUILayout.Width(20f), GUILayout.Height(20f));
            GUILayout.Space(2f);
            EditorGUILayout.LabelField(GetControllerDisplayName(controllerData.Name, _currentControllerIndex), _headerTitleStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Use Show to preview a state. Use Capture on each property row to record current values.", _headerSubtitleStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10f);
            DrawStateList(controllerData);
        }

        private void DrawStateList(UIControllerData controllerData)
        {
            List<UIControllerStateData> stateList = controllerData.StateList;
            if (stateList.Count == 0)
            {
                EditorGUILayout.HelpBox("Add at least one state to start previewing UI changes.", MessageType.Info);
            }

            for (int i = 0; i < stateList.Count; i++)
            {
                if (i > 0)
                {
                    EditorGUILayout.Space(StateBlockSpacing);
                }

                UIControllerStateData stateData = stateList[i];
                string stateKey = GetStateKey(i);
                bool isExpanded = GetStateExpanded(stateKey);

                BeginTintedHelpBox(i, false);
                EditorGUILayout.BeginHorizontal();
                bool newExpanded = EditorGUILayout.Foldout(isExpanded, $"State {stateData.Index}", true, _stateFoldoutStyle);
                if (newExpanded != isExpanded)
                {
                    _stateExpandedDict[stateKey] = newExpanded;
                }

                GUILayout.Space(8f);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(controllerData.Name)))
                {
                    if (GUILayout.Button("Show", _showButtonStyle, GUILayout.Width(ShowButtonWidth), GUILayout.Height(24f)))
                    {
                        ShowState(controllerData, stateData);
                    }
                }

                DrawStateComment(stateData, stateKey);
                GUILayout.FlexibleSpace();
                GUILayout.Label(BuildStateSummary(stateData), _summaryLabelStyle);

                if (GUILayout.Button("X", _iconButtonStyle, GUILayout.Width(DeleteButtonWidth), GUILayout.Height(24f)))
                {
                    ApplyMutation("Delete UIController State", () =>
                    {
                        stateList.RemoveAt(i);
                        SyncControllerStateIndexes();
                    });
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                if (newExpanded)
                {
                    EditorGUILayout.Space(ExpandedStateContentSpacing);
                    DrawTargetStateList(stateData);
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add State", _primaryAddButtonStyle, GUILayout.Width(320f), GUILayout.Height(42f)))
            {
                ApplyMutation("Add UIController State", () =>
                {
                    stateList.Add(new UIControllerStateData());
                    SyncControllerStateIndexes();
                });
                _stateExpandedDict[GetStateKey(stateList.Count - 1)] = true;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(12f);
        }

        private void DrawTargetStateList(UIControllerStateData stateData)
        {
            List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
            DrawTargetStateListValidation(targetStateList);

            if (targetStateList.Count == 0)
            {
                EditorGUILayout.HelpBox("Add target entries for this state, then choose which properties each target controls.", MessageType.Info);
            }

            for (int i = 0; i < targetStateList.Count; i++)
            {
                if (i > 0)
                {
                    EditorGUILayout.Space(TargetBlockSpacing);
                }

                UIControllerTargetStateData targetStateData = targetStateList[i];
                BeginTintedHelpBox(i, true);
                if (DrawTargetState(stateData.Index, targetStateData, targetStateList, i))
                {
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.EndVertical();
            }

            string firstAvailableTargetName = GetFirstAvailableTargetName(targetStateList, -1);
            bool canAddTarget = string.IsNullOrEmpty(firstAvailableTargetName) == false;
            GUIStyle addTargetButtonStyle = canAddTarget ? _outlineButtonStyle : _outlineButtonDisabledStyle;
            if (GUILayout.Button("+ Add Target", addTargetButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(32f)) && canAddTarget)
            {
                ApplyMutation("Add UIController Target", () =>
                {
                    UIControllerTargetStateData targetStateData = new UIControllerTargetStateData();
                    targetStateData.Name = firstAvailableTargetName;
                    targetStateList.Add(targetStateData);
                });
            }
        }

        private void DrawStateComment(UIControllerStateData stateData, string stateKey)
        {
            bool editing = IsStateCommentEditing(stateKey);
            if (editing)
            {
                GUILayout.Space(12f);
                GUILayout.Label("Comment:", _mutedLabelStyle, GUILayout.Width(62f));
                string commentBuffer = GetStateCommentBuffer(stateKey, stateData.Comment);
                string newCommentBuffer = EditorGUILayout.TextField(commentBuffer, GUILayout.MinWidth(160f));
                if (newCommentBuffer != commentBuffer)
                {
                    _stateCommentBufferDict[stateKey] = newCommentBuffer;
                }

                if (GUILayout.Button("OK", _secondaryButtonStyle, GUILayout.Width(CommentButtonWidth), GUILayout.Height(22f)))
                {
                    string finalComment = GetStateCommentBuffer(stateKey, stateData.Comment);
                    if (finalComment != (stateData.Comment ?? string.Empty))
                    {
                        ApplyMutation("Edit UIController State Comment", () => stateData.Comment = finalComment);
                    }

                    _stateCommentEditingDict[stateKey] = false;
                    GUI.FocusControl(null);
                    Repaint();
                }

                return;
            }

            GUILayout.Space(12f);
            string comment = stateData.Comment ?? string.Empty;
            GUIContent commentContent = new GUIContent($"Comment: {comment}");
            float commentWidth = _mutedLabelStyle.CalcSize(commentContent).x + 6f;
            GUILayout.Label(commentContent, _mutedLabelStyle, GUILayout.Width(commentWidth));
            GUILayout.Space(6f);
            if (GUILayout.Button("Edit", _secondaryButtonStyle, GUILayout.Width(CommentButtonWidth + 8f), GUILayout.Height(22f)))
            {
                _stateCommentEditingDict[stateKey] = true;
                _stateCommentBufferDict[stateKey] = comment;
            }

            return;
#if false

            bool isEditing = IsStateCommentEditing(stateKey);
            if (isEditing)
            {
                GUILayout.Space(4f);
                GUILayout.Label("注释:", EditorStyles.miniLabel, GUILayout.Width(32f));
                EditorGUI.BeginChangeCheck();
                string newComment = EditorGUILayout.DelayedTextField(stateData.Comment ?? string.Empty, GUILayout.MinWidth(160f));
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyMutation("Edit UIController State Comment", () => stateData.Comment = newComment);
                }

                if (GUILayout.Button("OK", EditorStyles.miniButton, GUILayout.Width(CommentButtonWidth)))
                {
                    _stateCommentEditingDict[stateKey] = false;
                }

                return;
            }

            string comment = string.IsNullOrWhiteSpace(stateData.Comment) ? "-" : stateData.Comment;
            GUILayout.Space(4f);
            GUILayout.Label($"注释: {comment}", EditorStyles.miniLabel);
            if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(CommentButtonWidth)))
            {
                _stateCommentEditingDict[stateKey] = true;
            }
        #endif
        }

        private void DrawTargetStateListValidation(List<UIControllerTargetStateData> targetStateList)
        {
            HashSet<string> existingTargetNameSet = new HashSet<string>();
            List<string> duplicateTargetNameList = new List<string>();
            int emptyTargetCount = 0;

            for (int i = 0; i < targetStateList.Count; i++)
            {
                string targetName = targetStateList[i]?.Name;
                if (string.IsNullOrWhiteSpace(targetName))
                {
                    emptyTargetCount++;
                    continue;
                }

                if (existingTargetNameSet.Add(targetName))
                {
                    continue;
                }

                if (duplicateTargetNameList.Contains(targetName) == false)
                {
                    duplicateTargetNameList.Add(targetName);
                }
            }

            if (duplicateTargetNameList.Count > 0)
            {
                EditorGUILayout.HelpBox($"Duplicate target in this state: {string.Join(", ", duplicateTargetNameList)}", MessageType.Error);
            }

            if (emptyTargetCount > 0)
            {
                EditorGUILayout.HelpBox($"This state contains {emptyTargetCount} empty target entry.", MessageType.Warning);
            }
        }

        private bool DrawTargetState(int stateIndex, UIControllerTargetStateData targetStateData, List<UIControllerTargetStateData> targetStateList, int targetIndex)
        {
            if (DrawTargetNamePopup(targetStateData, targetStateList, targetIndex))
            {
                return true;
            }

            RectTransform rectTransform = FindTargetRectTransform(targetStateData.Name);
            DrawReadOnlyObjectRow("RectTransform", rectTransform, typeof(RectTransform));

            EditorGUILayout.Space(4f);

            DrawPropertyList(stateIndex, targetIndex, targetStateData, rectTransform);

            if (string.IsNullOrWhiteSpace(targetStateData.Name) && targetStateData.PropertyList.Count > 0)
            {
                EditorGUILayout.HelpBox("Select a Target before previewing or capturing controlled properties.", MessageType.Error);
            }
            else if (rectTransform == null && targetStateData.PropertyList.Count > 0)
            {
                EditorGUILayout.HelpBox($"{targetStateData.Name} has no RectTransform binding.", MessageType.Error);
            }

            return false;
        }

        private void DrawPropertyList(int stateIndex, int targetIndex, UIControllerTargetStateData targetStateData, RectTransform rectTransform)
        {
            List<UIControllerProperty> propertyList = targetStateData.PropertyList;
            for (int i = 0; i < propertyList.Count; i++)
            {
                UIControllerProperty property = propertyList[i];
                if (property == null)
                {
                    continue;
                }

                if (DrawPropertyRow(stateIndex, targetIndex, targetStateData, propertyList, i, rectTransform))
                {
                    return;
                }
            }

            DrawAddPropertyButton(targetStateData, propertyList, rectTransform);
        }

        private bool DrawPropertyRow(int stateIndex, int targetIndex, UIControllerTargetStateData targetStateData, List<UIControllerProperty> propertyList, int propertyIndex, RectTransform rectTransform)
        {
            UIControllerProperty property = propertyList[propertyIndex];
            string propertyValueKey = GetPropertyValueKey(stateIndex, targetIndex, property);
            string errorMessage = null;
            bool isSupported = rectTransform != null && property.IsValid(rectTransform, out errorMessage);

            EditorGUILayout.BeginHorizontal();
            DrawPropertyNamePopup(targetStateData, propertyList, propertyIndex, rectTransform, propertyValueKey);
            GUILayout.Space(8f);
            if (property.CanAnimate)
            {
                bool newAnimate = EditorGUILayout.ToggleLeft("Animation", property.NeedAnimate, GUILayout.Width(AnimationToggleWidth));
                if (newAnimate != property.NeedAnimate)
                {
                    ApplyMutation("Toggle UIController Property Animation", () => property.NeedAnimate = newAnimate);
                }
            }

            DrawPropertyValue(property, propertyValueKey);
            using (new EditorGUI.DisabledScope(isSupported == false))
            {
                if (GUILayout.Button("Capture", _secondaryButtonStyle, GUILayout.Width(CaptureButtonWidth + 10f), GUILayout.Height(24f)))
                {
                    ApplyMutation($"Capture UIController {property.Name}", () => property.Capture(rectTransform));
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", _iconButtonStyle, GUILayout.Width(DeleteButtonWidth), GUILayout.Height(24f)))
            {
                ApplyMutation("Delete UIController Property", () =>
                {
                    propertyList.RemoveAt(propertyIndex);
                    targetStateData.RebuildCache();
                });
                ClearPropertyValueEditState(propertyValueKey);
                EditorGUILayout.EndHorizontal();
                return true;
            }

            EditorGUILayout.EndHorizontal();

            if (isSupported == false && string.IsNullOrEmpty(errorMessage) == false)
            {
                EditorGUILayout.HelpBox($"{property.Name}: {errorMessage}", MessageType.Error);
            }

            return false;
        }

        private void DrawPropertyValue(UIControllerProperty property, string propertyValueKey)
        {
            if (IsPropertyValueEditing(propertyValueKey))
            {
                DrawPropertyValueEditor(property, propertyValueKey);
                if (GUILayout.Button("OK", _secondaryButtonStyle, GUILayout.Width(CommentButtonWidth + 8f), GUILayout.Height(24f)))
                {
                    object value = GetPropertyValueBuffer(property, propertyValueKey);
                    ApplyMutation("Edit UIController Property Value", () => SetPropertyTargetValue(property, value));
                    ClearPropertyValueEditState(propertyValueKey);
                    GUI.FocusControl(null);
                    Repaint();
                }

                return;
            }

            GUILayout.Label($"Value  {property.GetValueText()}", _inlineValueLabelStyle);
            using (new EditorGUI.DisabledScope(CanEditPropertyValue(property) == false))
            {
                if (GUILayout.Button("Edit", _secondaryButtonStyle, GUILayout.Width(CommentButtonWidth + 8f), GUILayout.Height(24f)))
                {
                    _propertyValueEditingDict[propertyValueKey] = true;
                    _propertyValueBufferDict[propertyValueKey] = GetPropertyTargetValue(property);
                }
            }
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
                if (newValue != floatValue)
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

        private string GetPropertyValueKey(int stateIndex, int targetIndex, UIControllerProperty property)
        {
            return $"{_currentControllerIndex}:{stateIndex}:{targetIndex}:{property.Name}";
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

        private void DrawPropertyNamePopup(UIControllerTargetStateData targetStateData, List<UIControllerProperty> propertyList, int propertyIndex, RectTransform rectTransform, string propertyValueKey)
        {
            List<UIControllerPropertyDefinition> availableDefinitionList = GetAvailablePropertyDefinitionList(propertyList, propertyIndex);
            if (availableDefinitionList.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(0, new[] { "<No Available Properties>" }, _toolbarPopupStyle, GUILayout.Width(PropertyPopupWidth), GUILayout.Height(24f));
                }

                return;
            }

            string[] options = GetPropertyOptions(availableDefinitionList);
            UIControllerProperty property = propertyList[propertyIndex];
            int popupIndex = GetPropertyDefinitionIndex(availableDefinitionList, property.Name);
            if (popupIndex < 0)
            {
                popupIndex = 0;
            }

            using (new EditorGUI.DisabledScope(rectTransform == null))
            {
                int newIndex = EditorGUILayout.Popup(popupIndex, options, _toolbarPopupStyle, GUILayout.Width(PropertyPopupWidth), GUILayout.Height(24f));
                if (newIndex != popupIndex)
                {
                    UIControllerPropertyDefinition definition = availableDefinitionList[newIndex];
                    ApplyMutation("Change UIController Property", () =>
                    {
                        UIControllerProperty newProperty = definition.Create();
                        CaptureProperty(newProperty, rectTransform);
                        propertyList[propertyIndex] = newProperty;
                        targetStateData.RebuildCache();
                    });
                    ClearPropertyValueEditState(propertyValueKey);
                }
            }
        }

        private void DrawAddPropertyButton(UIControllerTargetStateData targetStateData, List<UIControllerProperty> propertyList, RectTransform rectTransform)
        {
            List<UIControllerPropertyDefinition> availableDefinitionList = GetAvailablePropertyDefinitionList(propertyList, -1);
            bool canAddProperty = availableDefinitionList.Count > 0 && rectTransform != null;
            GUIStyle addPropertyButtonStyle = canAddProperty ? _outlineButtonStyle : _outlineButtonDisabledStyle;
            if (GUILayout.Button("+ Add Property", addPropertyButtonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(32f)) && canAddProperty)
            {
                UIControllerPropertyDefinition definition = availableDefinitionList[0];
                ApplyMutation("Add UIController Property", () =>
                {
                    UIControllerProperty property = definition.Create();
                    CaptureProperty(property, rectTransform);
                    targetStateData.SetProperty(property);
                });
            }
        }

        private List<UIControllerPropertyDefinition> GetAvailablePropertyDefinitionList(List<UIControllerProperty> propertyList, int ignorePropertyIndex)
        {
            HashSet<string> usedPropertyNameSet = new HashSet<string>();
            for (int i = 0; i < propertyList.Count; i++)
            {
                if (i == ignorePropertyIndex)
                {
                    continue;
                }

                UIControllerProperty property = propertyList[i];
                if (property == null || string.IsNullOrWhiteSpace(property.Name))
                {
                    continue;
                }

                usedPropertyNameSet.Add(property.Name);
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

        private void CaptureProperty(UIControllerProperty property, RectTransform rectTransform)
        {
            if (rectTransform == null || property == null)
            {
                return;
            }

            if (property.IsValid(rectTransform, out _))
            {
                property.Capture(rectTransform);
            }
        }

        private bool DrawTargetNamePopup(UIControllerTargetStateData targetStateData, List<UIControllerTargetStateData> targetStateList, int targetIndex)
        {
            if (_controllerTargetNameList.Count > 0 &&
                string.IsNullOrEmpty(targetStateData.Name) == false &&
                _controllerTargetNameList.Contains(targetStateData.Name) == false)
            {
                EditorGUILayout.HelpBox($"Target {targetStateData.Name} not found in controller targets.", MessageType.Error);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Target", _rowLabelStyle, GUILayout.Width(RowLabelWidth), GUILayout.Height(24f));

            List<string> availableTargetNameList = GetAvailableTargetNameList(targetStateList, targetIndex);
            if (availableTargetNameList.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(0, new[] { "<No Available Targets>" }, _toolbarPopupStyle, GUILayout.Height(24f));
                }
            }
            else
            {
                string[] options = new string[availableTargetNameList.Count];
                for (int i = 0; i < availableTargetNameList.Count; i++)
                {
                    options[i] = availableTargetNameList[i];
                }

                int popupIndex = availableTargetNameList.IndexOf(targetStateData.Name);
                if (popupIndex < 0)
                {
                    popupIndex = 0;
                }

                int newIndex = EditorGUILayout.Popup(popupIndex, options, _toolbarPopupStyle, GUILayout.Height(24f));
                if (newIndex != popupIndex)
                {
                    string targetName = availableTargetNameList[newIndex];
                    ApplyMutation("Change UIController Target", () => targetStateData.Name = targetName);
                }
            }

            if (GUILayout.Button("X", _iconButtonStyle, GUILayout.Width(DeleteButtonWidth), GUILayout.Height(24f)))
            {
                ApplyMutation("Delete UIController Target", () => targetStateList.RemoveAt(targetIndex));
                EditorGUILayout.EndHorizontal();
                return true;
            }

            EditorGUILayout.EndHorizontal();
            return false;
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

        private void BeginTintedHelpBox(int index, bool isTargetBlock)
        {
            GUIStyle style = isTargetBlock ? _targetCardStyle : _stateCardStyle;
            Rect rect = EditorGUILayout.BeginVertical(style);
            if (UnityEngine.Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color fillColor = GetTintedHelpBoxColor(index, isTargetBlock);
            Color borderColor = GetCardBorderColor(isTargetBlock);
            Rect fillRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            EditorGUI.DrawRect(fillRect, fillColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), borderColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), borderColor);
        }

        private Color GetTintedHelpBoxColor(int index, bool isTargetBlock)
        {
            bool isEven = index % 2 == 0;
            if (EditorGUIUtility.isProSkin)
            {
                if (isTargetBlock == false)
                {
                    return isEven
                        ? new Color(0.18f, 0.20f, 0.24f, 0.98f)
                        : new Color(0.17f, 0.19f, 0.23f, 0.98f);
                }

                return isEven
                    ? new Color(0.20f, 0.22f, 0.26f, 0.98f)
                    : new Color(0.19f, 0.21f, 0.25f, 0.98f);
            }

            if (isTargetBlock == false)
            {
                return isEven
                    ? new Color(0.92f, 0.94f, 0.97f, 1f)
                    : new Color(0.90f, 0.92f, 0.96f, 1f);
            }

            return isEven
                ? new Color(0.95f, 0.96f, 0.98f, 1f)
                : new Color(0.92f, 0.94f, 0.97f, 1f);
        }

        private List<string> GetAvailableTargetNameList(List<UIControllerTargetStateData> targetStateList, int ignoreTargetIndex)
        {
            HashSet<string> usedTargetNameSet = new HashSet<string>();
            for (int i = 0; i < targetStateList.Count; i++)
            {
                if (i == ignoreTargetIndex)
                {
                    continue;
                }

                string targetName = targetStateList[i]?.Name;
                if (string.IsNullOrWhiteSpace(targetName))
                {
                    continue;
                }

                usedTargetNameSet.Add(targetName);
            }

            List<string> availableTargetNameList = new List<string>();
            for (int i = 0; i < _controllerTargetNameList.Count; i++)
            {
                string targetName = _controllerTargetNameList[i];
                if (usedTargetNameSet.Contains(targetName))
                {
                    continue;
                }

                availableTargetNameList.Add(targetName);
            }

            return availableTargetNameList;
        }

        private string GetFirstAvailableTargetName(List<UIControllerTargetStateData> targetStateList, int ignoreTargetIndex)
        {
            List<string> availableTargetNameList = GetAvailableTargetNameList(targetStateList, ignoreTargetIndex);
            if (availableTargetNameList.Count == 0)
            {
                return null;
            }

            return availableTargetNameList[0];
        }

        private void RefreshControllerTargetNames()
        {
            _controllerTargetNameList.Clear();
            List<UIControllerTargetBinding> bindingList = _uiControllerPanel.ControllerTargetBindingList;
            HashSet<string> existingNameSet = new HashSet<string>();

            for (int i = 0; i < bindingList.Count; i++)
            {
                string targetName = bindingList[i].Name;
                if (string.IsNullOrWhiteSpace(targetName) || existingNameSet.Add(targetName) == false)
                {
                    continue;
                }

                _controllerTargetNameList.Add(targetName);
            }
        }

        private void RefreshPanelCaches()
        {
            if (_uiControllerPanel == null)
            {
                return;
            }

            SyncControllerStateIndexes();
            _uiControllerPanel.OnAfterDeserialize();
        }

        private void SyncControllerStateIndexes()
        {
            if (_uiControllerPanel == null)
            {
                return;
            }

            List<UIControllerData> controllerList = _uiControllerPanel.ControllerList;
            for (int i = 0; i < controllerList.Count; i++)
            {
                List<UIControllerStateData> stateList = controllerList[i].StateList;
                for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
                {
                    stateList[stateIndex].Index = stateIndex;
                }
            }
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

        private RectTransform FindTargetRectTransform(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            List<UIControllerTargetBinding> bindingList = _uiControllerPanel.ControllerTargetBindingList;
            for (int i = 0; i < bindingList.Count; i++)
            {
                if (bindingList[i].Name == targetName)
                {
                    return bindingList[i].RectTransform;
                }
            }

            return null;
        }

        private void ShowState(UIControllerData controllerData, UIControllerStateData stateData)
        {
            if (string.IsNullOrWhiteSpace(controllerData.Name))
            {
                return;
            }

            bool hasAnimatedProperty = HasAnimatedProperty(stateData);
            if (Application.isPlaying == false)
            {
                DOTweenEditorPreview.Stop(false, true);
                DOTweenEditorPreview.Start(null);
            }

            _pendingAnimatedShowDirty = hasAnimatedProperty;
            RefreshPanelCaches();
            _uiControllerPanel.SetControllerState(controllerData.Name, stateData.Index);
            if (hasAnimatedProperty == false)
            {
                MarkPreviewTargetsDirty();
            }
        }

        private bool HasAnimatedProperty(UIControllerStateData stateData)
        {
            List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
            for (int i = 0; i < targetStateList.Count; i++)
            {
                List<UIControllerProperty> propertyList = targetStateList[i].PropertyList;
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

            List<UIControllerTargetBinding> bindingList = _uiControllerPanel.ControllerTargetBindingList;
            for (int i = 0; i < bindingList.Count; i++)
            {
                RectTransform rectTransform = bindingList[i].RectTransform;
                if (rectTransform == null)
                {
                    continue;
                }

                EditorUtility.SetDirty(rectTransform.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform.gameObject);
                EditorUtility.SetDirty(rectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rectTransform);
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

        private void ValidateCurrentControllerIndex(List<UIControllerData> controllerList)
        {
            if (_currentControllerIndex < 0 || _currentControllerIndex >= controllerList.Count)
            {
                _currentControllerIndex = 0;
            }
        }

        private string GetControllerDisplayName(string controllerName, int index)
        {
            return string.IsNullOrWhiteSpace(controllerName) ? $"Controller {index + 1}" : controllerName;
        }

        private string BuildStateSummary(UIControllerStateData stateData)
        {
            int targetCount = stateData.TargetStateList.Count;
            int controlledPropertyCount = 0;
            for (int i = 0; i < stateData.TargetStateList.Count; i++)
            {
                controlledPropertyCount += stateData.TargetStateList[i].PropertyDict.Count;
            }

            return $"{targetCount} entries  |  {controlledPropertyCount} controls";
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

        private bool IsStateCommentEditing(string stateKey)
        {
            if (_stateCommentEditingDict.TryGetValue(stateKey, out bool isEditing))
            {
                return isEditing;
            }

            _stateCommentEditingDict[stateKey] = false;
            return false;
        }

        private string GetStateCommentBuffer(string stateKey, string currentComment)
        {
            if (_stateCommentBufferDict.TryGetValue(stateKey, out string commentBuffer))
            {
                return commentBuffer;
            }

            commentBuffer = currentComment ?? string.Empty;
            _stateCommentBufferDict[stateKey] = commentBuffer;
            return commentBuffer;
        }

        private void EnsureStyles()
        {
            if (_toolbarCardStyle != null)
            {
                return;
            }

            Color toolbarFillColor = EditorGUIUtility.isProSkin ? new Color(0.17f, 0.19f, 0.22f, 1f) : new Color(0.92f, 0.94f, 0.97f, 1f);
            Color headerFillColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.21f, 0.25f, 1f) : new Color(0.94f, 0.96f, 0.98f, 1f);
            Color stateFillColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.20f, 0.24f, 1f) : new Color(0.95f, 0.96f, 0.98f, 1f);
            Color targetFillColor = EditorGUIUtility.isProSkin ? new Color(0.20f, 0.22f, 0.26f, 1f) : new Color(0.97f, 0.98f, 0.99f, 1f);
            Color textColor = EditorGUIUtility.isProSkin ? new Color(0.92f, 0.95f, 0.98f, 1f) : new Color(0.20f, 0.24f, 0.29f, 1f);
            Color mutedTextColor = EditorGUIUtility.isProSkin ? new Color(0.70f, 0.76f, 0.83f, 1f) : new Color(0.35f, 0.40f, 0.48f, 1f);

            _toolbarCardStyle = CreateCardStyle(toolbarFillColor, new RectOffset(12, 12, 12, 12));
            _headerCardStyle = CreateCardStyle(headerFillColor, new RectOffset(16, 16, 14, 14));
            _stateCardStyle = CreateCardStyle(stateFillColor, new RectOffset(14, 14, 12, 12));
            _targetCardStyle = CreateCardStyle(targetFillColor, new RectOffset(12, 12, 10, 10));

            _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = textColor }
            };

            _headerSubtitleStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = mutedTextColor }
            };

            _toolbarFieldStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 7, 7),
                fontSize = 12,
                normal = { textColor = textColor, background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.20f, 0.23f, 0.28f, 1f) : new Color(0.96f, 0.97f, 0.99f, 1f)) }
            };

            _toolbarPopupStyle = new GUIStyle(EditorStyles.popup)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                padding = new RectOffset(10, 24, 5, 5),
                normal = { textColor = textColor, background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.20f, 0.23f, 0.28f, 1f) : new Color(0.96f, 0.97f, 0.99f, 1f)) },
                hover = { textColor = textColor, background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.22f, 0.25f, 0.30f, 1f) : new Color(0.94f, 0.96f, 0.99f, 1f)) },
                active = { textColor = textColor, background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.22f, 0.25f, 0.30f, 1f) : new Color(0.94f, 0.96f, 0.99f, 1f)) },
                focused = { textColor = textColor, background = CreateColorTexture(EditorGUIUtility.isProSkin ? new Color(0.20f, 0.23f, 0.28f, 1f) : new Color(0.96f, 0.97f, 0.99f, 1f)) }
            };

            _stateFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = textColor },
                onNormal = { textColor = textColor }
            };

            _mutedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 12,
                normal = { textColor = mutedTextColor }
            };

            _rowLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = mutedTextColor }
            };

            _summaryLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = mutedTextColor }
            };

            _inlineValueLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 12,
                normal = { textColor = textColor }
            };

            _showButtonStyle = CreateButtonStyle(
                new Color(0.28f, 0.46f, 0.76f, 1f),
                new Color(0.33f, 0.51f, 0.82f, 1f),
                new Color(0.24f, 0.41f, 0.68f, 1f),
                Color.white,
                FontStyle.Bold,
                new RectOffset(10, 10, 4, 4));

            _secondaryButtonStyle = CreateButtonStyle(
                EditorGUIUtility.isProSkin ? new Color(0.24f, 0.27f, 0.32f, 1f) : new Color(0.88f, 0.91f, 0.96f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.28f, 0.31f, 0.37f, 1f) : new Color(0.86f, 0.89f, 0.95f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.22f, 0.25f, 0.30f, 1f) : new Color(0.84f, 0.87f, 0.93f, 1f),
                textColor,
                FontStyle.Normal,
                new RectOffset(10, 10, 4, 4));

            _outlineButtonStyle = CreateButtonStyle(
                EditorGUIUtility.isProSkin ? new Color(0.25f, 0.31f, 0.40f, 1f) : new Color(0.87f, 0.92f, 0.99f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.29f, 0.36f, 0.47f, 1f) : new Color(0.83f, 0.89f, 0.98f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.21f, 0.27f, 0.36f, 1f) : new Color(0.79f, 0.86f, 0.97f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.95f, 0.98f, 1f, 1f) : new Color(0.15f, 0.23f, 0.35f, 1f),
                FontStyle.Bold,
                new RectOffset(12, 12, 6, 6));

            _outlineButtonDisabledStyle = CreateButtonStyle(
                EditorGUIUtility.isProSkin ? new Color(0.16f, 0.19f, 0.24f, 1f) : new Color(0.91f, 0.94f, 0.98f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.16f, 0.19f, 0.24f, 1f) : new Color(0.91f, 0.94f, 0.98f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.16f, 0.19f, 0.24f, 1f) : new Color(0.91f, 0.94f, 0.98f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.72f, 0.79f, 0.88f, 1f) : new Color(0.42f, 0.49f, 0.58f, 1f),
                FontStyle.Bold,
                new RectOffset(12, 12, 6, 6));

            _primaryAddButtonStyle = CreateButtonStyle(
                new Color(0.31f, 0.53f, 0.88f, 1f),
                new Color(0.36f, 0.58f, 0.93f, 1f),
                new Color(0.27f, 0.47f, 0.80f, 1f),
                Color.white,
                FontStyle.Bold,
                new RectOffset(12, 12, 7, 7));

            _iconButtonStyle = CreateButtonStyle(
                EditorGUIUtility.isProSkin ? new Color(0.22f, 0.25f, 0.30f, 1f) : new Color(0.90f, 0.93f, 0.97f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.26f, 0.29f, 0.34f, 1f) : new Color(0.88f, 0.91f, 0.96f, 1f),
                EditorGUIUtility.isProSkin ? new Color(0.20f, 0.22f, 0.27f, 1f) : new Color(0.86f, 0.89f, 0.94f, 1f),
                mutedTextColor,
                FontStyle.Bold,
                new RectOffset(4, 4, 4, 4));
        }

        private void DrawWindowBackground()
        {
            Color backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.14f, 0.16f, 0.19f, 1f)
                : new Color(0.90f, 0.93f, 0.97f, 1f);
            EditorGUI.DrawRect(new Rect(0f, 0f, position.width, position.height), backgroundColor);
        }

        private void DrawReadOnlyObjectRow(string labelText, UnityEngine.Object value, System.Type defaultType)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(labelText, _rowLabelStyle, GUILayout.Width(RowLabelWidth), GUILayout.Height(24f));
            using (new EditorGUI.DisabledScope(true))
            {
                System.Type objectType = value != null ? value.GetType() : defaultType;
                EditorGUILayout.ObjectField(value, objectType, true, GUILayout.Height(24f));
            }

            EditorGUILayout.EndHorizontal();
        }

        private GUIContent GetIconContent(string text, string iconName, System.Type objectType)
        {
            Texture image = null;
            if (image == null && string.IsNullOrEmpty(iconName) == false)
            {
                GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
                image = iconContent?.image;
            }

            if (image == null && objectType != null)
            {
                GUIContent typeContent = EditorGUIUtility.ObjectContent(null, objectType);
                image = typeContent?.image;
            }

            return image != null ? new GUIContent($" {text}", image) : new GUIContent(text);
        }

        private GUIStyle CreateCardStyle(Color fillColor, RectOffset padding)
        {
            return new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = padding,
                normal = { background = CreateColorTexture(fillColor) }
            };
        }

        private GUIStyle CreateButtonStyle(Color normalColor, Color hoverColor, Color activeColor, Color textColor, FontStyle fontStyle, RectOffset padding)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = fontStyle,
                fontSize = 12,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(padding.left, padding.right, Math.Max(2, padding.top - 2), Math.Max(2, padding.bottom - 2)),
                clipping = TextClipping.Overflow,
                wordWrap = false,
                contentOffset = Vector2.zero
            };

            Texture2D normalTexture = CreateColorTexture(normalColor);
            Texture2D hoverTexture = CreateColorTexture(hoverColor);
            Texture2D activeTexture = CreateColorTexture(activeColor);

            style.normal.background = normalTexture;
            style.hover.background = hoverTexture;
            style.active.background = activeTexture;
            style.focused.background = normalTexture;
            style.onNormal.background = normalTexture;
            style.onHover.background = hoverTexture;
            style.onActive.background = activeTexture;
            style.onFocused.background = normalTexture;

            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            style.focused.textColor = textColor;
            style.onNormal.textColor = textColor;
            style.onHover.textColor = textColor;
            style.onActive.textColor = textColor;
            style.onFocused.textColor = textColor;
            return style;
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

        private Color GetCardBorderColor(bool isTargetBlock)
        {
            if (EditorGUIUtility.isProSkin)
            {
                return isTargetBlock
                    ? new Color(0.28f, 0.32f, 0.38f, 1f)
                    : new Color(0.31f, 0.36f, 0.43f, 1f);
            }

            return isTargetBlock
                ? new Color(0.80f, 0.84f, 0.90f, 1f)
                : new Color(0.76f, 0.82f, 0.89f, 1f);
        }
        #endregion
    }
}
