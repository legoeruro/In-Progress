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

    [Header("Behavior")]
    [Tooltip("If true, this form should be discarded instead of submitted.")]
    public bool shouldBeDiscarded = false;

    [Header("Rewards")]
    [Tooltip("Applied when the form is received/spawned into the scene.")]
    public List<FormReward> rewardsOnReceive = new List<FormReward>();
    [Tooltip("Applied when the form is successfully submitted.")]
    public List<FormReward> rewardsOnSubmitSuccess = new List<FormReward>();
    [Tooltip("Applied when submission fails (including discard).")]
    public List<FormReward> rewardsOnSubmitFailure = new List<FormReward>();
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
