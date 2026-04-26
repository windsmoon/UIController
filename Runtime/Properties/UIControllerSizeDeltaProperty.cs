using System;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerSizeDeltaProperty : UIControllerProperty<Vector2>
    {
        #region fields
        public const string PropertyName = "SizeDelta";
        #endregion

        #region properties
        public override string Name => PropertyName;
        public override bool CanAnimate => true;
        #endregion

        #region methods
        public override bool IsValid(RectTransform rectTransform, out string errorMessage)
        {
            errorMessage = null;
            return true;
        }

        public override void Capture(RectTransform rectTransform)
        {
            _value = rectTransform.sizeDelta;
        }

        public override Vector2 GetCurrentValue(RectTransform rectTransform)
        {
            return rectTransform.sizeDelta;
        }

        public override Vector2 GetTargetValue()
        {
            return _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, Vector2 value)
        {
            rectTransform.sizeDelta = value;
        }

        public override string GetValueText()
        {
            return _value.ToString();
        }
        #endregion
    }
}
