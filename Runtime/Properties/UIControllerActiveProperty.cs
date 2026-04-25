using System;
using UnityEngine;

namespace Windsmoon.UIStateController.Properties
{
    [Serializable]
    public class UIControllerActiveProperty : UIControllerProperty<bool>
    {
        #region fields
        public const string PropertyName = "Active";
        #endregion

        #region properties
        public override string Name => PropertyName;
        #endregion

        #region methods
        public override bool IsValid(RectTransform rectTransform, out string errorMessage)
        {
            errorMessage = null;
            return rectTransform != null;
        }

        public override void Capture(RectTransform rectTransform)
        {
            _value = rectTransform.gameObject.activeSelf;
        }

        public override bool GetCurrentValue(RectTransform rectTransform)
        {
            return rectTransform.gameObject.activeSelf;
        }

        public override bool GetTargetValue()
        {
            return _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, bool value)
        {
            rectTransform.gameObject.SetActive(value);
        }

        public override string GetValueText()
        {
            return _value ? "True" : "False";
        }
        #endregion
    }
}
