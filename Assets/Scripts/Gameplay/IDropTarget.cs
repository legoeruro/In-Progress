using UnityEngine;
using InProcess.Gameplay;

/// <summary>
/// Interface for drop targets: when a thing is dropped onto this IDropTarget,
/// this object has specific behavior
/// </summary>
public interface IDropTarget
{
    /// <summary>
    /// Can this target interact with the DraggableUI instance
    /// </summary>
    /// <param name="draggable"></param>
    /// <returns></returns>
    bool CanAccept(DraggableUI draggable);

    /// <summary>
    /// Begin interaction with DraggableUI instance
    /// </summary>
    /// <param name="draggable"></param>
    void Accept(DraggableUI draggable);

    /// <summary>
    /// Optional for highlighting
    /// </summary>
    void OnPointerEnter() { }

    /// <summary>
    /// Optional for highlighting
    /// </summary>
    void OnPointerExit() { }
}
