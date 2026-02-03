using UnityEngine;
using InProcess.Gameplay;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Form : MonoBehaviour
{
    public FieldType valueType;
    public string value;
    public FillSlot currentSlot;

    // Assuming we're generating the forms and it's contents automatically
    private List<FillSlot> formSlots;
    public void Initialize(FormDefinition formData)
    {
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
                    CreateTextElement(content, currentYPosition, padding.x, formWidth, textFormatting);
                    currentYPosition -= TextFormattingHelper.GetLineHeight(content.type, textFormatting);
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
    }

    private void CreateTextElement(FormContentField content, float yPosition, float xPadding, float formWidth, FormTextFormattingConfig textFormatting)
    {
        GameObject textObj = new GameObject($"Text_{content.type}");
        textObj.transform.SetParent(transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textObj.AddComponent<LayoutElement>();

        // Set text content
        textComponent.text = content.text;

        // Apply formatting based on content type
        TextFormattingHelper.ApplyStyles(textComponent, content.type, textFormatting);

        // Position the text
        textRect.anchoredPosition = new Vector2(xPadding, yPosition);
        float lineHeight = TextFormattingHelper.GetLineHeight(content.type, textFormatting);
        textRect.sizeDelta = new Vector2(
            formWidth - (xPadding * 2),
            lineHeight
        );
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

        // Set appearance
        slotImage.color = config != null ? config.slotBackgroundColor : new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray background

        // Position and size
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
                data[slot.CurrentWordBlock.valueType.ToString()] = slot.CurrentWordBlock.value;
            }
        }

        return data;
    }

    public bool VerifyForm()
    {
        foreach (var slot in formSlots)
        {
            if (slot.CurrentWordBlock.valueType != slot.requiredType)
            {
                return false;
            }
        }
        return true;
    }
}
