using UnityEngine;

public class SubmitZoneQueueCounter : MonoBehaviour
{
    [SerializeField] private SubmitZone submitZone;
    [SerializeField] private FormInboxQueueCounter queueCounter;

    private void Awake()
    {
        if (submitZone == null)
            submitZone = FindFirstObjectByType<SubmitZone>();
    }

    private void OnEnable()
    {
        if (submitZone != null)
            submitZone.RegisteredFormsChanged += OnRegisteredFormsChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (submitZone != null)
            submitZone.RegisteredFormsChanged -= OnRegisteredFormsChanged;
    }

    private void OnRegisteredFormsChanged(int count)
    {
        if (queueCounter != null)
            queueCounter.SetCount(count);
    }

    private void Refresh()
    {
        int count = submitZone != null ? submitZone.RegisteredForms.Count : 0;
        if (queueCounter != null)
            queueCounter.SetCount(count);
    }
}
