using System.Collections.Generic;
using UnityEngine;

public class AlphaFormSpawnSource : FormSpawnSource
{
    [SerializeField] private AlphaFormDefinitionFactory definitionFactory;
    [SerializeField] private AlphaPromptLibrary promptLibrary;
    [SerializeField] private List<AlphaPromptSpec> fallbackPrompts = new List<AlphaPromptSpec>();
    [SerializeField] private int initialSpawnCount = 1;
    [SerializeField] private bool loopPrompts = true;

    private readonly List<int> shuffledPromptIndices = new List<int>();
    private int cycleCursor;
    private int cachedPromptCount = -1;

    public override void InitializeSource(FormManager manager, GameStateManager gameStateManager)
    {
        shuffledPromptIndices.Clear();
        cycleCursor = 0;
        cachedPromptCount = -1;
    }

    public override IEnumerable<FormDefinition> GetInitialForms()
    {
        int count = Mathf.Max(0, initialSpawnCount);
        for (int i = 0; i < count; i++)
        {
            var def = GetNextForm();
            if (def == null) yield break;
            yield return def;
        }
    }

    public override FormDefinition GetNextForm()
    {
        var prompts = GetPromptSource();
        if (definitionFactory == null || prompts == null || prompts.Count == 0)
            return null;

        EnsureShuffledCycle(prompts.Count);
        if (cycleCursor >= shuffledPromptIndices.Count)
        {
            if (!loopPrompts)
                return null;

            ShuffleCurrentCycle();
            cycleCursor = 0;
        }

        int promptIndex = shuffledPromptIndices[cycleCursor];
        cycleCursor++;
        var spec = prompts[promptIndex];
        return definitionFactory.CreateRuntimeDefinition(spec);
    }

    private List<AlphaPromptSpec> GetPromptSource()
    {
        if (promptLibrary != null && promptLibrary.prompts != null && promptLibrary.prompts.Count > 0)
            return promptLibrary.prompts;

        return fallbackPrompts;
    }

    private void EnsureShuffledCycle(int promptCount)
    {
        if (cachedPromptCount == promptCount && shuffledPromptIndices.Count == promptCount)
            return;

        cachedPromptCount = promptCount;
        shuffledPromptIndices.Clear();
        for (int i = 0; i < promptCount; i++)
            shuffledPromptIndices.Add(i);

        ShuffleCurrentCycle();
        cycleCursor = 0;
    }

    private void ShuffleCurrentCycle()
    {
        for (int i = shuffledPromptIndices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = shuffledPromptIndices[i];
            shuffledPromptIndices[i] = shuffledPromptIndices[j];
            shuffledPromptIndices[j] = temp;
        }
    }
}
