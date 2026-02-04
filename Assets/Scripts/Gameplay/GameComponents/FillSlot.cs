using UnityEngine;
public class FillSlot : MonoBehaviour, IDropTarget
{
    // TODO: use this for validation of forms
    [SerializeField] public FieldTypeData requiredType;

    public WordBlock CurrentWordBlock { get; private set; }

    public bool CanAccept(DraggableUI draggable)
    {
        // expect this draggable to be a word block. if not, do nothing.
        var wordBlock = draggable.GetComponent<WordBlock>();

        if (wordBlock == null) return false;
        return true;
    }

    public void Accept(DraggableUI draggable)
    {
        var wordBlock = draggable.GetComponent<WordBlock>();

        // clear the current word block if it exists
        if (CurrentWordBlock != null)
        {
            CurrentWordBlock.currentSlot = null;
            CurrentWordBlock.OnRemoveFromFillSlot();
        }

        CurrentWordBlock = wordBlock;
        wordBlock.currentSlot = this;
        CurrentWordBlock.OnUseInFillSlot();

        var rect = wordBlock.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchoredPosition = Vector2.zero;

        //TODO: make it so that when we drop the text block into a fillSlot, 
        // that textblock's text is truncated by the size of the slot
    }

    /// <summary>
    /// Detach wordBlock from this slot
    /// </summary>
    public void Detach()
    {
        if (CurrentWordBlock == null) return;
        CurrentWordBlock.currentSlot = null;
        CurrentWordBlock = null;
    }
}
