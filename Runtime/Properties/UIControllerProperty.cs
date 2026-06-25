using System;
using DG.Tweening;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public abstract class UIControllerProperty
    {
        #region fields
        public const float DefaultAnimationDuration = 0.25f;
        public const Ease DefaultAnimationEase = Ease.OutCubic;
        private const float MinAnimationDuration = 0f;
        private const float MinAnimationDelay = 0f;

        [SerializeField]
        private bool _needAnimation;
        [SerializeField]
        private Ease _animationEase = DefaultAnimationEase;
        [SerializeField]
        private float _animationDuration = DefaultAnimationDuration;
        [SerializeField]
        private float _animationDelay;
        #endregion

        #region properties
        public abstract string Name { get; }
        public virtual bool CanAnimate => false;
        public bool NeedAnimate
        {
            get => CanAnimate && _needAnimation;
            set => _needAnimation = CanAnimate && value;
        }
        public Ease AnimationEase
        {
            get => _animationEase == Ease.Unset ? DefaultAnimationEase : _animationEase;
            set => _animationEase = value;
        }
        public float AnimationDuration
        {
            get => Mathf.Max(MinAnimationDuration, _animationDuration);
            set => _animationDuration = Mathf.Max(MinAnimationDuration, value);
        }
        public float AnimationDelay
        {
            get => Mathf.Max(MinAnimationDelay, _animationDelay);
            set => _animationDelay = Mathf.Max(MinAnimationDelay, value);
        }
        #endregion

        #region methods
        public abstract bool IsValid(RectTransform rectTransform, out string errorMessage);
        public abstract void Capture(RectTransform rectTransform);
        public abstract void ApplyTargetValue(RectTransform rectTransform);
        public abstract string GetValueText();
        #endregion
    }

    [Serializable]
    public abstract class UIControllerProperty<T> : UIControllerProperty
    {
        #region fields
        [SerializeField]
        protected T _value;
        #endregion

        #region methods
        public abstract T GetCurrentValue(RectTransform rectTransform);
        public abstract T GetTargetValue();
        public abstract void SetCurrentValue(RectTransform rectTransform, T value);

        public void SetTargetValue(T value)
        {
            _value = value;
        }
        
        public sealed override void ApplyTargetValue(RectTransform rectTransform)
        {
            SetCurrentValue(rectTransform, GetTargetValue());
        }
        #endregion
    }
}
