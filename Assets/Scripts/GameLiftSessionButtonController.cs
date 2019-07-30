using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameLiftSessionButtonController : MonoBehaviour
{

    public string GameSessionId;

    // Use this for initialization
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClickButton);
    }

    void OnClickButton()
    {

        staticData.gameLiftInterface.SelectGameSession(GameSessionId);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
