using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using Windsmoon.UIController;
using Windsmoon.UIController.Properties;

namespace Windsmoon.Controller
{
    [DisallowMultipleComponent]
    public class UIControllerPanel : MonoBehaviour, ISerializationCallbackReceiver
    {
        private readonly struct UIControllerTweenKey : IEquatable<UIControllerTweenKey>
        {
            #region fields
            private readonly RectTransform _targetRectTransform;
            private readonly string _propertyName;
            #endregion

            #region methods
            public UIControllerTweenKey(RectTransform targetRectTransform, string propertyName)
            {
                _targetRectTransform = targetRectTransform;
                _propertyName = propertyName;
            }

            public bool Equals(UIControllerTweenKey other)
            {
                return _targetRectTransform == other._targetRectTransform && _propertyName == other._propertyName;
            }

            public override bool Equals(object obj)
            {
                return obj is UIControllerTweenKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_targetRectTransform, _propertyName);
            }
            #endregion
        }

        #region fields
        [SerializeField]
        private int _dataVersion = 1;
#if UNITY_EDITOR
#pragma warning disable CS0618
        [SerializeField, HideInInspector, Obsolete("Legacy data for manual migration only.")]
        private List<UIControllerTargetBinding> _controllerTargetBindingList = new List<UIControllerTargetBinding>();
#pragma warning restore CS0618
#endif
        [SerializeField]
        private List<UIControllerData> _controllerList = new List<UIControllerData>();

        private Dictionary<string, UIControllerData> _controllerDict;
        private Dictionary<UIControllerTweenKey, Tween> _propertyTweenDict;
#if UNITY_EDITOR
        public event Action PreviewAnimationCompleted;
        private static readonly MethodInfo _prepareTweenForPreviewMethod = Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview, DOTweenEditor")?.GetMethod("PrepareTweenForPreview", BindingFlags.Public | BindingFlags.Static);
        private StateAnimationCompletion _previewAnimationCompletion;
#endif
        #endregion

        #region properties
        public int DataVersion => _dataVersion;
        public List<UIControllerData> ControllerList => _controllerList;
#if UNITY_EDITOR
#pragma warning disable CS0618
        public List<UIControllerTargetBinding> LegacyControllerTargetBindingList => _controllerTargetBindingList;
#pragma warning restore CS0618
#endif
        #endregion

