using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FormWorkspaceScaleTracker : MonoBehaviour
{
    [SerializeField] private FormWorkspaceTable workspaceTable;
    [SerializeField] private bool updateOnlyWhileDragging = true;

    private RectTransform rectTransform;
    private DraggableUI draggable;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        draggable = GetComponent<DraggableUI>();
    }

    private void OnEnable()
    {
        if (draggable != null)
            draggable.DragEnded += OnDragEnded;
    }

    private void OnDisable()
    {
        if (draggable != null)
            draggable.DragEnded -= OnDragEnded;
    }

    private void LateUpdate()
    {
        if (workspaceTable == null || rectTransform == null)
            return;

        if (updateOnlyWhileDragging && (draggable == null || !draggable.IsDragging))
            return;

        workspaceTable.ApplyScaleForPosition(rectTransform);
    }

    public void Initialize(FormWorkspaceTable table, bool onlyWhileDragging = true)
    {
        workspaceTable = table;
        updateOnlyWhileDragging = onlyWhileDragging;
        ApplyNow();
    }

    public void ApplyNow()
    {
        if (workspaceTable == null || rectTransform == null)
            return;

        workspaceTable.ApplyScaleForPosition(rectTransform);
    }

    private void OnDragEnded(DraggableUI _)
    {
        ApplyNow();
    }
}
