using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class HoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Cursor")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotspot;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cursorTexture != null)
            Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearCursor();
    }

    private void OnDisable()
    {
        ClearCursor();
    }

    private void ClearCursor()
    {
        DefaultCursorController.ApplyDefaultCursor();
    }
}
