using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FormWindowMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FormFactory formFactory;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Slider scrollSlider;

    [Header("Behavior")]
    [SerializeField] private bool featureEnabled = true;
    [SerializeField] private bool windowVisible = true;
    [SerializeField] private float previewScale = 0.5f;

    [Header("Layout")]
    [SerializeField] private float itemSpacing = 12f;
    [SerializeField] private bool resizeContentRootWidth;

    private readonly List<Form> parkedForms = new List<Form>();
    private float currentScrollNormalized;
    private float currentContentWidth;

    public bool FeatureEnabled => featureEnabled;

    private void Awake()
    {
        if (formFactory == null)
            formFactory = FindFirstObjectByType<FormFactory>();
        if (viewport == null && contentRoot != null)
            viewport = contentRoot.parent as RectTransform;

        SetWindowVisible(windowVisible);
    }

    private void OnEnable()
    {
        if (scrollSlider != null)
            scrollSlider.onValueChanged.AddListener(OnScrollValueChanged);
    }

    private void OnDisable()
    {
        if (scrollSlider != null)
            scrollSlider.onValueChanged.RemoveListener(OnScrollValueChanged);
    }

    public void SetFeatureEnabled(bool enabled)
    {
        if (featureEnabled == enabled)
            return;

        featureEnabled = enabled;
        if (!featureEnabled)
            ReleaseAllParkedForms();
    }

    public void SetWindowVisible(bool visible)
    {
        windowVisible = visible;
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }

    public bool RegisterSpawnedForm(Form form)
    {
        if (!featureEnabled || form == null || contentRoot == null)
            return false;
        if (parkedForms.Contains(form))
            return true;

        parkedForms.Add(form);
        AttachPreviewClick(form);

        var drag = form.GetComponent<DraggableUI>();
        if (drag != null)
            drag.enabled = false;

        var rect = form.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.SetParent(contentRoot, worldPositionStays: false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.localScale = Vector3.one * Mathf.Max(0.1f, previewScale);
        }

        ApplyLayoutAndScroll();
        return true;
    }

    public void UnregisterForm(Form form)
    {
        if (form == null) return;
        bool removed = parkedForms.Remove(form);
        if (!removed) return;

        var click = form.GetComponent<FormWindowPreviewClick>();
        if (click != null)
            Destroy(click);

        ApplyLayoutAndScroll();
    }

    public void OpenFormFromWindow(Form form)
    {
        if (form == null || !parkedForms.Remove(form))
            return;

        var click = form.GetComponent<FormWindowPreviewClick>();
        if (click != null)
            Destroy(click);

        var drag = form.GetComponent<DraggableUI>();
        if (drag != null)
            drag.enabled = true;

        if (formFactory != null)
            formFactory.PlaceAtCenter(form);

        form.transform.localScale = Vector3.one;
        form.PlaySpawnAnimation();
        ApplyLayoutAndScroll();
    }

    private void ReleaseAllParkedForms()
    {
        if (parkedForms.Count == 0)
            return;

        var copy = new List<Form>(parkedForms);
        for (int i = 0; i < copy.Count; i++)
            OpenFormFromWindow(copy[i]);
    }

    private void AttachPreviewClick(Form form)
    {
        var click = form.GetComponent<FormWindowPreviewClick>();
        if (click == null)
            click = form.gameObject.AddComponent<FormWindowPreviewClick>();
        click.Initialize(this, form);
    }

    private void ApplyLayoutAndScroll()
    {
        CleanupNullForms();
        currentContentWidth = LayoutParkedForms();
        RefreshScroll(currentContentWidth);
    }

    private void CleanupNullForms()
    {
        for (int i = parkedForms.Count - 1; i >= 0; i--)
        {
            if (parkedForms[i] == null)
                parkedForms.RemoveAt(i);
        }
    }

    private float LayoutParkedForms()
    {
        if (contentRoot == null)
            return 0f;

        float x = 0f;
        for (int i = 0; i < parkedForms.Count; i++)
        {
            var form = parkedForms[i];
            if (form == null) continue;

            var rect = form.GetComponent<RectTransform>();
            if (rect == null) continue;

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);

            float rawWidth = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
            float scaledWidth = rawWidth * Mathf.Max(0.1f, previewScale);
            x += scaledWidth + itemSpacing;
        }

        float width = Mathf.Max(0f, x - itemSpacing);
        if (resizeContentRootWidth)
        {
            var size = contentRoot.sizeDelta;
            size.x = width;
            contentRoot.sizeDelta = size;
        }

        return width;
    }

    private void RefreshScroll(float contentWidth)
    {
        if (contentRoot == null)
            return;

        float viewportWidth = viewport != null ? viewport.rect.width : 0f;
        float maxOffset = Mathf.Max(0f, contentWidth - viewportWidth);
        currentScrollNormalized = Mathf.Clamp01(currentScrollNormalized);

        if (scrollSlider != null)
        {
            bool canScroll = maxOffset > 0.001f;
            scrollSlider.minValue = 0f;
            scrollSlider.maxValue = 1f;
            scrollSlider.wholeNumbers = false;
            scrollSlider.gameObject.SetActive(canScroll);
            scrollSlider.SetValueWithoutNotify(currentScrollNormalized);
        }

        ApplyContentOffset(maxOffset);
    }

    private void OnScrollValueChanged(float normalized)
    {
        currentScrollNormalized = Mathf.Clamp01(normalized);

        float viewportWidth = viewport != null ? viewport.rect.width : 0f;
        float maxOffset = Mathf.Max(0f, currentContentWidth - viewportWidth);
        ApplyContentOffset(maxOffset);
    }

    private void ApplyContentOffset(float maxOffset)
    {
        if (contentRoot == null)
            return;

        float x = -currentScrollNormalized * maxOffset;
        contentRoot.anchoredPosition = new Vector2(x, contentRoot.anchoredPosition.y);
    }
}
