using UnityEngine;
using UnityEngine.EventSystems;

public class FormWindowPreviewClick : MonoBehaviour, IPointerClickHandler
{
    private FormWindowMenu owner;
    private Form form;

    public void Initialize(FormWindowMenu ownerMenu, Form targetForm)
    {
        owner = ownerMenu;
        form = targetForm;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;
        if (eventData.dragging)
            return;
        if (owner == null || form == null)
            return;

        owner.OpenFormFromWindow(form);
    }
}
