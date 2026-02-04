using UnityEngine;
using System;

[CreateAssetMenu(menuName = "In Progress/Forms/Fill Slot Data (for Forms)")]
public class FillSlotData : ScriptableObject
{
    public FieldTypeData requiredType;

    public String placeholderText = "";

    // Should we put a size onto the slot? or make it the same whatever there is

    // TODO: wishlist
    // public String hoverText;
}
