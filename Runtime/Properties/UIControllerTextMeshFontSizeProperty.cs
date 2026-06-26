using System;
using TMPro;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerTextMeshFontSizeProperty : UIControllerProperty<float>
    {
        #region fields
        public const string PropertyName = "TextMeshFontSize";
        #endregion

        #region properties
        public override string Name => PropertyName;
        public override bool CanAnimate => true;
        #endregion

        #region methods
        public override bool IsValid(RectTransform rectTransform, out string errorMessage)
        {
            if (GetTextMesh(rectTransform) != null)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = "Target has no TextMeshProUGUI component.";
            return false;
        }

        public override void Capture(RectTransform rectTransform)
        {
            TextMeshProUGUI textMesh = GetTextMesh(rectTransform);
            if (textMesh != null)
            {
                _value = textMesh.fontSize;
            }
        }

        public override float GetCurrentValue(RectTransform rectTransform)
        {
            TextMeshProUGUI textMesh = GetTextMesh(rectTransform);
            return textMesh != null ? textMesh.fontSize : _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, float value)
        {
            TextMeshProUGUI textMesh = GetTextMesh(rectTransform);
            if (textMesh != null)
            {
                textMesh.fontSize = value;
            }
        }

        private static TextMeshProUGUI GetTextMesh(RectTransform rectTransform)
        {
            return rectTransform.GetComponent<TextMeshProUGUI>();
        }
        #endregion
    }
}
