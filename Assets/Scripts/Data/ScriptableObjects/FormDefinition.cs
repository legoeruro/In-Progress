using UnityEngine;
using InProcess.Gameplay;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "In Progress/Forms/Form Definition")]
public class FormDefinition : ScriptableObject
{
    /// <summary>
    /// Contains text AND information for where to put fill slots.
    /// </summary>
    public List<FormContentField> contentArray;
    public FormLayoutConfig formLayoutConfig;

    [Header("Timing")]
    [Tooltip("Time allowed to complete this form")]
    public float timeToCompleteSeconds = -1f;
}

[System.Serializable]
public struct FormContentField
{
    public FormContentType type;

    // if type is text
    public String text;

    // if type is FillSlot
    public FillSlotData fillSlotData;
}

public enum FormContentType
{
    Title, H1, H2, H3, Text, FillSlot
}
