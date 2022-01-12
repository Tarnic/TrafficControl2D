using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SemaphoreColorH : MonoBehaviour
{
    [SerializeField] private Material myMaterial;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        char sec = DateTime.Now.ToString("ss")[1];

        if (sec.CompareTo('5') < 0)
        {
            myMaterial.color = Color.red;
        }
        else
        {
            myMaterial.color = Color.green;
        }
        
    }
}
