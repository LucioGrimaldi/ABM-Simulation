using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        for(int ATTEMPTED_SPS = 15; ATTEMPTED_SPS<=60; ATTEMPTED_SPS++)
        {
            int MAX_SPS = 60;
            int[] TOPICS = new int[MAX_SPS];

            TOPICS = new int[ATTEMPTED_SPS];

            for (int t = 1; t <= ATTEMPTED_SPS; t++)
            {
                TOPICS[t - 1] = t * MAX_SPS / (ATTEMPTED_SPS + 1);
            }

            Debug.Log(string.Join(" ", TOPICS));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
