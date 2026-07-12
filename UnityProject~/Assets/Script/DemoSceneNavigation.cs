using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class DemoSceneNavigation
{
    private const string MainSceneName = "Main";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MainSceneName)
        {
            BindMainMenu();
            return;
        }

        Button[] buttonArray = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttonArray)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && label.text == "return")
            {
                button.onClick.AddListener(() => SceneManager.LoadScene(MainSceneName));
                break;
            }
        }
    }

    private static void BindMainMenu()
    {
        BindSceneButton("BUTTON / TAB STATES", "ProjectButtonTabStates");
        BindSceneButton("POPUP ANIMATION", "PopupCombinedAnimation");
        BindSceneButton("INVENTORY ITEM SLOT", "InventoryItemSlotStates");
        BindSceneButton("PROGRESS / COOLDOWN", "ProgressCooldownStates");
        BindSceneButton("FOLDOUT MENU", "FoldoutMenuStates");
        BindSceneButton("TOOLTIP HOVER", "TooltipHoverStates");
        BindSceneButton("CHARACTER HUD", "CharacterHudStates");
    }

    private static void BindSceneButton(string labelText, string sceneName)
    {
        Button[] buttonArray = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttonArray)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && label.text == labelText)
            {
                button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
                break;
            }
        }
    }
}
