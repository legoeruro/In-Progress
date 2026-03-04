using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class FormInbox : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [FormerlySerializedAs("formManager")]
    [SerializeField] private MonoBehaviour queueSourceBehaviour;
    [SerializeField] private GameObject hasIncomingState;
    [SerializeField] private GameObject noIncomingState;
    [SerializeField] private FormInboxQueueCounter queueCounter;
    private IFormQueueSource queueSource;

    private void Awake()
    {
        ResolveQueueSource();
    }

    private void OnEnable()
    {
        ResolveQueueSource();

        if (queueSource != null)
            queueSource.PendingSpawnQueueChanged += OnPendingSpawnQueueChanged;

        RefreshVisualState();
    }

    private void OnDisable()
    {
        if (queueSource != null)
            queueSource.PendingSpawnQueueChanged -= OnPendingSpawnQueueChanged;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;
        if (eventData.dragging)
            return;
        if (queueSource == null)
            return;

        queueSource.TryOpenNextIncomingForm();
        RefreshVisualState();
    }

    private void OnPendingSpawnQueueChanged(int _)
    {
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        int pendingCount = queueSource != null ? queueSource.PendingSpawnCount : 0;
        bool hasIncoming = pendingCount > 0;

        if (hasIncomingState != null)
            hasIncomingState.SetActive(hasIncoming);
        if (noIncomingState != null)
            noIncomingState.SetActive(!hasIncoming);
        if (queueCounter != null)
            queueCounter.SetCount(pendingCount);
    }

    private void ResolveQueueSource()
    {
        if (queueSourceBehaviour == null)
            queueSourceBehaviour = FindFirstObjectByType<FormManager>();

        queueSource = queueSourceBehaviour as IFormQueueSource;
    }
}
