using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubmitZone : MonoBehaviour, IDropTarget
{
    [Header("Layout")]
    [SerializeField] private RectTransform snapPoint;

    [Header("Visuals")]
    [SerializeField] private float droppedScaleMultiplier = 0.5f;
    [SerializeField] private bool tintZoneOnHover = true;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color hoverColor = new Color(0.85f, 0.95f, 1f, 1f);

    private readonly HashSet<Form> registeredForms = new HashSet<Form>();
    private readonly Dictionary<Form, Vector3> originalScales = new Dictionary<Form, Vector3>();

    private Image zoneImage;

    public event Action<List<Form>> SubmitRequested;

    public IReadOnlyCollection<Form> RegisteredForms => registeredForms;

    private void Awake()
    {
        zoneImage = GetComponent<Image>();
        if (zoneImage != null && tintZoneOnHover)
            zoneImage.color = normalColor;
    }

    public bool CanAccept(DraggableUI draggable)
    {
        return draggable != null && draggable.GetComponent<Form>() != null;
    }

    public void Accept(DraggableUI draggable)
    {
        var form = draggable.GetComponent<Form>();
        if (form == null) return;

        Register(form);

        var rect = form.GetComponent<RectTransform>();
        if (rect != null)
        {
            var targetParent = snapPoint != null ? snapPoint : transform as RectTransform;
            rect.SetParent(targetParent, true);
        }
    }

    public void Register(Form form)
    {
        if (form == null) return;
        if (!registeredForms.Add(form)) return;

        if (!originalScales.ContainsKey(form))
            originalScales[form] = form.transform.localScale;

        form.transform.localScale = originalScales[form] * droppedScaleMultiplier;

        // TODO: in the future - submit zone could also be different mail addresses
        form.SetSubmitZone(this);
    }

    public void Deregister(Form form)
    {
        if (form == null) return;
        if (!registeredForms.Remove(form)) return;

        if (originalScales.TryGetValue(form, out var scale))
        {
            form.transform.localScale = scale;
            originalScales.Remove(form);
        }
        else
        {
            form.transform.localScale = Vector3.one;
        }

        if (form.CurrentSubmitZone == this)
            form.ClearSubmitZone();
    }

    public void SubmitAll()
    {
        if (registeredForms.Count == 0) return;
        SubmitRequested?.Invoke(new List<Form>(registeredForms));
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
}
