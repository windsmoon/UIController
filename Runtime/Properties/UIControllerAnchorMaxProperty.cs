using System;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerAnchorMaxProperty : UIControllerProperty<Vector2>
    {
        #region fields
        public const string PropertyName = "AnchorMax";
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
            _value = rectTransform.anchorMax;
        }

        public override Vector2 GetCurrentValue(RectTransform rectTransform)
        {
            return rectTransform.anchorMax;
        }

        public override void SetCurrentValue(RectTransform rectTransform, Vector2 value)
        {
            rectTransform.anchorMax = value;
        }
        #endregion
    }
}
