using UnityEngine;

public class FormFactory : MonoBehaviour
{
    [SerializeField] private Transform formsParent; // canvas
    [SerializeField] private Form formPrefab;

    public Form Create(FormDefinition def)
    {
        var form = Instantiate(formPrefab, formsParent);
        var rect = form.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = Vector2.zero;
        form.Initialize(def);
        return form;
    }
}
