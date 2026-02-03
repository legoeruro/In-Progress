using UnityEngine;
using InProcess.Gameplay;

public class WordBlock : MonoBehaviour
{
    public FieldType valueType;
    public string value;
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
