using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        GameObject returnButtonObject = GameObject.Find("ReturnToMain");
        returnButtonObject.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(MainSceneName));
    }

    private static void BindMainMenu()
    {
        BindSceneButton("Demo_ProjectButtonTab", "ProjectButtonTabStates");
        BindSceneButton("Demo_PopupAnimation", "PopupCombinedAnimation");
        BindSceneButton("Demo_InventoryItem", "InventoryItemSlotStates");
        BindSceneButton("Demo_ProgressCooldown", "ProgressCooldownStates");
        BindSceneButton("Demo_FoldoutMenu", "FoldoutMenuStates");
        BindSceneButton("Demo_TooltipHover", "TooltipHoverStates");
        BindSceneButton("Demo_CharacterHUD", "CharacterHudStates");
    }

    private static void BindSceneButton(string buttonName, string sceneName)
    {
        GameObject.Find(buttonName).GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(sceneName));
    }
}
