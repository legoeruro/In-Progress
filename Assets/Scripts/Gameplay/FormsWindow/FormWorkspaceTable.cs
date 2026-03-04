using UnityEngine;

public class FormWorkspaceTable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform workspaceArea;
    [SerializeField] private RectTransform workspaceContentRoot;
    [SerializeField] private Canvas canvas;

    [Header("Scaling")]
    [SerializeField] private float fullSizeScale = 1f;
    [SerializeField] private float minimizedScale = 0.5f;

    private void Awake()
    {
        if (workspaceArea == null)
            workspaceArea = transform as RectTransform;
        if (workspaceContentRoot == null)
            workspaceContentRoot = workspaceArea;
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    public void PlaceAtCenter(Form form)
    {
        if (form == null || workspaceContentRoot == null)
            return;

        var rect = form.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.SetParent(workspaceContentRoot, worldPositionStays: false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();

        ApplyScaleForPosition(rect);
    }

    public void ApplyScaleForPosition(RectTransform formRect)
    {
        if (formRect == null)
            return;

        bool insideWorkspace = IsFormCenterInside(formRect);
        float targetScale = insideWorkspace ? Mathf.Max(0.1f, fullSizeScale) : Mathf.Max(0.1f, minimizedScale);
        formRect.localScale = Vector3.one * targetScale;
    }

    private bool IsFormCenterInside(RectTransform formRect)
    {
        if (workspaceArea == null || formRect == null)
            return false;

        Vector3 worldCenter = formRect.TransformPoint(formRect.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), worldCenter);

        return RectTransformUtility.RectangleContainsScreenPoint(
            workspaceArea,
            screenPoint,
            GetEventCamera());
    }

    private Camera GetEventCamera()
    {
        if (canvas == null)
            return null;

        return canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
    }
}
