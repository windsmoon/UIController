using System;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public abstract class UIControllerProperty
    {
        #region fields
        [SerializeField]
        private bool _needAnimation;
        #endregion

        #region properties
        public abstract string Name { get; }
        public virtual bool CanAnimate => false;
        public bool NeedAnimate
        {
            get => CanAnimate && _needAnimation;
            set => _needAnimation = CanAnimate && value;
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
