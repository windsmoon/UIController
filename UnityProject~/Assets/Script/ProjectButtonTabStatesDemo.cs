using UnityEngine;
using UnityEngine.UI;
using Windsmoon.UIController;

public sealed class ProjectButtonTabStatesDemo : MonoBehaviour
{
    private const string ButtonControllerName = "ProjectButton";
    private const string TabControllerName = "ProjectTab";

    private UIControllerPanel _panel;
    private Button _projectButton;
    private Button _projectTab;
    private Button _normalButton;
    private Button _selectedButton;
    private Button _disabledButton;
    private int _buttonStateIndex;
    private int _tabStateIndex;

    private void Awake()
    {
        _panel = GetComponent<UIControllerPanel>();
        _projectButton = transform.Find("ProjectButton").GetComponent<Button>();
        _projectTab = transform.Find("ProjectTab").GetComponent<Button>();
        _normalButton = transform.Find("State_Normal").GetComponent<Button>();
        _selectedButton = transform.Find("State_Selected").GetComponent<Button>();
        _disabledButton = transform.Find("State_Disabled").GetComponent<Button>();

        _projectButton.onClick.AddListener(AdvanceButtonState);
        _projectTab.onClick.AddListener(AdvanceTabState);
        _normalButton.onClick.AddListener(ShowNormal);
        _selectedButton.onClick.AddListener(ShowSelected);
        _disabledButton.onClick.AddListener(ShowDisabled);
        ShowNormal(true);
    }

    private void OnDestroy()
    {
        _projectButton.onClick.RemoveListener(AdvanceButtonState);
        _projectTab.onClick.RemoveListener(AdvanceTabState);
        _normalButton.onClick.RemoveListener(ShowNormal);
        _selectedButton.onClick.RemoveListener(ShowSelected);
        _disabledButton.onClick.RemoveListener(ShowDisabled);
    }

    private void AdvanceButtonState()
    {
        _buttonStateIndex = (_buttonStateIndex + 1) % 3;
        _panel.SetControllerState(ButtonControllerName, _buttonStateIndex);
    }

    private void AdvanceTabState()
    {
        _tabStateIndex = (_tabStateIndex + 1) % 3;
        _panel.SetControllerState(TabControllerName, _tabStateIndex);
    }

    private void ShowNormal()
    {
        ShowNormal(false);
    }

    private void ShowNormal(bool forceNoAnimation)
    {
        SetBothStates("Normal", 0, forceNoAnimation);
    }

    private void ShowSelected()
    {
        SetBothStates("Selected", 1);
    }

    private void ShowDisabled()
    {
        SetBothStates("Disabled", 2);
    }

    private void SetBothStates(string stateName, int stateIndex, bool forceNoAnimation = false)
    {
        _buttonStateIndex = stateIndex;
        _tabStateIndex = stateIndex;
        _panel.SetControllerState(ButtonControllerName, stateName, forceNoAnimation);
        _panel.SetControllerState(TabControllerName, stateName, forceNoAnimation);
    }
}
