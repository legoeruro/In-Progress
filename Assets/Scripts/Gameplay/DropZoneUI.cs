using UnityEngine;
using UnityEngine.EventSystems;

//IPointerEnterHandler, IPointerExitHandler
public class DropZoneUI : MonoBehaviour, IDropHandler
{
    private IDropTarget dropTarget;

    [SerializeField] private RectTransform snapPoint;

    private void Awake()
    {
        dropTarget = GetComponent<IDropTarget>();
        if (snapPoint == null)
            snapPoint = GetComponent<RectTransform>();
    }

    public void SetSnapPoint(RectTransform rect)
    {
        snapPoint = rect;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggable = eventData.pointerDrag?.GetComponent<DraggableUI>();
        if (draggable == null || dropTarget == null) return;

        if (dropTarget.CanAccept(draggable))
        {
            draggable.MarkAcceptedByDropTarget();
            dropTarget.Accept(draggable);
        }

        // Dont return to start, as we want flow to be user can drag and drop stuff around as they wish
        // TODO: think about if thing is wordblock, how do we make it return but not for other stuff
        // else
        //     draggable.ReturnToStart();
    }

    // public void OnPointerEnter(PointerEventData eventData)
    // {
    //     dropTarget.OnPointerEnter();
    // }

    // public void OnPointerExit(PointerEventData eventData)
    // {
    //     dropTarget.OnPointerExit();
    // }
}
