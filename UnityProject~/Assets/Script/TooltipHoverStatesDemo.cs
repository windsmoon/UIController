using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Windsmoon.UIController;

public static class TooltipHoverStatesDemo
{
    private const string SceneName = "TooltipHoverStates";
    private const string TooltipControllerName = "Tooltip";
    private const string TargetControllerName = "HoverTargetPosition";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName)
        {
            return;
        }

        UIControllerPanel panel = GameObject.Find("UIControllerDemoPanel").GetComponent<UIControllerPanel>();
        EventTrigger hoverTrigger = panel.transform.Find("HoverTarget").GetComponent<EventTrigger>();
        Button moveButton = panel.transform.Find("MoveTarget").GetComponent<Button>();
        Button showButton = panel.transform.Find("ShowTooltip").GetComponent<Button>();
        Button hideButton = panel.transform.Find("HideTooltip").GetComponent<Button>();
        bool targetOnRight = false;

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener(_ => panel.SetControllerState(TooltipControllerName, "Visible"));
        EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener(_ => panel.SetControllerState(TooltipControllerName, "Hidden"));
        hoverTrigger.triggers.Clear();
        hoverTrigger.triggers.Add(pointerEnter);
        hoverTrigger.triggers.Add(pointerExit);

        moveButton.onClick.AddListener(() =>
        {
            targetOnRight = targetOnRight == false;
            panel.SetControllerState(TargetControllerName, targetOnRight ? "Right" : "Left");
        });
        showButton.onClick.AddListener(() => panel.SetControllerState(TooltipControllerName, "Visible"));
        hideButton.onClick.AddListener(() => panel.SetControllerState(TooltipControllerName, "Hidden"));

        panel.SetControllerState(TargetControllerName, "Left", true);
        panel.SetControllerState(TooltipControllerName, "Hidden", true);
    }
}
