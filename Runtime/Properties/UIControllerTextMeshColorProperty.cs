using System;
using TMPro;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerTextMeshColorProperty : UIControllerProperty<Color>
    {
        #region fields
        public const string PropertyName = "TextMeshColor";
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
                _value = textMesh.color;
            }
        }

        public override Color GetCurrentValue(RectTransform rectTransform)
        {
            TextMeshProUGUI textMesh = GetTextMesh(rectTransform);
            return textMesh != null ? textMesh.color : _value;
        }

        public override Color GetTargetValue()
        {
            return _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, Color value)
        {
            TextMeshProUGUI textMesh = GetTextMesh(rectTransform);
            if (textMesh != null)
            {
                textMesh.color = value;
            }
        }

        public override string GetValueText()
        {
            return _value.ToString();
        }

        private static TextMeshProUGUI GetTextMesh(RectTransform rectTransform)
        {
            return rectTransform.GetComponent<TextMeshProUGUI>();
        }
        #endregion
    }
}
