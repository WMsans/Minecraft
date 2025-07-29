using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAround : MonoBehaviour
{
    public float distance;
    public float speed;
    public Vector3 offset;

    public float addUp;
    public float speedUp;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.I)){
            speed += 1;
        }
        if(Input.GetKeyDown(KeyCode.K)){
            speed -= 1;
        }
        transform.position = Quaternion.Euler(0, Time.time * speed, 0) * Vector3.forward * distance + offset +
                             Vector3.up * Mathf.Sin(Time.time * speedUp) * addUp;
    }
}