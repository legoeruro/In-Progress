using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum FailedDropBehavior
    {
        None,
        ReturnToStart,
        DestroyGameObject
    }

    private Canvas canvas; // the canvas this lives under

    public bool IsDragging { get; private set; }
    public System.Action<DraggableUI> DragEnded;

    private RectTransform rect;
    private CanvasGroup canvasGroup;

    private Vector2 startAnchoredPos;
    private Transform startParent;
    private bool wasAcceptedByDropTarget;

    [Header("Drag Behavior")]
    [SerializeField] private FailedDropBehavior failedDropBehavior = FailedDropBehavior.None;

    [Header("Template Spawner")]
    [SerializeField] private bool spawnCopyOnDrag;
    [SerializeField] private bool spawnedCopiesDestroyOnInvalidDrop = true;

    private DraggableUI activeDragCopy;

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
        if (spawnCopyOnDrag)
        {
            BeginDragWithSpawnedCopy(eventData);
            return;
        }

        BeginDragInternal();
    }

    private void BeginDragWithSpawnedCopy(PointerEventData eventData)
    {
        if (activeDragCopy != null && !activeDragCopy.IsDragging)
            ClearActiveDragCopyReference();
        if (activeDragCopy != null)
            return;

        var copyObject = Instantiate(gameObject, transform.parent);
        copyObject.name = gameObject.name;

        var copyDrag = copyObject.GetComponent<DraggableUI>();
        if (copyDrag == null)
        {
            Destroy(copyObject);
            return;
        }

        copyDrag.spawnCopyOnDrag = false;
        if (spawnedCopiesDestroyOnInvalidDrop)
            copyDrag.failedDropBehavior = FailedDropBehavior.DestroyGameObject;

        activeDragCopy = copyDrag;
        activeDragCopy.DragEnded += OnActiveDragCopyEnded;

        // Ensure drops target the runtime copy, not the template source.
        eventData.pointerDrag = copyObject;
        copyDrag.BeginDragInternal();
    }

    private void BeginDragInternal()
    {
        IsDragging = true;
        wasAcceptedByDropTarget = false;

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

        // form: detach from submit zone
        var form = GetComponent<Form>();
        if (form != null && form.CurrentSubmitZone != null)
        {
            form.CurrentSubmitZone.Deregister(form);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activeDragCopy != null)
        {
            activeDragCopy.OnDrag(eventData);
            return;
        }

        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activeDragCopy != null)
        {
            if (activeDragCopy.IsDragging)
                activeDragCopy.OnEndDrag(eventData);
            ClearActiveDragCopyReference();
            return;
        }

        IsDragging = false;
        canvasGroup.blocksRaycasts = true;

        DragEnded?.Invoke(this);

        if (!wasAcceptedByDropTarget)
        {
            switch (failedDropBehavior)
            {
                case FailedDropBehavior.ReturnToStart:
                    ReturnToStart();
                    break;
                case FailedDropBehavior.DestroyGameObject:
                    Destroy(gameObject);
                    break;
            }
        }
    }

    public void ReturnToStart()
    {
        rect.SetParent(startParent, worldPositionStays: false);
        rect.anchoredPosition = startAnchoredPos;
    }

    public void MarkAcceptedByDropTarget()
    {
        wasAcceptedByDropTarget = true;
    }

    public void SetFailedDropBehavior(FailedDropBehavior behavior)
    {
        failedDropBehavior = behavior;
    }

    public void ConfigureTemplateSpawner(bool enabled, bool destroyCopiesOnInvalidDrop = true)
    {
        spawnCopyOnDrag = enabled;
        spawnedCopiesDestroyOnInvalidDrop = destroyCopiesOnInvalidDrop;
    }

    private void OnActiveDragCopyEnded(DraggableUI endedDragCopy)
    {
        if (endedDragCopy == activeDragCopy)
            ClearActiveDragCopyReference();
    }

    private void ClearActiveDragCopyReference()
    {
        if (activeDragCopy != null)
            activeDragCopy.DragEnded -= OnActiveDragCopyEnded;
        activeDragCopy = null;
    }
}
