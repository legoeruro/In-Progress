using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AlphaPromptSpec
{
    public string title;
    [TextArea] public string promptText;
    public FillSlotData requiredSlot;
    public float timeToCompleteSeconds;
}

public class AlphaFormDefinitionFactory : MonoBehaviour
{
    [SerializeField] private FormLayoutConfig defaultLayoutConfig;
    [SerializeField] private string fallbackTitle = "Alpha Form";

    public FormDefinition CreateRuntimeDefinition(AlphaPromptSpec spec)
    {
        if (defaultLayoutConfig == null || spec.requiredSlot == null)
            return null;

        var def = ScriptableObject.CreateInstance<FormDefinition>();
        def.name = $"RuntimeForm_{Guid.NewGuid():N}";
        def.formLayoutConfig = defaultLayoutConfig;
        def.timeToCompleteSeconds = spec.timeToCompleteSeconds;
        def.shouldBeDiscarded = false;
        def.rewardsOnReceive = new List<FormReward>();
        def.rewardsOnSubmitSuccess = new List<FormReward>();
        def.rewardsOnSubmitFailure = new List<FormReward>();

        string titleText = string.IsNullOrWhiteSpace(spec.title) ? fallbackTitle : spec.title;
        string promptText = string.IsNullOrWhiteSpace(spec.promptText) ? "Fill in the required field." : spec.promptText;

        def.contentArray = new List<FormContentField>
        {
            new FormContentField
            {
                type = FormContentType.Title,
                text = titleText,
                fillSlotData = null
            },
            new FormContentField
            {
                type = FormContentType.Text,
                text = promptText,
                fillSlotData = null
            },
            new FormContentField
            {
                type = FormContentType.FillSlot,
                text = string.Empty,
                fillSlotData = spec.requiredSlot
            }
        };

        return def;
    }
}
