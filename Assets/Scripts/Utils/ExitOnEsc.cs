using UnityEngine;
using UnityEngine.InputSystem;

public class ExitOnEsc : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
    }
}
