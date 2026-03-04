using TMPro;
using UnityEngine;

public class FormInboxQueueCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text countText;
    [SerializeField] private string countFormat = "{0}";
    [SerializeField] private bool hideWhenZero = true;

    public void SetCount(int count)
    {
        int safeCount = Mathf.Max(0, count);

        if (countText != null)
            countText.text = string.Format(countFormat, safeCount);

        if (hideWhenZero)
            gameObject.SetActive(safeCount > 0);
    }
}