        #region interface impls
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            DeserializeControllerDict();
            DeserializeStateCaches();
        }
        #endregion

        #region methods
        public void SetControllerState(string controllerName, int stateIndex, bool forceNoAnimation = false)
        {
            SetControllerState(controllerName, stateIndex, null, forceNoAnimation);
        }

        // 没有动画时同步回调；属性动画结束或被 Kill 时都计入完成。
        public void SetControllerState(string controllerName, int stateIndex, Action onCompleted, bool forceNoAnimation = false)
        {
            if (_controllerDict == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(controllerName))
            {
                throw new Exception($"controllerName is null or empty on panel {name}");
            }
            if (stateIndex < 0)
            {
                throw new Exception($"stateIndex is invalid on panel {name}");
            }

            UIControllerData controllerData = FindController(controllerName);
            UIControllerStateData stateData = FindState(controllerData, stateIndex);
            ApplyControllerState(controllerData, stateData, forceNoAnimation, onCompleted);
        }

        public void SetControllerState(string controllerName, string stateName, bool forceNoAnimation = false)
        {
            SetControllerState(controllerName, stateName, null, forceNoAnimation);
        }

        public void SetControllerState(string controllerName, string stateName, Action onCompleted, bool forceNoAnimation = false)
        {
            if (_controllerDict == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(controllerName))
            {
                throw new Exception($"controllerName is null or empty on panel {name}");
            }
            if (string.IsNullOrWhiteSpace(stateName))
            {
                throw new Exception($"stateName is null or whitespace on panel {name}");
            }

            UIControllerData controllerData = FindController(controllerName);
            UIControllerStateData stateData = FindState(controllerData, stateName);
            ApplyControllerState(controllerData, stateData, forceNoAnimation, onCompleted);
        }

        public void StopControllerAnimation(string controllerName)
        {
            if (HasController(controllerName) == false)
            {
                return;
            }

            UIControllerData controllerData = _controllerDict[controllerName];
            foreach (UIControllerTargetData targetData in controllerData.TargetList)
            {
                if (targetData == null)
                {
                    continue;
                }

                foreach (string propertyName in targetData.PropertyNameList)
                {
                    KillTween(targetData.RectTransform, propertyName);
                }
            }
        }

        public bool HasController(string controllerName)
        {
            if (string.IsNullOrEmpty(controllerName) || _controllerDict == null)
            {
                return false;
            }

            return _controllerDict.ContainsKey(controllerName);
        }

        public bool HasControllerState(string controllerName, int stateIndex)
        {
            if (stateIndex < 0 || HasController(controllerName) == false)
            {
                return false;
            }

            UIControllerData controllerData = _controllerDict[controllerName];
            List<UIControllerStateData> stateList = controllerData.StateList;
            return stateIndex < stateList.Count;
        }

        public bool HasControllerState(string controllerName, string stateName)
        {
            return TryGetStateIndex(controllerName, stateName, out _);
        }

        public bool TryGetStateIndex(string controllerName, string stateName, out int stateIndex)
        {
            stateIndex = -1;
            if (string.IsNullOrWhiteSpace(stateName) || HasController(controllerName) == false)
            {
                return false;
            }

            UIControllerData controllerData = _controllerDict[controllerName];
            return controllerData.TryGetStateIndex(stateName, out stateIndex);
        }

        public int GetStateCount(string controllerName)
        {
            if (HasController(controllerName) == false)
            {
                return 0;
            }

            UIControllerData controllerData = _controllerDict[controllerName];
            return controllerData.StateList.Count;
        }

        public float GetStateAnimationDuration(string controllerName, int stateIndex)
        {
            if (HasControllerState(controllerName, stateIndex) == false)
            {
                return 0f;
            }

            UIControllerData controllerData = _controllerDict[controllerName];
            UIControllerStateData stateData = controllerData.StateList[stateIndex];
            float maxDuration = 0f;
            List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
            for (int targetIndex = 0; targetIndex < targetStateList.Count; targetIndex++)
            {
                UIControllerTargetStateData targetStateData = targetStateList[targetIndex];
                if (targetStateData == null)
                {
                    continue;
                }

                List<UIControllerProperty> propertyList = targetStateData.PropertyList;
                for (int propertyIndex = 0; propertyIndex < propertyList.Count; propertyIndex++)
                {
                    UIControllerProperty property = propertyList[propertyIndex];
                    if (property == null || property.CanAnimate == false || property.NeedAnimate == false || property.AnimationDuration <= 0f)
                    {
                        continue;
                    }

                    maxDuration = Mathf.Max(maxDuration, property.AnimationDelay + property.AnimationDuration);
                }
            }

            return maxDuration;
        }

        public float GetStateAnimationDuration(string controllerName, string stateName)
        {
            if (TryGetStateIndex(controllerName, stateName, out int stateIndex) == false)
            {
                return 0f;
            }

            return GetStateAnimationDuration(controllerName, stateIndex);
        }

