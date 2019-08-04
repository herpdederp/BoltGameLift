using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amazon.GameLift.Model;
using System.Threading;

public class GameLiftInterface : MonoBehaviour
{
    public GameObject AdminInterfacePanel;

    public GameObject SelectGameSessionButton;

    public GameLiftClient myGameLiftClient;



    public GameObject SelectGameSessionPanel;

    public Text CreateGameSessionMaxPlayers;
    public Dropdown WorldDropDown;

    public Text SessionToFindID;

    public void DoDescribeGameSession()
    {


        Thread t = new Thread(new ParameterizedThreadStart(myGameLiftClient.DescribeGameSessions));
        t.Start(SessionToFindID.text);
    }

    public void ShutDownBolt()
    {
        BoltNetwork.Shutdown();
    }


    public void DisplaySelectGameSessionPanel()
    {

        if (myGameLiftClient.gameSessionlist == null)
            return;

        if (myGameLiftClient.gameSessionlist.GameSessions.Count == 0)
            return;

        SelectGameSessionPanel.SetActive(true);

        Transform MyImageTransform = SelectGameSessionPanel.transform.GetChild(0);

        foreach (Transform child in MyImageTransform)
        {
            GameObject.Destroy(child.gameObject);
        }



        int count = 0;
        foreach (GameSession GS in myGameLiftClient.gameSessionlist.GameSessions)
        {
            //TODO: Make this actually dictate what mode the server starts in
            string boltMode = "";
            GameProperty GP = GS.GameProperties.Find(x => x.Key == "BoltPro");
            if (GP == null)
                Debug.LogError("Missing Game Property");
            else if (GP.Value == "true")
                boltMode = "Bolt Pro";
            else if (GP.Value == "false")
                boltMode = "Bolt Free";
            else Debug.LogError("Missing Game Property Key");

            GameObject button = GameObject.Instantiate(SelectGameSessionButton, MyImageTransform);
            button.GetComponent<RectTransform>().localPosition += new Vector3(0, (count * -40f), 0);
            button.transform.GetChild(0).GetComponent<Text>().text = "Name: " + GS.Name
                + " Players: " + +GS.CurrentPlayerSessionCount + "/" + GS.MaximumPlayerSessionCount
                + " Port: " + GS.Port + " Status: " + GS.Status.ToString() + " GameSessionData: " + GS.GameSessionData
                + " " + boltMode;

            button.GetComponent<GameLiftSessionButtonController>().GameSessionId = GS.GameSessionId;
            count++;
        }

    }

    public void SelectGameSession(string gameSessionID)
    {
        bool found = false;
        foreach (GameSession GS in myGameLiftClient.gameSessionlist.GameSessions)
        {
            if (GS.GameSessionId == gameSessionID)
            {
                found = true;
                myGameLiftClient.selectedGameSession = GS;

                break;
            }
        }
        if (found == true)
            GameLiftServerExampleBehavior.LogToMyConsoleMainThread("Selected Game Session");
        else GameLiftServerExampleBehavior.LogToMyConsoleMainThread("Can't Select Game Session");

        SelectGameSessionPanel.SetActive(false);
    }


    public void DoCreatePlayerSession()
    {
        if (myGameLiftClient.selectedGameSession == null)
            GameLiftServerExampleBehavior.LogToMyConsoleMainThread("Need to Select a Game Session");
        else
            myGameLiftClient.DoCreatePlayerSession(myGameLiftClient.selectedGameSession);
    }

    public void DoCreateGameSession()
    {


        int a = WorldDropDown.value;
        string worldName = WorldDropDown.options[a].text;

        int maxPlayers = int.Parse(CreateGameSessionMaxPlayers.text);
        myGameLiftClient.DoCreateGameSession(maxPlayers, worldName);

    }

    public void ToggleBoltFreeMode()
    {
#if BOLT_CLOUD
        GameLiftServerExampleBehavior.LogToMyConsoleMainThread("Cannot toggle mode in Bolt Free");
        return;
#endif
#if !BOLT_CLOUD
        staticData.boltFree = !staticData.boltFree;
        GameLiftServerExampleBehavior.LogToMyConsoleMainThread("Bolt Free Mode: " + staticData.boltFree.ToString());
#endif
    }


    // Use this for initialization
    void Start()
    {
        staticData.gameLiftInterface = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Quote))
            AdminInterfacePanel.SetActive(!AdminInterfacePanel.activeSelf);

        if (Input.GetKeyDown(KeyCode.T))
        {

        }

    }
}
