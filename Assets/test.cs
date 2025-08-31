using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    int speed = 4;

    void Start()
    {
        ToMove(out speed);

        Debug.Log(speed);
    }

    private void ToMove(out int toSpeed, int doSpeed = 1)
    {
        toSpeed =+ 2;
    }

}
