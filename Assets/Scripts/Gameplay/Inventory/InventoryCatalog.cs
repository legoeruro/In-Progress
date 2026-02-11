using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCatalog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private WordBlock wordBlockTemplatePrefab;
    [SerializeField] private Slider scrollSlider;

    [Header("Layout")]
    [SerializeField] private float itemSpacing = 12f;
    [SerializeField] private bool resizeContentRootWidth = false;
    [SerializeField] private bool allowDuplicateDefinitions = true;

    [Header("Filtering")]
    [SerializeField] private bool autoBuildFilterGroupsFromItems = true;
    [SerializeField] private List<InventoryFilterGroup> filterGroups = new List<InventoryFilterGroup>();
    [SerializeField] private InventoryFilterGroup activeFilterGroup;

    [Header("Starting Inventory Data")]
    [SerializeField] private List<WordBlockDefinition> itemDefinitions = new List<WordBlockDefinition>();
    [SerializeField] private bool clearExistingChildrenOnBuild = true;

    private readonly List<WordBlockDefinition> unlockedDefinitions = new List<WordBlockDefinition>();
    private readonly List<WordBlock> spawnedTemplates = new List<WordBlock>();
    private readonly List<WordBlock> visibleTemplates = new List<WordBlock>();

    private float currentScrollNormalized;
    private float currentContentWidth;

    public IReadOnlyList<InventoryFilterGroup> FilterGroups => filterGroups;

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

    private void Awake()
    {
        if (viewport == null && contentRoot != null)
            viewport = contentRoot.parent as RectTransform;

        BuildInitialInventory();
    }

    public void BuildInitialInventory()
    {
        unlockedDefinitions.Clear();

        for (int i = 0; i < itemDefinitions.Count; i++)
        {
            var definition = itemDefinitions[i];
            if (definition == null || !definition.unlockedAtStart)
                continue;

            AddDefinitionInternal(definition);
        }

        if (autoBuildFilterGroupsFromItems)
            RebuildFilterGroupsFromUnlockedItems();

        RebuildVisuals();
    }

    public WordBlock AddDefinition(WordBlockDefinition definition)
    {
        if (definition == null) return null;
        if (!AddDefinitionInternal(definition)) return null;

        if (autoBuildFilterGroupsFromItems)
            EnsureFilterGroupExists(definition.inventoryFilterGroup);

        return SpawnTemplateAndRefresh(definition);
    }

    public bool RemoveDefinition(WordBlockDefinition definition)
    {
        if (definition == null) return false;
        if (!unlockedDefinitions.Remove(definition)) return false;

        if (autoBuildFilterGroupsFromItems)
            RebuildFilterGroupsFromUnlockedItems();

        RebuildVisuals();
        return true;
    }

    public void SetFilterGroup(InventoryFilterGroup filterGroup)
    {
        activeFilterGroup = filterGroup;
        ApplyFilterAndLayout();
    }

    public void ClearFilterGroup()
    {
        activeFilterGroup = null;
        ApplyFilterAndLayout();
    }

    public void RefreshFilterGroups()
    {
        RebuildFilterGroupsFromUnlockedItems();
        ApplyFilterAndLayout();
    }

    private void RebuildVisuals()
    {
        if (contentRoot == null || wordBlockTemplatePrefab == null)
            return;

        ClearSpawnedTemplates();

        for (int i = 0; i < unlockedDefinitions.Count; i++)
        {
            SpawnTemplate(unlockedDefinitions[i]);
        }

        ApplyFilterAndLayout();
    }

    private WordBlock SpawnTemplateAndRefresh(WordBlockDefinition definition)
    {
        var block = SpawnTemplate(definition);
        ApplyFilterAndLayout();
        return block;
    }

    private WordBlock SpawnTemplate(WordBlockDefinition definition)
    {
        if (contentRoot == null || wordBlockTemplatePrefab == null || definition == null)
            return null;

        var block = Instantiate(wordBlockTemplatePrefab, contentRoot);
        block.currentSlot = null;
        block.inventoryItemType = definition.itemType;
        block.valueType = definition.valueType;
        block.value = definition.value;
        block.typeLabelOverride = definition.typeLabelOverride;
        block.inventoryFilterGroup = definition.inventoryFilterGroup;

        var drag = block.GetComponent<DraggableUI>();
        if (drag != null)
            drag.ConfigureTemplateSpawner(enabled: true, destroyCopiesOnInvalidDrop: true);

        var textView = block.GetComponent<WordBlockTextView>();
        if (textView != null)
        {
            if (definition.overrideWidthSettings)
                textView.ApplySizing(definition.minWidth, definition.maxWidth, definition.horizontalPadding);
            textView.Refresh();
        }

        spawnedTemplates.Add(block);
        return block;
    }

    private void ApplyFilterAndLayout()
    {
        visibleTemplates.Clear();

        for (int i = 0; i < spawnedTemplates.Count; i++)
        {
            var block = spawnedTemplates[i];
            if (block == null) continue;

            bool visible = activeFilterGroup == null || block.inventoryFilterGroup == activeFilterGroup;
            block.gameObject.SetActive(visible);
            if (visible)
                visibleTemplates.Add(block);
        }

        float contentWidth = LayoutVisibleTemplates();
        currentContentWidth = contentWidth;
        RefreshScroll(contentWidth);
    }

    private float LayoutVisibleTemplates()
    {
        if (contentRoot == null)
            return 0f;

        float x = 0f;

        for (int i = 0; i < visibleTemplates.Count; i++)
        {
            var block = visibleTemplates[i];
            if (block == null) continue;

            var rect = block.GetComponent<RectTransform>();
            if (rect == null) continue;

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);

            float width = rect.rect.width > 0f ? rect.rect.width : rect.sizeDelta.x;
            x += width + itemSpacing;
        }

        float contentWidth = Mathf.Max(0f, x - itemSpacing);

        if (resizeContentRootWidth)
        {
            var size = contentRoot.sizeDelta;
            size.x = contentWidth;
            contentRoot.sizeDelta = size;
        }

        return contentWidth;
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

    private void RebuildFilterGroupsFromUnlockedItems()
    {
        filterGroups.Clear();

        for (int i = 0; i < unlockedDefinitions.Count; i++)
        {
            var definition = unlockedDefinitions[i];
            if (definition == null) continue;
            EnsureFilterGroupExists(definition.inventoryFilterGroup);
        }

        if (activeFilterGroup != null && !filterGroups.Contains(activeFilterGroup))
            activeFilterGroup = null;
    }

    private void EnsureFilterGroupExists(InventoryFilterGroup group)
    {
        if (group == null) return;
        if (!filterGroups.Contains(group))
            filterGroups.Add(group);
    }

    private void ClearSpawnedTemplates()
    {
        if (clearExistingChildrenOnBuild && contentRoot != null)
        {
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
        else
        {
            for (int i = 0; i < spawnedTemplates.Count; i++)
            {
                if (spawnedTemplates[i] != null)
                    Destroy(spawnedTemplates[i].gameObject);
            }
        }

        spawnedTemplates.Clear();
        visibleTemplates.Clear();
    }

    private bool AddDefinitionInternal(WordBlockDefinition definition)
    {
        if (definition == null) return false;
        if (!allowDuplicateDefinitions && unlockedDefinitions.Contains(definition)) return false;

        unlockedDefinitions.Add(definition);
        return true;
    }
}
