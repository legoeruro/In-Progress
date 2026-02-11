using UnityEngine;
using InProcess.Gameplay;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Form : MonoBehaviour
{
    public FormDefinition Definition { get; private set; }
    public SubmitZone CurrentSubmitZone { get; private set; }
    public DiscardZone CurrentDiscardZone { get; private set; }

    // Assuming we're generating the forms and it's contents automatically
    private List<FillSlot> formSlots;

    [Header("Timing")]
    [SerializeField] private float timeToCompleteSeconds = -1f;
    [SerializeField] private float fadeOutSeconds = 1.5f;
    [SerializeField] private Image timeBarImage;

    private float remainingSeconds;
    private float totalSeconds;
    private CanvasGroup canvasGroup;
    private bool isExpiring;
    private Coroutine spawnRoutine;
    private Coroutine expireRoutine;

    [Header("Spawn Animation")]
    [SerializeField] private bool playSpawnAnimation = true;
    [SerializeField] private float spawnAnimDuration = 0.25f;
    [SerializeField] private float spawnStartScale = 0.9f;

    public event Action<Form> Expired;

    public void Initialize(FormDefinition formData)
    {
        ResetTimingState();

        Definition = formData;
        timeToCompleteSeconds = formData != null ? formData.timeToCompleteSeconds : -1f;

        // Step 1: Load layout config and store margins as variables
        FormLayoutConfig layoutConfig = formData.formLayoutConfig;
        if (layoutConfig == null)
        {
            Debug.LogError("Form " + gameObject.name + " has no FormLayoutConfig assigned!");
            return;
        }

        Vector2 padding = layoutConfig.padding;
        float slotSpacing = layoutConfig.slotSpacing;
        Vector2 slotSize = layoutConfig.slotSize;
        FormTextFormattingConfig textFormatting = layoutConfig.textFormatting;

        // Initialize form slots list
        formSlots = new List<FillSlot>();

        RectTransform formRect = GetComponent<RectTransform>();
        if (formRect == null)
        {
            Debug.LogError("Form requires a RectTransform component!");
            return;
        }

        float formWidth = formRect.rect.width;
        if (formWidth <= 0f)
        {
            formWidth = formRect.sizeDelta.x;
        }

        // Step 2: Iterate through content array and generate texts + fill slots
        float currentYPosition = -padding.y;

        foreach (var content in formData.contentArray)
        {
            switch (content.type)
            {
                case FormContentType.Title:
                case FormContentType.H1:
                case FormContentType.H2:
                case FormContentType.H3:
                case FormContentType.Text:
                    float textHeight = CreateTextElement(content, currentYPosition, padding.x, formWidth, textFormatting);
                    currentYPosition -= textHeight;
                    break;

                case FormContentType.FillSlot:
                    FillSlot newSlot = CreateFillSlotElement(content.fillSlotData, currentYPosition, padding.x, formWidth, slotSize, layoutConfig);
                    if (newSlot != null)
                    {
                        formSlots.Add(newSlot);
                    }
                    currentYPosition -= slotSize.y;
                    break;
            }

            // Add spacing between elements
            currentYPosition -= slotSpacing;
        }

        InitializeTiming();
    }

    private float CreateTextElement(FormContentField content, float yPosition, float xPadding, float formWidth, FormTextFormattingConfig textFormatting)
    {
        GameObject textObj = new GameObject($"Text_{content.type}");
        textObj.transform.SetParent(transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textObj.AddComponent<LayoutElement>();

        // Set text content
        textComponent.text = content.text;
        textComponent.enableWordWrapping = true;
        textComponent.overflowMode = TextOverflowModes.Overflow;

        // Apply formatting based on content type
        TextFormattingHelper.ApplyStyles(textComponent, content.type, textFormatting);

        // Anchor to top-left for consistent positioning
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);

        // Position the text
        textRect.anchoredPosition = new Vector2(xPadding, yPosition);
        float availableWidth = formWidth - (xPadding * 2);
        float preferredHeight = textComponent.GetPreferredValues(content.text, availableWidth, 0f).y;
        textRect.sizeDelta = new Vector2(availableWidth, preferredHeight);

        return preferredHeight;
    }

    /// <summary>
    /// Creates a fill slot element (interactive drop target) in the form.
    /// </summary>
    private FillSlot CreateFillSlotElement(FillSlotData slotData, float yPosition, float xPadding, float formWidth, Vector2 slotSize, FormLayoutConfig config)
    {
        if (slotData == null)
        {
            Debug.LogWarning("FillSlotData is null, skipping fill slot creation");
            return null;
        }

        GameObject slotObj = new GameObject($"FillSlot_{slotData.requiredType}");
        slotObj.transform.SetParent(transform, false);

        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        Image slotImage = slotObj.AddComponent<Image>();
        FillSlot fillSlot = slotObj.AddComponent<FillSlot>();
        var dropZone = slotObj.AddComponent<DropZoneUI>();
        fillSlot.requiredType = slotData.requiredType;
        dropZone.SetSnapPoint(slotRect);

        // Set appearance
        slotImage.color = config != null ? config.slotBackgroundColor : new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray background
        slotImage.raycastTarget = true;

        // Position and size
        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0f, 1f);
        slotRect.anchoredPosition = new Vector2(xPadding, yPosition);
        slotRect.sizeDelta = new Vector2(formWidth - (xPadding * 2), slotSize.y);

        return fillSlot;
    }

    /// <summary>
    /// Returns the filled data as a submission object.
    /// </summary>
    public Dictionary<string, string> GetFormData()
    {
        Dictionary<string, string> data = new Dictionary<string, string>();

        foreach (var slot in formSlots)
        {
            if (slot.CurrentWordBlock != null)
            {
                var fieldType = slot.CurrentWordBlock.valueType;
                var key = fieldType != null && !string.IsNullOrWhiteSpace(fieldType.fieldId)
                    ? fieldType.fieldId
                    : fieldType != null ? fieldType.name : "UnknownField";
                data[key] = slot.CurrentWordBlock.value;
            }
        }

        return data;
    }

    public bool VerifyForm()
    {
        foreach (var slot in formSlots)
        {
            if (!slot.CurrentWordBlock)
                return false;
            if (slot.CurrentWordBlock.valueType != slot.requiredType)
            {
                return false;
            }
        }
        return true;
    }

    private void InitializeTiming()
    {
        ResetTimingState();
        EnsureTimeBarReference();

        if (timeToCompleteSeconds < 0f)
        {
            if (timeBarImage != null)
            {
                timeBarImage.fillAmount = 1f;
                timeBarImage.gameObject.SetActive(false);
            }
            return;
        }

        totalSeconds = Mathf.Max(0.1f, timeToCompleteSeconds);
        remainingSeconds = totalSeconds;

        if (timeBarImage != null)
        {
            timeBarImage.type = Image.Type.Filled;
            timeBarImage.fillMethod = Image.FillMethod.Horizontal;
            timeBarImage.fillOrigin = 0;
            timeBarImage.transform.SetAsLastSibling();
            timeBarImage.gameObject.SetActive(true);
            timeBarImage.fillAmount = 1f;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        isExpiring = true;
    }

    private void Update()
    {
        if (!isExpiring || timeToCompleteSeconds < 0f) return;

        remainingSeconds -= Time.deltaTime;
        if (timeBarImage != null && totalSeconds > 0f)
            timeBarImage.fillAmount = Mathf.Clamp01(remainingSeconds / totalSeconds);

        if (remainingSeconds <= 0f)
        {
            isExpiring = false;
            if (expireRoutine == null)
                expireRoutine = StartCoroutine(FadeOutAndDisable());
        }
    }

    private System.Collections.IEnumerator FadeOutAndDisable()
    {
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        float t = 0f;
        float startAlpha = canvasGroup.alpha;
        float duration = Mathf.Max(0.1f, fadeOutSeconds);

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalized);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        expireRoutine = null;
        Expired?.Invoke(this);
        gameObject.SetActive(false);
    }

    public void PlaySpawnAnimation()
    {
        if (!playSpawnAnimation) return;
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnAnimRoutine());
    }

    private System.Collections.IEnumerator SpawnAnimRoutine()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        float duration = Mathf.Max(0.05f, spawnAnimDuration);
        float t = 0f;
        float startScale = Mathf.Max(0.1f, spawnStartScale);
        Vector3 targetScale = Vector3.one;

        transform.localScale = new Vector3(startScale, startScale, startScale);
        canvasGroup.alpha = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            float scale = Mathf.Lerp(startScale, 1f, eased);
            transform.localScale = new Vector3(scale, scale, scale);
            canvasGroup.alpha = eased;
            yield return null;
        }

        transform.localScale = targetScale;
        canvasGroup.alpha = 1f;
    }

    public void SetSubmitZone(SubmitZone zone)
    {
        CurrentSubmitZone = zone;
    }

    public void ClearSubmitZone()
    {
        CurrentSubmitZone = null;
    }

    public void SetDiscardZone(DiscardZone zone)
    {
        CurrentDiscardZone = zone;
    }

    public void ClearDiscardZone()
    {
        CurrentDiscardZone = null;
    }

    private void OnDisable()
    {
        ResetTimingState();

        if (CurrentSubmitZone != null)
            CurrentSubmitZone.Deregister(this);
        if (CurrentDiscardZone != null)
            CurrentDiscardZone.Deregister(this);
    }

    private void ResetTimingState()
    {
        isExpiring = false;
        remainingSeconds = 0f;
        totalSeconds = 0f;

        if (expireRoutine != null)
        {
            StopCoroutine(expireRoutine);
            expireRoutine = null;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void EnsureTimeBarReference()
    {
        if (timeBarImage != null) return;

        var images = GetComponentsInChildren<Image>(includeInactive: true);
        for (int i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image == null || image.gameObject == gameObject) continue;

            var name = image.gameObject.name;
            if (name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                timeBarImage = image;
                return;
            }
        }
    }
}
