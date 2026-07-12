using UnityEngine;
using UnityEngine.EventSystems;
using Windsmoon.UIController;

public class UIControllerButtonState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const string ControllerName = "Interaction";

    [SerializeField]
    private UIControllerPanel _panel;

    private bool _isPointerInside;

    private void Awake()
    {
        _panel.SetControllerState(ControllerName, "Normal", true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        _panel.SetControllerState(ControllerName, "Hover");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        _panel.SetControllerState(ControllerName, "Normal");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _panel.SetControllerState(ControllerName, "Pressed");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _panel.SetControllerState(ControllerName, _isPointerInside ? "Hover" : "Normal");
    }
}
