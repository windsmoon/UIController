using System;
using UnityEngine;
using UnityEngine.UI;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerImageFillAmountProperty : UIControllerProperty<float>
    {
        #region fields
        public const string PropertyName = "ImageFillAmount";
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
                _value = image.fillAmount;
            }
        }

        public override float GetCurrentValue(RectTransform rectTransform)
        {
            Image image = GetImage(rectTransform);
            return image != null ? image.fillAmount : _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, float value)
        {
            Image image = GetImage(rectTransform);
            if (image != null)
            {
                image.fillAmount = value;
            }
        }

        private static Image GetImage(RectTransform rectTransform)
        {
            return rectTransform.GetComponent<Image>();
        }
        #endregion
    }
}
