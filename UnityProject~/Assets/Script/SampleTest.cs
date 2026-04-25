using Windsmoon.UIStateController;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class SampleTest : MonoBehaviour
{
    #region consts
    private const string LevelControllerName = "Level";
    private const string PosControllerName = "Pos";
    private const string HoverControlerName = "Hover";
    #endregion

    #region fields
    [SerializeField]
    private UIControllerPanel _starsControllerPanel;
    [SerializeField]
    private UIControllerPanel _infoControllerPanel;
    
    
    [SerializeField]
    private Button _starLevel1Button;
    [SerializeField]
    private Button _starLevel2Button;
    [SerializeField]
    private Button _starLevel3Button;
    [SerializeField]
    private Button _starLevel4Button;
    
    [SerializeField]
    private Button _upPosButton;
    [SerializeField]
    private Button _middlePosButton;
    [SerializeField]
    private Button _downPosButton;
    
    [SerializeField]
    private Image _hoverImage;

    private EventTrigger _hoverImageEventTrigger;
    private EventTrigger.Entry _hoverImagePointerEnterEntry;
    private EventTrigger.Entry _hoverImagePointerExitEntry;
    #endregion

    #region methods
    private void OnEnable()
    {
        AddButtonListener(_starLevel1Button, OnStarLevel1ButtonClicked);
        AddButtonListener(_starLevel2Button, OnStarLevel2ButtonClicked);
        AddButtonListener(_starLevel3Button, OnStarLevel3ButtonClicked);
        AddButtonListener(_starLevel4Button, OnStarLevel4ButtonClicked);
        AddButtonListener(_upPosButton, OnUpPosButtonClicked);
        AddButtonListener(_middlePosButton, OnMiddlePosButtonClicked);
        AddButtonListener(_downPosButton, OnDownPosButtonClicked);
        AddHoverImageListeners();
    }

    private void OnDisable()
    {
        RemoveButtonListener(_starLevel1Button, OnStarLevel1ButtonClicked);
        RemoveButtonListener(_starLevel2Button, OnStarLevel2ButtonClicked);
        RemoveButtonListener(_starLevel3Button, OnStarLevel3ButtonClicked);
        RemoveButtonListener(_starLevel4Button, OnStarLevel4ButtonClicked);
        RemoveButtonListener(_upPosButton, OnUpPosButtonClicked);
        RemoveButtonListener(_middlePosButton, OnMiddlePosButtonClicked);
        RemoveButtonListener(_downPosButton, OnDownPosButtonClicked);
        RemoveHoverImageListeners();
    }

    private void OnStarLevel1ButtonClicked()
    {
        SetControllerState(LevelControllerName, 0);
    }

    private void OnStarLevel2ButtonClicked()
    {
        SetControllerState(LevelControllerName, 1);
    }

    private void OnStarLevel3ButtonClicked()
    {
        SetControllerState(LevelControllerName, 2);
    }

    private void OnStarLevel4ButtonClicked()
    {
        SetControllerState(LevelControllerName, 3);
    }

    private void OnUpPosButtonClicked()
    {
        SetControllerState(PosControllerName, 0);
    }

    private void OnMiddlePosButtonClicked()
    {
        SetControllerState(PosControllerName, 1);
    }

    private void OnDownPosButtonClicked()
    {
        SetControllerState(PosControllerName, 2);
    }

    private void SetControllerState(string controllerName, int stateIndex)
    {
        _starsControllerPanel.SetControllerState(controllerName, stateIndex);
    }

    private void AddHoverImageListeners()
    {

        if (_hoverImageEventTrigger == null)
        {
            _hoverImageEventTrigger = _hoverImage.GetComponent<EventTrigger>();
            if (_hoverImageEventTrigger == null)
            {
                _hoverImageEventTrigger = _hoverImage.gameObject.AddComponent<EventTrigger>();
            }
        }

        if (_hoverImagePointerEnterEntry == null)
        {
            _hoverImagePointerEnterEntry = AddHoverImageTriggerEntry(EventTriggerType.PointerEnter, OnHoverImagePointerEnterEvent);
        }

        if (_hoverImagePointerExitEntry == null)
        {
            _hoverImagePointerExitEntry = AddHoverImageTriggerEntry(EventTriggerType.PointerExit, OnHoverImagePointerExitEvent);
        }
    }

    private void RemoveHoverImageListeners()
    {
        if (_hoverImageEventTrigger != null)
        {
            if (_hoverImagePointerEnterEntry != null)
            {
                _hoverImageEventTrigger.triggers.Remove(_hoverImagePointerEnterEntry);
            }

            if (_hoverImagePointerExitEntry != null)
            {
                _hoverImageEventTrigger.triggers.Remove(_hoverImagePointerExitEntry);
            }
        }

        _hoverImagePointerEnterEntry = null;
        _hoverImagePointerExitEntry = null;
    }

    private EventTrigger.Entry AddHoverImageTriggerEntry(EventTriggerType eventType, UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = eventType
        };
        entry.callback.AddListener(action);
        _hoverImageEventTrigger.triggers.Add(entry);
        return entry;
    }

    private void OnHoverImagePointerEnterEvent(BaseEventData eventData)
    {
        _infoControllerPanel.SetControllerState(HoverControlerName, 1);
    }

    private void OnHoverImagePointerExitEvent(BaseEventData eventData)
    {
        _infoControllerPanel.SetControllerState(HoverControlerName, 0);
    }

    private static void AddButtonListener(Button button, UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void RemoveButtonListener(Button button, UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }
    #endregion
}
