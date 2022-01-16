using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SemaphoreColorSystem : MonoBehaviour
{
    [SerializeField] private Material myMaterialH;
    [SerializeField] private Material myMaterialV;

    public static float timeRemaining = 5;
    public static bool flagV = false;
    public static bool flagH = false;
    private int order = 0;     // 0 flagV - 1 flagH


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
            if (flagV == flagH && order == 0)
            {
                timeRemaining = 5;
                order = 1;
                flagV = true;
                myMaterialV.color = Color.green;
            } 
            else if (flagV == flagH && order == 1)
            {
                timeRemaining = 5;
                order = 0;
                flagH = true;
                myMaterialH.color = Color.green;
            }
            else
            {
                flagH = flagV = false;  // first both the semaphores become red
                myMaterialH.color = Color.red;
                myMaterialV.color = Color.red;
                timeRemaining = 3;
            }
        }
    }
}
