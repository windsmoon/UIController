using System;
using TMPro;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerTextForTextMeshProperty : UIControllerProperty<string>
    {
        #region fields
        public const string PropertyName = "TextForTextMesh";
        #endregion

        #region properties
        public override string Name => PropertyName;
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
            _value = GetTextMesh(rectTransform)?.text ?? string.Empty;
        }

        public override string GetCurrentValue(RectTransform rectTransform)
        {
            return GetTextMesh(rectTransform)?.text ?? string.Empty;
        }

        public override string GetTargetValue()
        {
            return _value ?? string.Empty;
        }

        public override void SetCurrentValue(RectTransform rectTransform, string value)
        {
            TextMeshProUGUI textMesh = GetTextMesh(rectTransform);
            if (textMesh != null)
            {
                textMesh.text = value ?? string.Empty;
            }
        }

        public override string GetValueText()
        {
            return _value ?? string.Empty;
        }

        private static TextMeshProUGUI GetTextMesh(RectTransform rectTransform)
        {
            return rectTransform.GetComponent<TextMeshProUGUI>();
        }
        #endregion
    }
}
