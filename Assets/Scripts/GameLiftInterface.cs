using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameLiftInterface : MonoBehaviour
{
    public GameLiftClient myGameLiftClient;

    public Text CreateGameSessionMaxPlayers;

    public void DoCreateGameSession()
    {
        int maxPlayers = int.Parse(CreateGameSessionMaxPlayers.text);
        myGameLiftClient.DoCreateGameSession(maxPlayers);

    }

    public void ToggleBoltFreeMode()
    {
#if BOLT_CLOUD
        GameLiftServerExampleBehavior.LogToMyConsoleMainThread("Cannot toggle mode in Bolt Free");
        return;
#endif

        staticData.boltFree = !staticData.boltFree;
        GameLiftServerExampleBehavior.LogToMyConsoleMainThread("Bolt Free Mode: " + staticData.boltFree.ToString());
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
