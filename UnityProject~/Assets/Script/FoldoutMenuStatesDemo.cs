using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Windsmoon.UIController;

public static class FoldoutMenuStatesDemo
{
    private const string SceneName = "FoldoutMenuStates";
    private const string ControllerName = "FoldoutMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindToggleEvents()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        UIControllerPanel panel = Object.FindFirstObjectByType<UIControllerPanel>();
        Button menuButton = panel.transform.Find("FoldoutMenu").GetComponent<Button>();
        Button toggleButton = panel.transform.Find("ToggleMenu").GetComponent<Button>();
        bool isExpanded = true;

        UnityAction toggleMenu = () =>
        {
            isExpanded = isExpanded == false;
            panel.SetControllerState(ControllerName, isExpanded ? "Expanded" : "Collapsed");
        };

        menuButton.onClick.AddListener(toggleMenu);
        toggleButton.onClick.AddListener(toggleMenu);
        panel.SetControllerState(ControllerName, "Expanded", true);
    }
}
