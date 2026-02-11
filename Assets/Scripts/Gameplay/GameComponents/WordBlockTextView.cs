using TMPro;
using UnityEngine;
using InProcess.Gameplay;

[RequireComponent(typeof(WordBlock))]
public class WordBlockTextView : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private TMP_Text typeLabel;

    [Header("Sizing")]
    [SerializeField] private RectTransform blockRect;
    [SerializeField] private float minWidth = 110f;
    [SerializeField] private float maxWidth = 260f;
    [SerializeField] private float horizontalPadding = 24f;

    [Header("Display")]
    [SerializeField] private bool useDisplayNameFallback = true;

    private WordBlock wordBlock;

    private void Awake()
    {
        wordBlock = GetComponent<WordBlock>();
        if (blockRect == null)
            blockRect = GetComponent<RectTransform>();
        if (valueLabel == null)
            valueLabel = GetComponentInChildren<TMP_Text>(includeInactive: true);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (wordBlock == null)
            return;

        string valueText = ResolveValueText();
        string typeText = ResolveTypeLabelText();

        if (valueLabel != null)
        {
            valueLabel.enableWordWrapping = false;
            valueLabel.overflowMode = TextOverflowModes.Ellipsis;
            valueLabel.text = valueText;
        }

        if (typeLabel != null)
            typeLabel.text = typeText;

        RefreshWidth(valueText);
    }

    public void ApplySizing(float minWidthValue, float maxWidthValue, float horizontalPaddingValue)
    {
        minWidth = minWidthValue;
        maxWidth = maxWidthValue;
        horizontalPadding = horizontalPaddingValue;
    }

    private void RefreshWidth(string valueText)
    {
        if (blockRect == null || valueLabel == null)
            return;

        float preferred = valueLabel.GetPreferredValues(valueText).x + horizontalPadding;
        float clampedWidth = Mathf.Clamp(preferred, minWidth, maxWidth);
        var size = blockRect.sizeDelta;
        size.x = clampedWidth;
        blockRect.sizeDelta = size;
    }

    private string ResolveValueText()
    {
        if (!string.IsNullOrWhiteSpace(wordBlock.value))
            return wordBlock.value;

        if (useDisplayNameFallback && wordBlock.valueType != null && !string.IsNullOrWhiteSpace(wordBlock.valueType.displayName))
            return wordBlock.valueType.displayName;

        return "Word Block";
    }

    private string ResolveTypeLabelText()
    {
        if (!string.IsNullOrWhiteSpace(wordBlock.typeLabelOverride))
            return wordBlock.typeLabelOverride;

        if (wordBlock.valueType != null && !string.IsNullOrWhiteSpace(wordBlock.valueType.displayName))
            return wordBlock.valueType.displayName;

        if (wordBlock.inventoryItemType == ItemType.Attachment)
            return "Attachment";

        return "Word Block";
    }
}
