using System;
using UnityEngine;
using UnityEngine.UI;

namespace Framework.UI.Controller.Properties
{
    [Serializable]
    public class UIControllerImageColorProperty : UIControllerProperty<Color>
    {
        #region fields
        public const string PropertyName = "ImageColor";
        #endregion

        #region properties
        public override string Name => PropertyName;
        public override bool CanAnimate => true;
        #endregion

        #region methods
        public override bool IsValid(RectTransform rectTransform, out string errorMessage)
        {
            if (GetImage(rectTransform) != null)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = "Target has no Image component.";
            return false;
        }

        public override void Capture(RectTransform rectTransform)
        {
            Image image = GetImage(rectTransform);
            if (image != null)
            {
                _value = image.color;
            }
        }

        public override Color GetCurrentValue(RectTransform rectTransform)
        {
            Image image = GetImage(rectTransform);
            return image != null ? image.color : _value;
        }

        public override Color GetTargetValue()
        {
            return _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, Color value)
        {
            Image image = GetImage(rectTransform);
            if (image != null)
            {
                image.color = value;
            }
        }

        public override string GetValueText()
        {
            return _value.ToString();
        }

        private static Image GetImage(RectTransform rectTransform)
        {
            return rectTransform != null ? rectTransform.GetComponent<Image>() : null;
        }
        #endregion
    }
}
