using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Windsmoon.UIController;

public static class CharacterHudStatesDemo
{
    private const string SceneName = "CharacterHudStates";
    private const string ControllerName = "CharacterHUD";

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
        Button normalButton = panel.transform.Find("State_Normal").GetComponent<Button>();
        Button lowHealthButton = panel.transform.Find("State_LowHealth").GetComponent<Button>();
        Button deadButton = panel.transform.Find("State_Dead").GetComponent<Button>();

        normalButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "Normal"));
        lowHealthButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "LowHealth"));
        deadButton.onClick.AddListener(() => panel.SetControllerState(ControllerName, "Dead"));
        panel.SetControllerState(ControllerName, "Normal", true);
    }
}
