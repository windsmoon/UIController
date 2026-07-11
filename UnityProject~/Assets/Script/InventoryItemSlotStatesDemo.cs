using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Windsmoon.UIController;

public sealed class InventoryItemSlotStatesDemo : MonoBehaviour
{
    private const string SceneName = "InventoryItemSlotStates";
    private const string ControllerName = "InventoryItemSlot";

    private static readonly string[] StateNames = { "Unselected", "Selected", "Locked", "Obtained" };
    private static readonly Color[] CrystalColors =
    {
        new Color(0.5f, 0.64f, 0.58f, 1f),
        new Color(0.24f, 0.95f, 0.72f, 1f),
        new Color(0.25f, 0.27f, 0.26f, 1f),
        new Color(1f, 0.68f, 0.18f, 1f)
    };

    private readonly Image[] _selectorImages = new Image[4];
    private readonly Image[] _crystalImages = new Image[3];
    private UIControllerPanel _panel;
    private Button _itemSlot;
    private TMP_FontAsset _font;
    private int _currentStateIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        UIControllerPanel panel = Object.FindFirstObjectByType<UIControllerPanel>();
        if (panel != null && panel.GetComponent<InventoryItemSlotStatesDemo>() == null)
        {
            panel.gameObject.AddComponent<InventoryItemSlotStatesDemo>();
        }
    }

    private void Awake()
    {
        _panel = GetComponent<UIControllerPanel>();
        _itemSlot = transform.Find("InventoryItemSlot").GetComponent<Button>();
        _font = transform.Find("Title").GetComponent<TMP_Text>().font;

        BuildCrystalIcon();
        BuildStateSelectors();
        _itemSlot.onClick.AddListener(OnItemSlotClicked);
        SetState(0, true);
    }

    private void OnDestroy()
    {
        _itemSlot.onClick.RemoveListener(OnItemSlotClicked);
    }

    private void OnItemSlotClicked()
    {
        SetState(_currentStateIndex == 1 ? 0 : 1);
    }

    private void SetState(int stateIndex, bool forceNoAnimation = false)
    {
        _currentStateIndex = stateIndex;
        _panel.SetControllerState(ControllerName, StateNames[stateIndex], forceNoAnimation);

        for (int index = 0; index < _selectorImages.Length; index++)
        {
            _selectorImages[index].color = index == stateIndex
                ? new Color(0.52f, 0.27f, 0.07f, 1f)
                : new Color(0.11f, 0.125f, 0.12f, 1f);
        }

        Color crystalColor = CrystalColors[stateIndex];
        _crystalImages[0].color = crystalColor;
        _crystalImages[1].color = new Color(crystalColor.r * 0.72f, crystalColor.g * 0.72f, crystalColor.b * 0.72f, 1f);
        _crystalImages[2].color = stateIndex == 2
            ? new Color(0.38f, 0.4f, 0.39f, 1f)
            : new Color(1f, 0.95f, 0.75f, 0.9f);
    }

    private void BuildCrystalIcon()
    {
        Transform slotTransform = _itemSlot.transform;
        _crystalImages[0] = CreateImage("CrystalCore", slotTransform, new Vector2(0f, 34f), new Vector2(74f, 74f), 45f);
        _crystalImages[1] = CreateImage("CrystalInner", slotTransform, new Vector2(0f, 34f), new Vector2(46f, 46f), 45f);
        _crystalImages[2] = CreateImage("CrystalShine", slotTransform, new Vector2(-14f, 52f), new Vector2(8f, 32f), -24f);

        for (int index = _crystalImages.Length - 1; index >= 0; index--)
        {
            _crystalImages[index].transform.SetAsFirstSibling();
        }
    }

    private void BuildStateSelectors()
    {
        for (int stateIndex = 0; stateIndex < StateNames.Length; stateIndex++)
        {
            int capturedStateIndex = stateIndex;
            GameObject buttonObject = new GameObject($"State_{StateNames[stateIndex]}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(transform, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2((stateIndex - 1.5f) * 205f, -245f);
            rectTransform.sizeDelta = new Vector2(190f, 58f);

            Image image = buttonObject.GetComponent<Image>();
            _selectorImages[stateIndex] = image;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.43f, 0.32f, 0.18f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.62f, 1f);
            colors.pressedColor = new Color(0.76f, 0.58f, 0.34f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(() => SetState(capturedStateIndex));

            CreateSelectorLabel(buttonObject.transform, StateNames[stateIndex].ToUpperInvariant());
        }
    }

    private Image CreateImage(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, float rotation)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private void CreateSelectorLabel(Transform parent, string value)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = _font;
        label.text = value;
        label.fontSize = 17f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.94f, 0.89f, 0.77f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }
}
