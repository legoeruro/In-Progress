using System;
using UnityEngine;
using UnityEngine.UI;

public class DiscardZone : MonoBehaviour, IDropTarget
{
    [Header("References")]
    [SerializeField] private FormManager formManager;
    [SerializeField] private RectTransform snapPoint;
    [SerializeField] private Button discardButton;

    [Header("Visuals")]
    [SerializeField] private float droppedScaleMultiplier = 0.5f;
    [SerializeField] private bool tintZoneOnHover = true;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.85f, 1f);
    [SerializeField] private ParticleSystem discardEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;

    private Form registeredForm;
    private Vector3 originalScale = Vector3.one;
    private Image zoneImage;

    public event Action<Form> FormDiscarded;
    public Form RegisteredForm => registeredForm;

    private void Awake()
    {
        if (formManager == null)
            formManager = FindFirstObjectByType<FormManager>();

        zoneImage = GetComponent<Image>();
        if (zoneImage != null && tintZoneOnHover)
            zoneImage.color = normalColor;
    }

    private void OnEnable()
    {
        if (discardButton != null)
            discardButton.onClick.AddListener(DiscardCurrent);
    }

    private void OnDisable()
    {
        if (discardButton != null)
            discardButton.onClick.RemoveListener(DiscardCurrent);
    }

    public bool CanAccept(DraggableUI draggable)
    {
        var form = draggable != null ? draggable.GetComponent<Form>() : null;
        if (form == null) return false;
        return registeredForm == null || registeredForm == form;
    }

    public void Accept(DraggableUI draggable)
    {
        var form = draggable.GetComponent<Form>();
        if (form == null) return;

        Register(form);
    }

    public void Register(Form form)
    {
        if (form == null) return;
        if (registeredForm != null && registeredForm != form) return;
        if (registeredForm == form) return;

        registeredForm = form;
        originalScale = form.transform.localScale;
        form.transform.localScale = originalScale * droppedScaleMultiplier;
        form.SetDiscardZone(this);

        var rect = form.GetComponent<RectTransform>();
        if (rect != null)
        {
            var targetParent = snapPoint != null ? snapPoint : transform as RectTransform;
            rect.SetParent(targetParent, worldPositionStays: false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    public void Deregister(Form form)
    {
        if (form == null) return;
        if (registeredForm != form) return;

        form.transform.localScale = originalScale;
        if (form.CurrentDiscardZone == this)
            form.ClearDiscardZone();

        registeredForm = null;
    }

    public void DiscardCurrent()
    {
        if (registeredForm == null) return;

        var form = registeredForm;
        SpawnDiscardEffect(form);
        Deregister(form);

        if (formManager != null)
            formManager.DiscardForm(form);
        else
            form.gameObject.SetActive(false);

        FormDiscarded?.Invoke(form);
    }

    public void OnPointerEnter()
    {
        if (zoneImage != null && tintZoneOnHover)
            zoneImage.color = hoverColor;
    }

    public void OnPointerExit()
    {
        if (zoneImage != null && tintZoneOnHover)
            zoneImage.color = normalColor;
    }

    private void SpawnDiscardEffect(Form form)
    {
        if (discardEffectPrefab == null) return;

        Transform spawnTransform = effectSpawnPoint != null ? effectSpawnPoint : form.transform;
        var fx = Instantiate(discardEffectPrefab, spawnTransform.position, Quaternion.identity, transform);
        fx.Play();
        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.1f);
    }
}
