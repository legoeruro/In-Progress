using UnityEngine;
using UnityEngine.EventSystems;

public class DropZoneUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private IDropTarget dropTarget;
    private void Awake()
    {
        dropTarget = GetComponent<IDropTarget>();
    }

    [SerializeField] private RectTransform snapPoint;

    public void OnDrop(PointerEventData eventData)
    {
        var draggable = eventData.pointerDrag?.GetComponent<DraggableUI>();
        if (draggable == null || dropTarget == null) return;

        if (dropTarget.CanAccept(draggable))
            dropTarget.Accept(draggable);
        
        // Dont return to start, as we want flow to be user can drag and drop stuff around as they wish
        // TODO: think about if thing is wordblock, how do we make it return but not for other stuff
        // else
        //     draggable.ReturnToStart();

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        dropTarget.OnPointerEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        dropTarget.OnPointerExit();
    }
}
