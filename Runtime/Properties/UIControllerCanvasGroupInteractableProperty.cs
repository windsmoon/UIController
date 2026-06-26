using System;
using UnityEngine;

namespace Windsmoon.UIController.Properties
{
    [Serializable]
    public class UIControllerCanvasGroupInteractableProperty : UIControllerProperty<bool>
    {
        #region fields
        public const string PropertyName = "CanvasGroupInteractable";
        #endregion

        #region properties
        public override string Name => PropertyName;
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
                _value = canvasGroup.interactable;
            }
        }

        public override bool GetCurrentValue(RectTransform rectTransform)
        {
            CanvasGroup canvasGroup = GetCanvasGroup(rectTransform);
            return canvasGroup != null ? canvasGroup.interactable : _value;
        }

        public override void SetCurrentValue(RectTransform rectTransform, bool value)
        {
            CanvasGroup canvasGroup = GetCanvasGroup(rectTransform);
            if (canvasGroup != null)
            {
                canvasGroup.interactable = value;
            }
        }

        private static CanvasGroup GetCanvasGroup(RectTransform rectTransform)
        {
            return rectTransform.GetComponent<CanvasGroup>();
        }
        #endregion
    }
}
