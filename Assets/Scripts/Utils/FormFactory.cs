using UnityEngine;

public class FormFactory : MonoBehaviour
{
    [SerializeField] private Transform formsParent; // canvas
    [SerializeField] private Form formPrefab;

    public Form Create(FormDefinition def)
    {
        var form = Instantiate(formPrefab, formsParent);
        form.Initialize(def);
        return form;
    }
}
