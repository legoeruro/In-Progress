using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "In Progress/Forms/Form Group")]
public class FormGroup : ScriptableObject
{
    public string groupName;

    // List of form prefabs
    public List<FormDefinition> forms; 

    public List<GameFlags> flagsOff;
    public List<GameFlags> flagsOn;

    public bool IsUnlocked(GameStateManager gameStateManager)
    {
        if (gameStateManager == null)
        {
            Debug.LogWarning($"FormGroup '{name}' has no GameStateManager to evaluate flags.");
            return false;
        }

        if (flagsOn != null)
        {
            foreach (var flag in flagsOn)
            {
                if (flag == null) continue;
                if (!gameStateManager.GetFlagState(flag)) return false;
            }
        }

        if (flagsOff != null)
        {
            foreach (var flag in flagsOff)
            {
                if (flag == null) continue;
                if (gameStateManager.GetFlagState(flag)) return false;
            }
        }

        return true;
    }
}