#if UNITY_EDITOR
        public bool NeedsLegacyMigration()
        {
            List<UIControllerData> controllerList = ControllerList;
            for (int i = 0; i < controllerList.Count; i++)
            {
                if (ControllerNeedsLegacyMigration(controllerList[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public List<string> MigrateLegacyDataToControllerTargets()
        {
            List<string> warningList = new List<string>();
            Dictionary<string, RectTransform> legacyTargetBindingDict = BuildLegacyTargetBindingDict(warningList);
            List<UIControllerData> controllerList = ControllerList;

            for (int controllerIndex = 0; controllerIndex < controllerList.Count; controllerIndex++)
            {
                UIControllerData controllerData = controllerList[controllerIndex];
                if (ControllerNeedsLegacyMigration(controllerData) == false)
                {
                    continue;
                }

                MigrateLegacyController(controllerData, controllerIndex, legacyTargetBindingDict, warningList);
            }

            _dataVersion = 1;
            DeserializeStateCaches();
            return warningList;
        }
#endif

        private void DeserializeControllerDict()
        {
            List<UIControllerData> controllerList = ControllerList;
            if (controllerList.Count == 0)
            {
                _controllerDict = null;
                return;
            }

            _controllerDict = new Dictionary<string, UIControllerData>(controllerList.Count);
            foreach (UIControllerData controllerData in controllerList)
            {
                if (controllerData == null)
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(controllerData.Name))
                {
                    continue;
                }
                if (_controllerDict.ContainsKey(controllerData.Name))
                {
                    continue;
                }

                _controllerDict.Add(controllerData.Name, controllerData);
            }

            if (_controllerDict.Count == 0)
            {
                _controllerDict = null;
            }
        }

        private void DeserializeStateCaches()
        {
            List<UIControllerData> controllerList = ControllerList;
            for (int i = 0; i < controllerList.Count; i++)
            {
                UIControllerData controllerData = controllerList[i];
                if (controllerData == null)
                {
                    continue;
                }

                controllerData.RebuildCache();
            }
        }

        private UIControllerData FindController(string controllerName)
        {
            if (_controllerDict.TryGetValue(controllerName, out UIControllerData controllerData))
            {
                return controllerData;
            }

            throw new Exception($"can't find controller {controllerName} on panel {name}");
        }

        private UIControllerStateData FindState(UIControllerData controllerData, int stateIndex)
        {
            List<UIControllerStateData> stateList = controllerData.StateList;
            if (stateIndex >= stateList.Count)
            {
                throw new Exception($"can't find state index {stateIndex} in controller {controllerData.Name} on panel {name}");
            }

            UIControllerStateData stateData = stateList[stateIndex];
            if (stateData == null)
            {
                throw new Exception($"can't find state index {stateIndex} in controller {controllerData.Name} on panel {name}");
            }

            return stateData;
        }

        private UIControllerStateData FindState(UIControllerData controllerData, string stateName)
        {
            if (controllerData.TryGetStateIndex(stateName, out int stateIndex))
            {
                return FindState(controllerData, stateIndex);
            }

            throw new Exception($"can't find state name {stateName} in controller {controllerData.Name} on panel {name}");
        }

        private void ApplyControllerState(UIControllerData controllerData, UIControllerStateData stateData, bool forceNoAnimation, Action onCompleted)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                _previewAnimationCompletion?.Cancel();
                _previewAnimationCompletion = null;
                KillTweens();
            }
#endif

            StateAnimationCompletion completion = onCompleted == null ? null : new StateAnimationCompletion(onCompleted);
            try
            {
                ApplyControllerTargets(controllerData, stateData, forceNoAnimation, completion);
            }
            catch
            {
                completion?.Cancel();
#if UNITY_EDITOR
                _previewAnimationCompletion?.Cancel();
#endif
                throw;
            }

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                _previewAnimationCompletion?.CompleteOne();
            }
#endif
            // 初始的 1 表示 Tween 尚未全部注册，避免注册过程中有 Tween 被 Kill 时提前触发回调。
            // 例如对于同一个 target 的同一个属性，配置中可能会注册多次，允许注册多次，但是注册第二个时会把之前的 kill 掉。
            // 所有属性处理完后减去这个占位计数；没有 Tween 时也会在此同步触发回调。
            completion?.CompleteOne();
        }

        private void ApplyControllerTargets(UIControllerData controllerData, UIControllerStateData stateData, bool forceNoAnimation, StateAnimationCompletion completion)
        {
            List<UIControllerTargetData> targetList = controllerData.TargetList;
            List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
            for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
            {
                UIControllerTargetData targetData = targetList[targetIndex];
                if (targetData == null)
                {
                    continue;
                }

                List<string> propertyNameList = targetData.PropertyNameList;
                if (propertyNameList.Count == 0)
                {
                    continue;
                }

                if (targetIndex >= targetStateList.Count || targetStateList[targetIndex] == null)
                {
                    throw new Exception($"controller {controllerData.Name} state data is missing target state index {targetIndex} on panel {name}");
                }

                RectTransform rectTransform = targetData.RectTransform;
                if (rectTransform == null)
                {
                    throw new Exception($"controller {controllerData.Name} target index {targetIndex} has no RectTransform on panel {name}");
                }

                ApplyTargetState(controllerData, targetIndex, targetData, targetStateList[targetIndex], forceNoAnimation, completion);
            }
        }

        private void ApplyTargetState(UIControllerData controllerData, int targetIndex, UIControllerTargetData targetData, UIControllerTargetStateData targetStateData, bool forceNoAnimation, StateAnimationCompletion completion)
        {
            List<string> propertyNameList = targetData.PropertyNameList;
            List<UIControllerProperty> propertyList = targetStateData.PropertyList;
            RectTransform rectTransform = targetData.RectTransform;

            for (int propertyIndex = 0; propertyIndex < propertyNameList.Count; propertyIndex++)
            {
                string propertyName = propertyNameList[propertyIndex];
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    continue;
                }

                if (propertyIndex >= propertyList.Count)
                {
                    throw new Exception($"controller {controllerData.Name} target index {targetIndex} is missing property state index {propertyIndex} on panel {name}");
                }

                UIControllerProperty property = propertyList[propertyIndex];
                if (property == null)
                {
                    continue;
                }

                if (property.Name != propertyName)
                {
                    throw new Exception($"controller {controllerData.Name} target index {targetIndex} property index {propertyIndex} expected {propertyName}, got {property.Name} on panel {name}");
                }

                ApplyProperty(property, propertyName, rectTransform, forceNoAnimation, completion);
            }
        }

        private void ApplyProperty(UIControllerProperty property, string propertyName, RectTransform rectTransform, bool forceNoAnimation, StateAnimationCompletion completion)
        {
            KillTween(rectTransform, propertyName);

            if (property.CanAnimate == false || property.NeedAnimate == false || forceNoAnimation || property.AnimationDuration <= 0f)
            {
                property.ApplyTargetValue(rectTransform);
                return;
            }

            Tween tween = null;
            if (property is UIControllerProperty<float> floatProperty)
            {
                tween = CreateFloatTween(floatProperty, rectTransform);
            }
            else if (property is UIControllerProperty<int> intProperty)
            {
                tween = CreateIntTween(intProperty, rectTransform);
            }
            else if (property is UIControllerProperty<Vector2> vector2Property)
            {
                tween = CreateVector2Tween(vector2Property, rectTransform);
            }
            else if (property is UIControllerProperty<Vector3> vector3Property)
            {
                tween = CreateVector3Tween(vector3Property, rectTransform);
            }
            else if (property is UIControllerProperty<Color> colorProperty)
            {
                tween = CreateColorTween(colorProperty, rectTransform);
            }

            if (tween == null)
            {
                property.ApplyTargetValue(rectTransform);
                return;
            }

            ApplyAnimationOptions(property, tween);
            RegisterTween(rectTransform, propertyName, tween, completion);
        }

        private Tween CreateFloatTween(UIControllerProperty<float> property, RectTransform rectTransform)
        {
            float animatedValue = property.GetCurrentValue(rectTransform);
            float targetValue = property.GetTargetValue();
            Tween tween = DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, property.AnimationDuration);

            return tween;
        }

        private Tween CreateIntTween(UIControllerProperty<int> property, RectTransform rectTransform)
        {
            int animatedValue = property.GetCurrentValue(rectTransform);
            int targetValue = property.GetTargetValue();
            Tween tween = DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, property.AnimationDuration);

            return tween;
        }

        private Tween CreateVector2Tween(UIControllerProperty<Vector2> property, RectTransform rectTransform)
        {
            Vector2 animatedValue = property.GetCurrentValue(rectTransform);
            Vector2 targetValue = property.GetTargetValue();
            Tween tween = DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, property.AnimationDuration);

            return tween;
        }

        private Tween CreateVector3Tween(UIControllerProperty<Vector3> property, RectTransform rectTransform)
        {
            Vector3 animatedValue = property.GetCurrentValue(rectTransform);
            Vector3 targetValue = property.GetTargetValue();
            Tween tween = DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, property.AnimationDuration);

            return tween;
        }

        private Tween CreateColorTween(UIControllerProperty<Color> property, RectTransform rectTransform)
        {
            Color animatedValue = property.GetCurrentValue(rectTransform);
            Color targetValue = property.GetTargetValue();
            Tween tween = DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, property.AnimationDuration);

            return tween;
        }

        private void ApplyAnimationOptions(UIControllerProperty property, Tween tween)
        {
            tween.SetUpdate(true);
            AnimationCurve animationCurve = property.AnimationCurve;
            if (property.UseAnimationCurve && animationCurve != null && animationCurve.length > 0)
            {
                tween.SetEase(animationCurve);
            }
            else
            {
                // 旧配置默认使用 Ease，曲线缺失或被清空时也回退到 Ease。
                tween.SetEase(property.AnimationEase);
            }

            if (property.AnimationDelay > 0f)
            {
                tween.SetDelay(property.AnimationDelay);
            }
        }

        private void RegisterTween(RectTransform rectTransform, string propertyName, Tween tween, StateAnimationCompletion completion)
        {
            if (_propertyTweenDict == null)
            {
                _propertyTweenDict = new Dictionary<UIControllerTweenKey, Tween>();
            }

            PreparePreviewTween(tween, completion);
            _propertyTweenDict[new UIControllerTweenKey(rectTransform, propertyName)] = tween;
        }

        private void KillTween(RectTransform rectTransform, string propertyName)
        {
            if (_propertyTweenDict == null)
            {
                return;
            }

            UIControllerTweenKey key = new UIControllerTweenKey(rectTransform, propertyName);
            if (_propertyTweenDict.TryGetValue(key, out Tween tween) == false)
            {
                return;
            }

            // Kill 会同步触发回调，先移除旧记录，避免回调注册的新 Tween 被误删。
            _propertyTweenDict.Remove(key);
            tween?.Kill(false);
        }

        private void KillTweens()
        {
            if (_propertyTweenDict == null)
            {
                return;
            }

            foreach (Tween tween in _propertyTweenDict.Values)
            {
                tween?.Kill(false);
            }

            _propertyTweenDict.Clear();
        }

        private void PreparePreviewTween(Tween tween, StateAnimationCompletion completion)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                _prepareTweenForPreviewMethod?.Invoke(null, new object[] { tween, true, true, true });
                // 预览初始化会清除 Tween 回调，需要在初始化之后注册。
                _previewAnimationCompletion ??= new StateAnimationCompletion(() => PreviewAnimationCompleted?.Invoke());
                Action previewOnCompleted = _previewAnimationCompletion.Register(tween);
                Action stateOnCompleted = completion?.Register(tween);
                // Tween 的 OnKill 会被覆盖，在同一个回调中完成两组计数。
                Action onTweenFinished = () =>
                {
                    previewOnCompleted();
                    stateOnCompleted?.Invoke();
                };
                tween.OnKill(() => onTweenFinished());
                tween.OnComplete(() => onTweenFinished());
                return;
            }
