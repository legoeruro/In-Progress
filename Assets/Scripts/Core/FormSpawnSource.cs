using System.Collections.Generic;
using UnityEngine;

public abstract class FormSpawnSource : MonoBehaviour
{
    public abstract void InitializeSource(FormManager manager, GameStateManager gameStateManager);
    public abstract IEnumerable<FormDefinition> GetInitialForms();
    public abstract FormDefinition GetNextForm();
}
