using System;
using UnityEngine;

namespace Windsmoon.UIStateController.Properties
{
    [Serializable]
    public class UIControllerLocalScaleProperty : UIControllerProperty<Vector3>
    {
        #region fields
        public const string PropertyName = "LocalScale";
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
            _value = rectTransform.localScale;
        }

        public override Vector3 GetCurrentValue(RectTransform rectTransform)
        {
            return rectTransform.localScale;
        }

        public override Vector3 GetTargetValue()
        {
            return _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, Vector3 value)
        {
            rectTransform.localScale = value;
        }

        public override string GetValueText()
        {
            return _value.ToString();
        }
        #endregion
    }
}
