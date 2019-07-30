using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayerController : Bolt.EntityEventListener<ITestPlayer>
{
    //test player controller

    public override void Attached()
    {
        state.SetTransforms(state.transform, transform);
    }


    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (entity.IsOwner)
        {
            if (Input.GetKey(KeyCode.W))
            {
                transform.Translate(Vector3.forward * Time.deltaTime * 10f);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                transform.Translate(Vector3.back * Time.deltaTime * 10f);
            }

            if (Input.GetKey(KeyCode.A))
            {
                transform.Translate(Vector3.left * Time.deltaTime * 10f);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                transform.Translate(Vector3.right * Time.deltaTime * 10f);
            }


        }
    }
}
