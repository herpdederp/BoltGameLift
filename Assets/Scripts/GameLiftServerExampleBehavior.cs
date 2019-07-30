using UnityEngine;
using System.Collections.Generic;
using Amazon.GameLift;
using Amazon;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using System;
using UnityEngine.Networking;
using Bolt;
using UdpKit;
using System.Collections;
using Amazon.Runtime.Internal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Net.NetworkInformation;
using System.Net;

public class GameLiftServerExampleBehavior : Bolt.GlobalEventListener
{
    public string SceneToLoad;

    public Text myConsoleText;

    public string myID;

    bool started;

    [SerializeField]
    bool StartEvenIfNotHeadless;


    int listeningPort = 7777;


    IEnumerator Test0()
    {
        if (staticData.boltFree == true)
            BoltLauncher.SetUdpPlatform(new PhotonPlatform());
        BoltLauncher.StartServer(listeningPort);

        yield return null;
    }

    void Test1()
    {
        if (IsHeadlessMode() == true)
            listeningPort = int.Parse(GetArg("-p", "-port"));
        else
        {
            for (int a = 7777; a < 7799; a++)
            {
                if (PortInUse(a) == false)
                {
                    listeningPort = a;
                    break;
                }
            }



        }
    }

    public static bool PortInUse(int port)
    {
        bool inUse = false;

        IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] ipEndPoints = ipProperties.GetActiveUdpListeners();

