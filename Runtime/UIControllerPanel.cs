using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using Framework.UI.Controller.Properties;
using UnityEngine;

namespace Framework.UI.Controller
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
        private const float StateAnimationDuration = 0.25f;
        [SerializeField]
        private List<UIControllerTargetBinding> _controllerTargetBindingList = new List<UIControllerTargetBinding>();
        [SerializeField]
        private List<UIControllerData> _controllerList = new List<UIControllerData>();
        private Dictionary<string, UIControllerData> _controllerDict;
        private Dictionary<string, RectTransform> _controllerTargetDict;
        private Dictionary<UIControllerTweenKey, Tween> _propertyTweenDict;
#if UNITY_EDITOR
        public event Action PreviewAnimationCompleted;
        private static readonly MethodInfo _prepareTweenForPreviewMethod = Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview, DOTweenEditor")?.GetMethod("PrepareTweenForPreview", BindingFlags.Public | BindingFlags.Static);
        private int _pendingPreviewAnimationCount;
#endif
        #endregion

        #region properties
        public List<UIControllerTargetBinding> ControllerTargetBindingList => _controllerTargetBindingList;
        public List<UIControllerData> ControllerList => _controllerList;
        #endregion

        #region interface impls
        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            DeserializeControllerDict();
            DeserializeControllerTargetDict(false);
            DeserializeStateDicts();
        }
        #endregion

        #region methods
        public void SetControllerState(string controllerName, int stateIndex, bool forceNoAnimation = false)
        {
            if (_controllerDict == null || _controllerTargetDict == null)
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
            ApplyControllerState(stateData, forceNoAnimation);
        }

        private void DeserializeControllerDict()
        {
            if (_controllerList == null || _controllerList.Count == 0)
            {
                _controllerDict = null;
                return;
            }

            _controllerDict = new Dictionary<string, UIControllerData>(_controllerList.Count);
            foreach (UIControllerData controllerData in _controllerList)
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

        private void DeserializeControllerTargetDict(bool throwOnInvalid)
        {
            if (_controllerDict == null || _controllerDict.Count == 0)
            {
                _controllerTargetDict = null;
                return;
            }

            _controllerTargetDict = new Dictionary<string, RectTransform>(_controllerTargetBindingList.Count);
            foreach (UIControllerTargetBinding binding in _controllerTargetBindingList)
            {
                bool hasTargetName = string.IsNullOrWhiteSpace(binding.Name) == false;
                if (hasTargetName == false)
                {
                    if (throwOnInvalid)
                    {
                        throw new Exception("controller target binding has no name");
                    }

                    continue;
                }

                bool hasRectTransform = binding.RectTransform;
                if (hasRectTransform == false)
                {
                    if (throwOnInvalid)
                    {
                        throw new Exception($"controller target binding {binding.Name} has no RectTransform");
                    }

                    continue;
                }

                if (_controllerTargetDict.ContainsKey(binding.Name))
                {
                    if (throwOnInvalid)
                    {
                        throw new Exception($"controller target binding name {binding.Name} duplicated");
                    }

                    continue;
                }

                _controllerTargetDict.Add(binding.Name, binding.RectTransform);
            }
        }

        private void DeserializeStateDicts()
        {
            if (_controllerList == null)
            {
                return;
            }

            for (int i = 0; i < _controllerList.Count; i++)
            {
                UIControllerData controllerData = _controllerList[i];
                if (controllerData == null)
                {
                    continue;
                }

                List<UIControllerStateData> stateList = controllerData.StateList;
                if (stateList == null)
                {
                    continue;
                }

                for (int stateIndex = 0; stateIndex < stateList.Count; stateIndex++)
                {
                    UIControllerStateData stateData = stateList[stateIndex];
                    stateData?.RebuildCache();
                }
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

        private void ApplyControllerState(UIControllerStateData stateData, bool forceNoAnimation)
        {
            KillTweens();

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                _pendingPreviewAnimationCount = 0;
            }
#endif

            foreach (UIControllerTargetStateData targetStateData in stateData.TargetStateDict.Values)
            {
                ApplyTargetState(targetStateData, forceNoAnimation);
            }
        }

        private void ApplyTargetState(UIControllerTargetStateData targetStateData, bool forceNoAnimation)
        {
            if (targetStateData == null)
            {
                return;
            }

            if (targetStateData.PropertyDict.Count == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetStateData.Name))
            {
                return;
            }

            RectTransform rectTransform = GetControllerTargetRectTransform(targetStateData.Name);
            foreach (UIControllerProperty property in targetStateData.PropertyDict.Values)
            {
                ApplyProperty(property, rectTransform, forceNoAnimation);
            }
        }

        private RectTransform GetControllerTargetRectTransform(string targetName)
        {
            if (_controllerTargetDict.TryGetValue(targetName, out RectTransform rectTransform))
            {
                return rectTransform;
            }

            throw new Exception($"can't find controller target binding {targetName} on panel {name}");
        }

        private void ApplyProperty(UIControllerProperty property, RectTransform rectTransform, bool forceNoAnimation)
        {
            if (property.CanAnimate == false || property.NeedAnimate == false || forceNoAnimation)
            {
                property.ApplyTargetValue(rectTransform);
                return;
            }

            Tween tween = null;
            if (property is UIControllerProperty<float> floatProperty)
            {
                tween = CreateFloatTween(floatProperty, rectTransform);
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

            RegisterTween(rectTransform, property.Name, tween);
        }

        private Tween CreateFloatTween(UIControllerProperty<float> property, RectTransform rectTransform)
        {
            float animatedValue = property.GetCurrentValue(rectTransform);
            float targetValue = property.GetTargetValue();
            return DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, StateAnimationDuration).SetEase(Ease.OutCubic);
        }

        private Tween CreateVector2Tween(UIControllerProperty<Vector2> property, RectTransform rectTransform)
        {
            Vector2 animatedValue = property.GetCurrentValue(rectTransform);
            Vector2 targetValue = property.GetTargetValue();
            return DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, StateAnimationDuration).SetEase(Ease.OutCubic);
        }

        private Tween CreateVector3Tween(UIControllerProperty<Vector3> property, RectTransform rectTransform)
        {
            Vector3 animatedValue = property.GetCurrentValue(rectTransform);
            Vector3 targetValue = property.GetTargetValue();
            return DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, StateAnimationDuration).SetEase(Ease.OutCubic);
        }

        private Tween CreateColorTween(UIControllerProperty<Color> property, RectTransform rectTransform)
        {
            Color animatedValue = property.GetCurrentValue(rectTransform);
            Color targetValue = property.GetTargetValue();
            return DOTween.To(() => animatedValue, value =>
            {
                animatedValue = value;
                property.SetCurrentValue(rectTransform, value);
            }, targetValue, StateAnimationDuration).SetEase(Ease.OutCubic);
        }

        private void RegisterTween(RectTransform rectTransform, string propertyName, Tween tween)
        {
            if (_propertyTweenDict == null)
            {
                _propertyTweenDict = new Dictionary<UIControllerTweenKey, Tween>();
            }

            PreparePreviewTween(tween);
            _propertyTweenDict[new UIControllerTweenKey(rectTransform, propertyName)] = tween;
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

        private void PreparePreviewTween(Tween tween)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                _pendingPreviewAnimationCount++;
                _prepareTweenForPreviewMethod?.Invoke(null, new object[] { tween, true, true, true });
                tween.OnComplete(() =>
                {
                    _pendingPreviewAnimationCount--;
                    if (_pendingPreviewAnimationCount == 0)
                    {
                        PreviewAnimationCompleted?.Invoke();
                    }
                });
            }
#endif
        }
        #endregion
    }
}
