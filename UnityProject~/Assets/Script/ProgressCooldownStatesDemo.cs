using UnityEngine;
using UnityEngine.UI;
using Windsmoon.UIController;

public static class ProgressCooldownStatesDemo
{
    private const string SceneName = "ProgressCooldownStates";
    private const string ControllerName = "ProgressCooldown";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindStateButtons()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        UIControllerPanel panel = Object.FindFirstObjectByType<UIControllerPanel>();
        Button emptyButton = panel.transform.Find("State_0").GetComponent<Button>();
        Button quarterButton = panel.transform.Find("State_25").GetComponent<Button>();
        Button halfButton = panel.transform.Find("State_50").GetComponent<Button>();
        Button threeQuarterButton = panel.transform.Find("State_75").GetComponent<Button>();
        Button fullButton = panel.transform.Find("State_100").GetComponent<Button>();

        emptyButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "Empty"));
        quarterButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "Quarter"));
        halfButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "Half"));
        threeQuarterButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "ThreeQuarter"));
        fullButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "Full"));
        panel.SetControllerState(ControllerName, "Empty", true);
    }
}