        foreach (IPEndPoint endPoint in ipEndPoints)
        {
            if (endPoint.Port == port)
            {
                inUse = true;
                break;
            }
        }
        return inUse;
    }


    public static void LogToMyConsoleMainThread(string text)
    {
        NotAmazonUnityMainThreadDispatcher.Instance().Enqueue(
                GameLiftServerExampleBehavior.LogToMyConsoleEnum(text));
    }

    public static void LogToMyConsole(string text)
    {
        staticData.consoleText.text += "\n" + text;
        NotAmazonUnityMainThreadDispatcher.Instance().Enqueue(() => Debug.Log(text));
    }

    public static IEnumerator LogToMyConsoleEnum(string text)
    {

        LogToMyConsole(text);
        yield return null;
    }

    private void Update()
    {


        /*
        if (started == false)
            if (Input.GetKeyDown(KeyCode.E))
            {
                DoStartStuff();
            }
            */
    }


    public void DoStartStuff()
    {
        if (started == true)
        {
            LogToMyConsoleMainThread("GameLift Server Already Started");
            return;
        }

        started = true;

        LogToMyConsoleMainThread("GameLift Server Starting");




        Test1();
        Debug.Log("Port: " + listeningPort);

        //InitSDK establishes a local connection with the Amazon GameLift agent to enable 
        //further communication.
        var initSDKOutcome = GameLiftServerAPI.InitSDK();
        if (initSDKOutcome.Success)
        {
            ProcessParameters processParameters = new ProcessParameters(
                 (gameSession) =>
                 {

                     //Respond to new game session activation request. GameLift sends activation request 
                     //to the game server along with a game session object containing game properties 
                     //and other settings. Once the game server is ready to receive player connections, 
                     //invoke GameLiftServerAPI.ActivateGameSession()
                     GameLiftServerAPI.ActivateGameSession();

                     //TODO: We should call "ActivateGameSession" after Bolt is done starting
                     myID = gameSession.GameSessionId;
                     SceneToLoad = gameSession.GameSessionData;


                    
                     NotAmazonUnityMainThreadDispatcher.Instance().Enqueue(Test0());
                     //UnityMainThreadDispatcher.Instance().Enqueue(Test0());

                     LogToMyConsoleMainThread(
                         "Starting game with " + gameSession.MaximumPlayerSessionCount + " players");

                 },
                  (updateGameSession) =>
                  {
                      LogToMyConsoleMainThread("updateGameSession");

                      /*    updateGameSession.BackfillTicketId

                                MATCHMAKING_DATA_UPDATED = 0,
                                BACKFILL_FAILED = 1,
                                BACKFILL_TIMED_OUT = 2,
                                BACKFILL_CANCELLED = 3,
                                UNKNOWN = 4
                      */
                  },
                  () =>
                  {
                      //OnProcessTerminate callback. GameLift invokes this callback before shutting down 
                      //an instance hosting this game server. It gives this game server a chance to save
                      //its state, communicate with services, etc., before being shut down. 
                      //In this case, we simply tell GameLift we are indeed going to shut down.
                      GameLiftServerAPI.ProcessEnding();

                      //TODO: We should save all data and shutdown Bolt before calling "ProcessEnding" 
                  },
                () =>
                {
                    //This is the HealthCheck callback.
                    //GameLift invokes this callback every 60 seconds or so.
                    //Here, a game server might want to check the health of dependencies and such.
                    //Simply return true if healthy, false otherwise.
                    //The game server has 60 seconds to respond with its health status. 
                    //GameLift will default to 'false' if the game server doesn't respond in time.
                    //In this case, we're always healthy!
                    return true;

                    //TODO: maybe we should report unhealthy if performance is bad
                },
                //Here, the game server tells GameLift what port it is listening on for incoming player 
                //connections. In this example, the port is hardcoded for simplicity. Active game
                //that are on the same instance must have unique ports.
                listeningPort,
                new LogParameters(new List<string>()
                {
                    //Here, the game server tells GameLift what set of files to upload when the game session ends.
                    //GameLift uploads everything specified here for the developers to fetch later.
                     //TODO: put stuff in the log?
                    "C:/Users/gl-user-server/AppData/LocalLow/DefaultCompany/GameLiftTest2"
                    //"/local/game/logs/myserver.log"
                })
                );






            //Calling ProcessReady tells GameLift this game server is ready to receive incoming game sessions!
            var processReadyOutcome = GameLiftServerAPI.ProcessReady(processParameters);
            if (processReadyOutcome.Success)
            {
                LogToMyConsoleMainThread("ProcessReady success.");


            }
            else
            {
                LogToMyConsoleMainThread("ProcessReady failure : " + processReadyOutcome.Error.ToString());
            }
        }
        else
        {
            LogToMyConsoleMainThread("InitSDK failure : " + initSDKOutcome.Error.ToString());
            LogToMyConsoleMainThread("If testing locally, are you running the Server .jar?");
        }

        /*
        https://docs.aws.amazon.com/gamelift/latest/developerguide/gamelift-sdk-server-api.html#gamelift-sdk-server-initialize  
        https://docs.aws.amazon.com/gamelift/latest/developerguide/integration-server-sdk-cpp-ref-actions.html#integration-server-sdk-cpp-ref-processreadyasync 
        GameLiftServerAPI:

        AcceptPlayerSession (used)
        respond to  CreatePlayerSession() with an User ID and session ID


        ActivateGameSession (used)

        DescribePlayerSessions

        GetGameSessionId

        GetSdkVersion

        GetTerminationTime
        how much time is left to save data, move players to other game sessions

        InitSDK (used)

        ProcessEnding (used)

        ProcessReady (used)

        RemovePlayerSession (used)
        kick someone out? or when someone left
        "Notifies the Amazon GameLift service that a player with the specified 
        player session ID has disconnected from the server process. In response, 
        Amazon GameLift changes the player slot to available, 
        which allows it to be assigned to a new player."


        StartMatchBackfill

        StopMatchBackfill

        TerminateGameSession           
        "call this at the end of game session shutdown process"
        "After calling this action, the server process can call ProcessReady() 
        //to signal its availability to host a new game session. 
        //Alternatively it can call ProcessEnding() to shut down
        //the server process and terminate the instance."

        UpdatePlayerSessionCreationPolicy


        GetGameSessionLogUrl: Get logs from a game session, they are stored for 14 days


       */

    }


    //This is an example of a simple integration with GameLift server SDK that makes game server 
    //processes go active on Amazon GameLift
    public void Start()
    {



        //string IP = UnityEngine.Network.player.ipAddress;

        DontDestroyOnLoad(this.gameObject);

        //Set the port that your game service is listening on for incoming player connections (hard-coded here for simplicity)



        staticData.consoleText = myConsoleText;

        staticData.consoleText.text = "Bolt GameLift";


        bool headless = IsHeadlessMode();

        if (headless == true || StartEvenIfNotHeadless == true)
        {

            Application.targetFrameRate = 60;


            DoStartStuff();
        }
    }

    void OnApplicationQuit()
    {

        if (started == true)
            GameLiftServerAPI.Destroy();

        //Make sure to call GameLiftServerAPI.Destroy() when the application quits. 
        //This resets the local connection with GameLift's agent.
        //bool headless = IsHeadlessMode();
        //if (headless == true || StartEvenIfNotHeadless)
        //{
        //     GameLiftServerAPI.Destroy();
        // }
    }

    public override void BoltStartDone()
    {
        //bool headless = IsHeadlessMode();
        //if (headless == true || StartEvenIfNotHeadless == true)
        //{
        if (BoltNetwork.IsServer)
        {
            BoltNetwork.SetServerInfo(myID, null);

            string SceneName = "";
            if (SceneToLoad == "CubeWorld")
                SceneName = "game";
            else if (SceneToLoad == "SphereWorld")
                SceneName = "game2";

            BoltNetwork.LoadScene(SceneName);
        }
        else
        {

        }

    }

    static string GetArg(params string[] names)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            foreach (var name in names)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Utility function to detect if the game instance was started in headless mode.
    /// </summary>
    /// <returns><c>true</c>, if headless mode was ised, <c>false</c> otherwise.</returns>
    public static bool IsHeadlessMode()
    {
        return Environment.CommandLine.Contains("-batchmode") && Environment.CommandLine.Contains("-nographics");
    }





    public override void ConnectRequest(UdpEndPoint endpoint, IProtocolToken token)
    {
        //check UserID and SessionID


        TestToken myToken = (TestToken)token;



        //ask GameLift to verify sessionID is valid, it will change player slot from "RESERVED" to "ACTIVE"
        Aws.GameLift.GenericOutcome outCome = GameLiftServerAPI.AcceptPlayerSession(myToken.ArbitraryData);
        if (outCome.Success)
        {
            BoltNetwork.Accept(endpoint);
        }
        else
        {
            BoltNetwork.Refuse(endpoint);
        }

        /*
        This data type is used to specify which player session(s) to retrieve. 
        It can be used in several ways: 
        (1) provide a PlayerSessionId to request a specific player session; 
        (2) provide a GameSessionId to request all player sessions in the specified game session; or
        (3) provide a PlayerId to request all player sessions for the specified player.
        For large collections of player sessions, 
        use the pagination parameters to retrieve results as sequential pages.

    */
        var aaa = new DescribePlayerSessionsRequest()
        {
            PlayerSessionId = myToken.ArbitraryData,
            // GameSessionId = myToken.ArbitraryData,
            // PlayerId =
        };




        Aws.GameLift.DescribePlayerSessionsOutcome DPSO = GameLiftServerAPI.DescribePlayerSessions(aaa);
        string TheirPlayerId = DPSO.Result.PlayerSessions[0].PlayerId;
        Debug.Log(TheirPlayerId);

    }

    public override void Connected(BoltConnection connection)
    {
        if (BoltNetwork.IsServer)
        {
            TestToken myToken = (TestToken)connection.ConnectToken;

            connection.UserData = myToken.ArbitraryData;
        }
    }


    public override void Disconnected(BoltConnection connection)
    {
        if (BoltNetwork.IsServer)
            GameLiftServerAPI.RemovePlayerSession((string)connection.UserData);
    }

    public void LoadClientScene()
    {
        if (started == false)
            SceneManager.LoadScene("test", LoadSceneMode.Single);
    }
}
