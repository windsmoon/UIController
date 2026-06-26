using System;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerLocalRotationProperty : UIControllerProperty<Vector3>
    {
        #region fields
        public const string PropertyName = "LocalRotation";
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
            _value = rectTransform.localEulerAngles;
        }

        public override Vector3 GetCurrentValue(RectTransform rectTransform)
        {
            return rectTransform.localEulerAngles;
        }

        public override void SetCurrentValue(RectTransform rectTransform, Vector3 value)
        {
            rectTransform.localEulerAngles = value;
        }
        #endregion
    }
}
