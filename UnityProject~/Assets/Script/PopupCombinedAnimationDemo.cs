using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Windsmoon.UIController;

public sealed class PopupCombinedAnimationDemo : MonoBehaviour
{
    private const string SceneName = "PopupCombinedAnimation";
    private const string PopupControllerName = "PopupWindow";

    private static readonly Color ToggleOpenColor = new Color(0.08f, 0.31f, 0.25f, 1f);
    private static readonly Color ToggleCloseColor = new Color(0.42f, 0.16f, 0.08f, 1f);

    private UIControllerPanel _panel;
    private Button _popupButton;
    private Button _toggleButton;
    private TMP_Text _toggleLabel;
    private Image _toggleImage;
    private CanvasGroup _popupCanvasGroup;
    private bool _isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        UIControllerPanel panel = Object.FindFirstObjectByType<UIControllerPanel>();
        if (panel != null && panel.GetComponent<PopupCombinedAnimationDemo>() == null)
        {
            panel.gameObject.AddComponent<PopupCombinedAnimationDemo>();
        }
    }

    private void Awake()
    {
        _panel = GetComponent<UIControllerPanel>();
        _popupButton = transform.Find("PopupWindow").GetComponent<Button>();
        _toggleButton = transform.Find("PopupToggle").GetComponent<Button>();
        _toggleLabel = _toggleButton.GetComponentInChildren<TMP_Text>();
        _toggleImage = _toggleButton.GetComponent<Image>();
        _popupCanvasGroup = _popupButton.GetComponent<CanvasGroup>();

        Outline popupOutline = _popupButton.gameObject.AddComponent<Outline>();
        popupOutline.effectColor = new Color(0.58f, 0.36f, 0.12f, 1f);
        popupOutline.effectDistance = new Vector2(4f, -4f);

        Outline toggleOutline = _toggleButton.gameObject.AddComponent<Outline>();
        toggleOutline.effectColor = new Color(0.43f, 0.32f, 0.18f, 1f);
        toggleOutline.effectDistance = new Vector2(2f, -2f);

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
        _toggleLabel.text = isOpen ? "CLOSE BRIEFING" : "OPEN BRIEFING";
        _toggleImage.color = isOpen ? ToggleCloseColor : ToggleOpenColor;
    }
}
