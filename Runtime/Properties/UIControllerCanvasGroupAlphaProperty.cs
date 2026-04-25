using System;
using UnityEngine;

namespace Framework.UI.Controller.Properties
{
    [Serializable]
    public class UIControllerCanvasGroupAlphaProperty : UIControllerProperty<float>
    {
        #region fields
        public const string PropertyName = "CanvasGroupAlpha";
        #endregion

        #region properties
        public override string Name => PropertyName;
        public override bool CanAnimate => true;
        #endregion

        #region methods
        public override bool IsValid(RectTransform rectTransform, out string errorMessage)
        {
            if (GetCanvasGroup(rectTransform) != null)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = "Target has no CanvasGroup component.";
            return false;
        }

        public override void Capture(RectTransform rectTransform)
        {
            CanvasGroup canvasGroup = GetCanvasGroup(rectTransform);
            if (canvasGroup != null)
            {
                _value = canvasGroup.alpha;
            }
        }

        public override float GetCurrentValue(RectTransform rectTransform)
        {
            CanvasGroup canvasGroup = GetCanvasGroup(rectTransform);
            return canvasGroup != null ? canvasGroup.alpha : _value;
        }

        public override float GetTargetValue()
        {
            return _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, float value)
        {
            CanvasGroup canvasGroup = GetCanvasGroup(rectTransform);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = value;
            }
        }

        public override string GetValueText()
        {
            return _value.ToString("0.###");
        }

        private static CanvasGroup GetCanvasGroup(RectTransform rectTransform)
        {
            return rectTransform != null ? rectTransform.GetComponent<CanvasGroup>() : null;
        }
        #endregion
    }
}
