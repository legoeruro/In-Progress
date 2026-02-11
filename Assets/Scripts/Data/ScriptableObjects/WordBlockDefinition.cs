using UnityEngine;
using InProcess.Gameplay;

[CreateAssetMenu(menuName = "In Progress/Inventory/Word Block Definition")]
public class WordBlockDefinition : ScriptableObject
{
    [Header("Identity")]
    public string definitionId;

    [Header("Inventory + Data")]
    public ItemType itemType = ItemType.WordBlock;
    public FieldTypeData valueType;
    [TextArea] public string value;
    public InventoryFilterGroup inventoryFilterGroup;
    public bool unlockedAtStart = true;

    [Header("Text View")]
    [Tooltip("Optional text to force for the small type label on the block.")]
    public string typeLabelOverride;
    [Tooltip("If false, prefab defaults are used for block sizing.")]
    public bool overrideWidthSettings;
    public float minWidth = 110f;
    public float maxWidth = 260f;
    public float horizontalPadding = 24f;
}
