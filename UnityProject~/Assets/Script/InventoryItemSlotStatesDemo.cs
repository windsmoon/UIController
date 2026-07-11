using UnityEngine;
using UnityEngine.UI;
using Windsmoon.UIController;

public sealed class InventoryItemSlotStatesDemo : MonoBehaviour
{
    private const string ControllerName = "InventoryItemSlot";

    private UIControllerPanel _panel;
    private Button _itemSlot;
    private Button _unselectedButton;
    private Button _selectedButton;
    private Button _lockedButton;
    private Button _obtainedButton;
    private bool _isSelected;

    private void Awake()
    {
        _panel = GetComponent<UIControllerPanel>();
        _itemSlot = transform.Find("InventoryItemSlot").GetComponent<Button>();
        _unselectedButton = transform.Find("State_Unselected").GetComponent<Button>();
        _selectedButton = transform.Find("State_Selected").GetComponent<Button>();
        _lockedButton = transform.Find("State_Locked").GetComponent<Button>();
        _obtainedButton = transform.Find("State_Obtained").GetComponent<Button>();

        _itemSlot.onClick.AddListener(ToggleSelection);
        _unselectedButton.onClick.AddListener(ShowUnselected);
        _selectedButton.onClick.AddListener(ShowSelected);
        _lockedButton.onClick.AddListener(ShowLocked);
        _obtainedButton.onClick.AddListener(ShowObtained);
        ShowUnselected(true);
    }

    private void OnDestroy()
    {
        _itemSlot.onClick.RemoveListener(ToggleSelection);
        _unselectedButton.onClick.RemoveListener(ShowUnselected);
        _selectedButton.onClick.RemoveListener(ShowSelected);
        _lockedButton.onClick.RemoveListener(ShowLocked);
        _obtainedButton.onClick.RemoveListener(ShowObtained);
    }

    private void ToggleSelection()
    {
        if (_isSelected)
        {
            ShowUnselected();
            return;
        }

        ShowSelected();
    }

    private void ShowUnselected()
    {
        ShowUnselected(false);
    }

    private void ShowUnselected(bool forceNoAnimation)
    {
        _isSelected = false;
        _panel.SetControllerState(ControllerName, "Unselected", forceNoAnimation);
    }

    private void ShowSelected()
    {
        _isSelected = true;
        _panel.SetControllerState(ControllerName, "Selected");
    }

    private void ShowLocked()
    {
        _isSelected = false;
        _panel.SetControllerState(ControllerName, "Locked");
    }

    private void ShowObtained()
    {
        _isSelected = false;
        _panel.SetControllerState(ControllerName, "Obtained");
    }
}
