using UnityEngine;
using InProcess.Gameplay;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Fill Slot Data (for Forms)")]
public class FillSlotData : ScriptableObject
{
    public FieldType requiredType;

    public String placeholderText = "";

    // Should we put a size onto the slot? or make it the same whatever there is

    // TODO: wishlist
    // public String hoverText;
}
