using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas; // the canvas this lives under

    public bool IsDragging { get; private set; }
    public System.Action<DraggableUI> DragEnded;

    private RectTransform rect;
    private CanvasGroup canvasGroup;

    private Vector2 startAnchoredPos;
    private Transform startParent;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();


        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDragging = true;

        startAnchoredPos = rect.anchoredPosition;
        startParent = rect.parent;

        // Let raycasts pass through while dragging so drop targets can be detected
        canvasGroup.blocksRaycasts = false;

        rect.SetAsLastSibling();
        rect.SetParent(canvas.transform, worldPositionStays: true);
        rect.SetAsLastSibling();

        // item-specific behaviors

        //wordblock: clear the current slot the word block is in
        var wordBlock = GetComponent<WordBlock>();
        if (wordBlock?.currentSlot != null)
        {
            wordBlock.currentSlot.Detach();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        canvasGroup.blocksRaycasts = true;

        DragEnded?.Invoke(this);
    }

    public void ReturnToStart()
    {
        rect.SetParent(startParent, worldPositionStays: false);
        rect.anchoredPosition = startAnchoredPos;
    }
}
