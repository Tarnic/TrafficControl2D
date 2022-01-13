using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SemaphoreColorSystem : MonoBehaviour
{
    [SerializeField] private Material myMaterialH;
    [SerializeField] private Material myMaterialV;

    private float timeRemaining = 5;
    private bool flag = true;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            timeRemaining = 5;
            flag = !flag;
            if (flag)
            {
                myMaterialH.color = Color.green;
                myMaterialV.color = Color.red;
            }
            else
            {
                myMaterialV.color = Color.green;
                myMaterialH.color = Color.red;
            }
        }
    }
}
