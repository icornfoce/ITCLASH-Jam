using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class List : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    public List<string> Data = new List<string>();
    private int Index = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            displayText.text = Data[Index];
            Index++;
        }
    }
}
