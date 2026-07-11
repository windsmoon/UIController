using UnityEngine;
using UnityEngine.UI;
using Windsmoon.UIController;

public sealed class PopupCombinedAnimationDemo : MonoBehaviour
{
    private const string PopupControllerName = "PopupWindow";

    private UIControllerPanel _panel;
    private Button _popupButton;
    private Button _toggleButton;
    private CanvasGroup _popupCanvasGroup;
    private bool _isOpen;

    private void Awake()
    {
        _panel = GetComponent<UIControllerPanel>();
        _popupButton = transform.Find("PopupWindow").GetComponent<Button>();
        _toggleButton = transform.Find("PopupToggle").GetComponent<Button>();
        _popupCanvasGroup = _popupButton.GetComponent<CanvasGroup>();

        _popupButton.onClick.AddListener(ClosePopup);
        _toggleButton.onClick.AddListener(TogglePopup);
        SetPopupOpen(false, true);
    }

    private void OnDestroy()
    {
        _popupButton.onClick.RemoveListener(ClosePopup);
        _toggleButton.onClick.RemoveListener(TogglePopup);
    }

    private void TogglePopup()
    {
        SetPopupOpen(_isOpen == false);
    }

    private void ClosePopup()
    {
        SetPopupOpen(false);
    }

    private void SetPopupOpen(bool isOpen, bool forceNoAnimation = false)
    {
        _isOpen = isOpen;
        _popupButton.interactable = isOpen;
        _popupCanvasGroup.blocksRaycasts = isOpen;
        _panel.SetControllerState(PopupControllerName, isOpen ? "Open" : "Closed", forceNoAnimation);
    }
}
