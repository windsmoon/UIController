using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Windsmoon.UIController;

public sealed class ProjectButtonTabStatesDemo : MonoBehaviour
{
    private const string SceneName = "ProjectButtonTabStates";
    private const string ButtonControllerName = "ProjectButton";
    private const string TabControllerName = "ProjectTab";

    private static readonly string[] StateNames = { "Normal", "Selected", "Disabled" };
    private static readonly Color Gold = new Color(0.91f, 0.64f, 0.22f, 1f);
    private static readonly Color MutedGold = new Color(0.43f, 0.32f, 0.18f, 1f);
    private static readonly Color SelectorNormal = new Color(0.12f, 0.13f, 0.14f, 1f);
    private static readonly Color SelectorSelected = new Color(0.48f, 0.24f, 0.07f, 1f);

    private readonly Image[] _stateButtonImages = new Image[3];
    private UIControllerPanel _panel;
    private Button _projectButton;
    private Button _projectTab;
    private TMP_Text _buttonStateText;
    private TMP_Text _tabStateText;
    private TMP_FontAsset _font;
    private int _buttonStateIndex;
    private int _tabStateIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        UIControllerPanel panel = Object.FindFirstObjectByType<UIControllerPanel>();
        if (panel != null && panel.GetComponent<ProjectButtonTabStatesDemo>() == null)
        {
            panel.gameObject.AddComponent<ProjectButtonTabStatesDemo>();
        }
    }

    private void Awake()
    {
        _panel = GetComponent<UIControllerPanel>();
        _projectButton = transform.Find(ButtonControllerName).GetComponent<Button>();
        _projectTab = transform.Find(TabControllerName).GetComponent<Button>();
        _font = transform.Find("Title").GetComponent<TMP_Text>().font;

        BuildGameHud();

        _projectButton.onClick.AddListener(OnProjectButtonClicked);
        _projectTab.onClick.AddListener(OnProjectTabClicked);
        SetBothStates(0, true);
    }

    private void OnDestroy()
    {
        _projectButton.onClick.RemoveListener(OnProjectButtonClicked);
        _projectTab.onClick.RemoveListener(OnProjectTabClicked);
    }

    private void OnProjectButtonClicked()
    {
        _buttonStateIndex = (_buttonStateIndex + 1) % StateNames.Length;
        _panel.SetControllerState(ButtonControllerName, StateNames[_buttonStateIndex]);
        RefreshStateLabels();
        ClearSelectorHighlight();
    }

    private void OnProjectTabClicked()
    {
        _tabStateIndex = (_tabStateIndex + 1) % StateNames.Length;
        _panel.SetControllerState(TabControllerName, StateNames[_tabStateIndex]);
        RefreshStateLabels();
        ClearSelectorHighlight();
    }

    private void SetBothStates(int stateIndex, bool forceNoAnimation = false)
    {
        _buttonStateIndex = stateIndex;
        _tabStateIndex = stateIndex;
        _panel.SetControllerState(ButtonControllerName, StateNames[stateIndex], forceNoAnimation);
        _panel.SetControllerState(TabControllerName, StateNames[stateIndex], forceNoAnimation);
        RefreshStateLabels();
        RefreshSelectorHighlight(stateIndex);
    }

    private void BuildGameHud()
    {
        CreateAccentLine(new Vector2(0f, 225f), new Vector2(760f, 3f));
        CreateAccentLine(new Vector2(0f, -118f), new Vector2(760f, 2f));

        CreateText("ButtonSection", "ACTION BUTTON", new Vector2(-250f, 130f), new Vector2(390f, 32f), 18f, Gold);
        CreateText("TabSection", "NAVIGATION TAB", new Vector2(250f, 130f), new Vector2(390f, 32f), 18f, Gold);
        _buttonStateText = CreateText("ButtonState", string.Empty, new Vector2(-250f, -43f), new Vector2(390f, 30f), 16f, Color.white);
        _tabStateText = CreateText("TabState", string.Empty, new Vector2(250f, -43f), new Vector2(390f, 30f), 16f, Color.white);

        for (int stateIndex = 0; stateIndex < StateNames.Length; stateIndex++)
        {
            int capturedStateIndex = stateIndex;
            Button button = CreateStateButton(StateNames[stateIndex], new Vector2((stateIndex - 1) * 230f, -205f));
            button.onClick.AddListener(() => SetBothStates(capturedStateIndex));
            _stateButtonImages[stateIndex] = button.GetComponent<Image>();
        }
    }

    private Button CreateStateButton(string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject($"State_{label}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(transform, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(210f, 58f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = SelectorNormal;

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = MutedGold;
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.62f, 1f);
        colors.pressedColor = new Color(0.76f, 0.58f, 0.34f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TMP_Text text = CreateText("Label", label.ToUpperInvariant(), Vector2.zero, Vector2.zero, 20f, new Color(0.94f, 0.89f, 0.77f, 1f), buttonObject.transform);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private TMP_Text CreateText(string objectName, string value, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color, Transform parent = null)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent == null ? transform : parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private void CreateAccentLine(Vector2 anchoredPosition, Vector2 size)
    {
        GameObject lineObject = new GameObject("GoldAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(transform, false);

        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        lineObject.GetComponent<Image>().color = MutedGold;
    }

    private void RefreshStateLabels()
    {
        _buttonStateText.text = $"<color=#8D7961>STATE</color>  {StateNames[_buttonStateIndex].ToUpperInvariant()}";
        _tabStateText.text = $"<color=#8D7961>STATE</color>  {StateNames[_tabStateIndex].ToUpperInvariant()}";
    }

    private void RefreshSelectorHighlight(int selectedStateIndex)
    {
        for (int stateIndex = 0; stateIndex < _stateButtonImages.Length; stateIndex++)
        {
            _stateButtonImages[stateIndex].color = stateIndex == selectedStateIndex ? SelectorSelected : SelectorNormal;
        }
    }

    private void ClearSelectorHighlight()
    {
        for (int stateIndex = 0; stateIndex < _stateButtonImages.Length; stateIndex++)
        {
            _stateButtonImages[stateIndex].color = SelectorNormal;
        }
    }
}
