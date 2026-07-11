using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Windsmoon.UIController;

public class DemoSceneNavigation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const string MainSceneName = "Main";

    [SerializeField]
    private UIControllerPanel _panel;
    [SerializeField]
    private string _controllerName;

    private void Awake()
    {
        _panel.SetControllerState(_controllerName, "Normal", true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _panel.SetControllerState(_controllerName, "Hover");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _panel.SetControllerState(_controllerName, "Normal");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _panel.SetControllerState(_controllerName, "Pressed");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _panel.SetControllerState(_controllerName, "Hover");
    }

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
