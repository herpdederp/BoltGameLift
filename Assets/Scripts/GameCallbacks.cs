using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCallbacks : Bolt.GlobalEventListener
{

    public override void SceneLoadLocalDone(string scene)
    {
        BoltNetwork.Instantiate(BoltPrefabs.TestPlayer);
    }


    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
