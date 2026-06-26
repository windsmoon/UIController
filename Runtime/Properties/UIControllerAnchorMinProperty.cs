using System;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerAnchorMinProperty : UIControllerProperty<Vector2>
    {
        #region fields
        public const string PropertyName = "AnchorMin";
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
            _value = rectTransform.anchorMin;
        }

        public override Vector2 GetCurrentValue(RectTransform rectTransform)
        {
            return rectTransform.anchorMin;
        }

        public override void SetCurrentValue(RectTransform rectTransform, Vector2 value)
        {
            rectTransform.anchorMin = value;
        }
        #endregion
    }
}
