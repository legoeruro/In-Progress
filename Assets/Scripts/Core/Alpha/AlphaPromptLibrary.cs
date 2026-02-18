using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "In Progress/Forms/Alpha Prompt Library")]
public class AlphaPromptLibrary : ScriptableObject
{
    public List<AlphaPromptSpec> prompts = new List<AlphaPromptSpec>();
}