#endif
            Action onCompleted = completion?.Register(tween);
            if (onCompleted != null)
            {
                tween.OnComplete(() => onCompleted());
            }
        }

#if UNITY_EDITOR
        private bool ControllerNeedsLegacyMigration(UIControllerData controllerData)
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

#pragma warning disable CS0618
        private Dictionary<string, RectTransform> BuildLegacyTargetBindingDict(List<string> warningList)
        {
            Dictionary<string, RectTransform> bindingDict = new Dictionary<string, RectTransform>();
            List<UIControllerTargetBinding> bindingList = LegacyControllerTargetBindingList;
            for (int i = 0; i < bindingList.Count; i++)
            {
                UIControllerTargetBinding binding = bindingList[i];
                if (string.IsNullOrWhiteSpace(binding.Name))
                {
                    warningList.Add($"Legacy target binding {i} has no name.");
                    continue;
                }

                if (bindingDict.ContainsKey(binding.Name))
                {
                    warningList.Add($"Legacy target binding name {binding.Name} duplicated. The first binding was kept.");
                    continue;
                }

                if (binding.RectTransform == null)
                {
                    warningList.Add($"Legacy target binding {binding.Name} has no RectTransform.");
                }

                bindingDict.Add(binding.Name, binding.RectTransform);
            }

            return bindingDict;
        }
