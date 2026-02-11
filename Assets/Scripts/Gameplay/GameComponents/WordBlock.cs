using UnityEngine;
using InProcess.Gameplay;

public class WordBlock : MonoBehaviour
{
    [Header("Data")]
    public ItemType inventoryItemType = ItemType.WordBlock;
    public FieldTypeData valueType;
    public string value;
    public string typeLabelOverride;
    public InventoryFilterGroup inventoryFilterGroup;
    public FillSlot currentSlot;

    public void OnUseInFillSlot()
    {
        // TODO: implement human resource machine thingy
    }

    public void OnRemoveFromFillSlot()
    {
        // TODO: destroy the instance

    }
}
