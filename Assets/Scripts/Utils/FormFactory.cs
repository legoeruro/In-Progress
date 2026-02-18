using UnityEngine;

public class FormFactory : MonoBehaviour
{
    [SerializeField] private Transform formsParent; // canvas
    [SerializeField] private Form formPrefab;
    public Transform FormsParent => formsParent;

    public Form Create(FormDefinition def)
    {
        var form = Instantiate(formPrefab, formsParent);
        var rect = form.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = Vector2.zero;
        form.Initialize(def);
        return form;
    }

    public void PlaceAtCenter(Form form)
    {
        if (form == null || formsParent == null)
            return;

        var rect = form.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.SetParent(formsParent, worldPositionStays: false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();
    }
}