#pragma warning restore CS0618

        private void MigrateLegacyController(UIControllerData controllerData, int controllerIndex, Dictionary<string, RectTransform> legacyTargetBindingDict, List<string> warningList)
        {
            string controllerLabel = GetControllerMigrationLabel(controllerData, controllerIndex);
            List<string> legacyTargetNameList = CollectLegacyTargetNames(controllerData, controllerLabel, warningList);
            List<UIControllerTargetData> targetList = controllerData.TargetList;
            targetList.Clear();

            for (int targetIndex = 0; targetIndex < legacyTargetNameList.Count; targetIndex++)
            {
                string targetName = legacyTargetNameList[targetIndex];
                UIControllerTargetData targetData = new UIControllerTargetData
                {
                    Name = targetName
                };

                if (legacyTargetBindingDict.TryGetValue(targetName, out RectTransform rectTransform))
                {
                    targetData.RectTransform = rectTransform;
                }
                else
                {
                    warningList.Add($"{controllerLabel}: legacy target {targetName} has no RectTransform binding.");
                }

                CollectLegacyPropertyNames(controllerData, controllerLabel, targetName, targetData.PropertyNameList, warningList);
                targetList.Add(targetData);
            }

            RebuildLegacyStateData(controllerData, controllerLabel, targetList, warningList);
        }

        private List<string> CollectLegacyTargetNames(UIControllerData controllerData, string controllerLabel, List<string> warningList)
        {
            List<string> targetNameList = new List<string>();
            HashSet<string> targetNameSet = new HashSet<string>();
            List<UIControllerStateData> stateList = controllerData.StateList;

            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerStateData stateData = stateList[stateIndex];
                if (stateData == null)
                {
                    warningList.Add($"{controllerLabel} state {stateIndex}: state data is null.");
                    continue;
                }

                HashSet<string> stateTargetNameSet = new HashSet<string>();
                List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
                for (int targetStateIndex = 0; targetStateIndex < targetStateList.Count; targetStateIndex++)
                {
                    UIControllerTargetStateData targetStateData = targetStateList[targetStateIndex];
                    if (targetStateData == null)
                    {
                        warningList.Add($"{controllerLabel} state {stateIndex}: target state {targetStateIndex} is null.");
                        continue;
                    }

                    string targetName = targetStateData.Name;
                    if (string.IsNullOrWhiteSpace(targetName))
                    {
                        warningList.Add($"{controllerLabel} state {stateIndex}: target state {targetStateIndex} has no target name.");
                        continue;
                    }

                    if (stateTargetNameSet.Add(targetName) == false)
                    {
                        warningList.Add($"{controllerLabel} state {stateIndex}: target {targetName} duplicated. The first target was kept.");
                        continue;
                    }

                    if (targetNameSet.Add(targetName))
                    {
                        targetNameList.Add(targetName);
                    }
                }
            }

            return targetNameList;
        }

        private void CollectLegacyPropertyNames(UIControllerData controllerData, string controllerLabel, string targetName, List<string> propertyNameList, List<string> warningList)
        {
            HashSet<string> propertyNameSet = new HashSet<string>();
            List<UIControllerStateData> stateList = controllerData.StateList;
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerStateData stateData = stateList[stateIndex];
                UIControllerTargetStateData targetStateData = stateData == null ? null : FindFirstLegacyTargetState(stateData.TargetStateList, targetName);
                if (targetStateData == null)
                {
                    continue;
                }

                HashSet<string> statePropertyNameSet = new HashSet<string>();
                List<UIControllerProperty> propertyList = targetStateData.PropertyList;
                for (int propertyIndex = 0; propertyIndex < propertyList.Count; propertyIndex++)
                {
                    UIControllerProperty property = propertyList[propertyIndex];
                    if (property == null)
                    {
                        warningList.Add($"{controllerLabel} state {stateIndex} target {targetName}: property {propertyIndex} is missing or has an unsupported SerializeReference type.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(property.Name))
                    {
                        warningList.Add($"{controllerLabel} state {stateIndex} target {targetName}: property {propertyIndex} has no name.");
                        continue;
                    }

                    if (statePropertyNameSet.Add(property.Name) == false)
                    {
                        warningList.Add($"{controllerLabel} state {stateIndex} target {targetName}: property {property.Name} duplicated. The first property was kept.");
                        continue;
                    }

                    if (propertyNameSet.Add(property.Name))
                    {
                        propertyNameList.Add(property.Name);
                    }
                }
            }
        }

        private void RebuildLegacyStateData(UIControllerData controllerData, string controllerLabel, List<UIControllerTargetData> targetList, List<string> warningList)
        {
            List<UIControllerStateData> stateList = controllerData.StateList;
            List<List<UIControllerTargetStateData>> legacyTargetStateLists = new List<List<UIControllerTargetStateData>>(stateList.Count);
            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerStateData stateData = stateList[stateIndex];
                legacyTargetStateLists.Add(stateData == null ? new List<UIControllerTargetStateData>() : new List<UIControllerTargetStateData>(stateData.TargetStateList));
            }

            for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
            {
                UIControllerStateData stateData = stateList[stateIndex];
                if (stateData == null)
                {
                    stateData = new UIControllerStateData();
                    stateList[stateIndex] = stateData;
                }

                List<UIControllerTargetStateData> targetStateList = stateData.TargetStateList;
                targetStateList.Clear();

                for (int targetIndex = 0; targetIndex < targetList.Count; targetIndex++)
                {
                    UIControllerTargetData targetData = targetList[targetIndex];
                    string targetName = targetData.Name;
                    UIControllerTargetStateData legacyTargetStateData = FindFirstLegacyTargetState(legacyTargetStateLists[stateIndex], targetName);
                    if (legacyTargetStateData == null && targetData.PropertyNameList.Count > 0)
                    {
                        warningList.Add($"{controllerLabel} state {stateIndex}: target {targetName} was missing. Default property data was created.");
                    }

                    Dictionary<string, UIControllerProperty> legacyPropertyDict = BuildLegacyPropertyDict(legacyTargetStateData);
                    UIControllerTargetStateData newTargetStateData = new UIControllerTargetStateData();
                    List<UIControllerProperty> newPropertyList = newTargetStateData.PropertyList;

                    for (int propertyIndex = 0; propertyIndex < targetData.PropertyNameList.Count; propertyIndex++)
                    {
                        string propertyName = targetData.PropertyNameList[propertyIndex];
                        if (legacyPropertyDict != null && legacyPropertyDict.TryGetValue(propertyName, out UIControllerProperty legacyProperty))
                        {
                            newPropertyList.Add(legacyProperty);
                            continue;
                        }

                        warningList.Add($"{controllerLabel} state {stateIndex} target {targetName}: property {propertyName} was missing. Default data was created.");
                        newPropertyList.Add(CreateDefaultProperty(propertyName, targetData.RectTransform, $"{controllerLabel} state {stateIndex} target {targetName}", warningList));
                    }

                    targetStateList.Add(newTargetStateData);
                }
            }
        }

        private static UIControllerTargetStateData FindFirstLegacyTargetState(List<UIControllerTargetStateData> targetStateList, string targetName)
        {
            for (int i = 0; i < targetStateList.Count; i++)
            {
                UIControllerTargetStateData targetStateData = targetStateList[i];
                if (targetStateData != null && targetStateData.Name == targetName)
                {
                    return targetStateData;
                }
            }

            return null;
        }

        private static Dictionary<string, UIControllerProperty> BuildLegacyPropertyDict(UIControllerTargetStateData targetStateData)
        {
            if (targetStateData == null)
            {
                return null;
            }

            Dictionary<string, UIControllerProperty> propertyDict = new Dictionary<string, UIControllerProperty>();
            List<UIControllerProperty> propertyList = targetStateData.PropertyList;
            for (int i = 0; i < propertyList.Count; i++)
            {
                UIControllerProperty property = propertyList[i];
                if (property == null || string.IsNullOrWhiteSpace(property.Name) || propertyDict.ContainsKey(property.Name))
                {
                    continue;
                }

                propertyDict.Add(property.Name, property);
            }

            return propertyDict;
        }

        private static UIControllerProperty CreateDefaultProperty(string propertyName, RectTransform rectTransform, string context, List<string> warningList)
        {
            UIControllerProperty property = UIControllerPropertyFactory.Create(propertyName);
            if (property == null)
            {
                warningList.Add($"{context}: could not create default property {propertyName}.");
                return null;
            }

            CapturePropertyIfValid(property, rectTransform);
            return property;
        }

        private static void CapturePropertyIfValid(UIControllerProperty property, RectTransform rectTransform)
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

        private static string GetControllerMigrationLabel(UIControllerData controllerData, int controllerIndex)
        {
            return string.IsNullOrWhiteSpace(controllerData.Name) ? $"controller {controllerIndex}" : $"controller {controllerData.Name}";
        }
#endif
        #endregion
    }
}
