using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PrintPlayerName : MonoBehaviour
{
    private TextMeshPro nameText;

    private void Awake()
    {
        nameText = GetComponent<TextMeshPro>();
    }

    public void SetName(string name)
    {
        nameText.text = name;
    }

    private void Update()
    {
        Camera cam = Camera.main;

        transform.LookAt(cam.transform.position);
        transform.Rotate(0, 180, 0);
    }

}
