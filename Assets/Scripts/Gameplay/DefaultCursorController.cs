using UnityEngine;

[DisallowMultipleComponent]
public class DefaultCursorController : MonoBehaviour
{
    [SerializeField] private Texture2D defaultCursorTexture;
    [SerializeField] private Vector2 hotspot;
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField] private bool applyOnAwake = true;

    private static Texture2D activeTexture;
    private static Vector2 activeHotspot;
    private static CursorMode activeMode = CursorMode.Auto;
    private static bool hasCustomDefault;

    private void Awake()
    {
        activeTexture = defaultCursorTexture;
        activeHotspot = hotspot;
        activeMode = cursorMode;
        hasCustomDefault = defaultCursorTexture != null;

        if (applyOnAwake)
            ApplyDefaultCursor();
    }

    public static void ApplyDefaultCursor()
    {
        if (hasCustomDefault)
            Cursor.SetCursor(activeTexture, activeHotspot, activeMode);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
