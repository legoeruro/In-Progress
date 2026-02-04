using UnityEngine;

[CreateAssetMenu(menuName = "In Progress/Forms/Form Layout Config")]
public class FormLayoutConfig : ScriptableObject
{
    [Header("Layout Margins")]
    public Vector2 padding; // left/right, top/bottom
    public float slotSpacing; // spacing between elements

    [Header("Slot Settings")]
    public Vector2 slotSize;
    public Color slotBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [Header("Text Formatting")]
    public FormTextFormattingConfig textFormatting;
}
