using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideMouse : MonoBehaviour
{
    public bool HideTheMouse;

    // Start is called before the first frame update
    void Start()
    {
        if (HideTheMouse)
            Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}