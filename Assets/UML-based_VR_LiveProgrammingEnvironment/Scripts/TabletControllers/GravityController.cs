using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    private Transform startTransform;
    private float timeUpdate;

    // Start is called before the first frame update
    void Start()
    {
        startTransform = transform;
        timeUpdate = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (timeUpdate < Time.time - 5)
        {
            transform.position = startTransform.position;
            transform.rotation = startTransform.rotation;
            timeUpdate = Time.time;
        }
    }
}
