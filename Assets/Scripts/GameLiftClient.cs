using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UdpKit;
using udpkit.platform.photon;

public class GameLiftClient : Bolt.GlobalEventListener
{
    /*
    https://docs.aws.amazon.com/gamelift/latest/developerguide/gamelift-sdk-client-api.html

    AmazonGameLiftClient: The default client configuration specifies the US East (N. Virginia) region

    


    AcceptMatch: accept/reject FlexMatch invite  
     
    CreateAlias: Creates an alias for a fleet.

    CreateBuild: Upload build (Not important here)
    
    CreateFleet: Creates a new fleet to run your game servers 
To create a new fleet, you must provide the following: 
(1) a fleet name, 
(2) an EC2 instance type and fleet type (spot or on-demand), 
(3) the build ID for your game build or script ID if using Realtime Servers, and
(4) a run-time configuration, which determines how game servers will run on each instance in the fleet.

    CreateGameSession: Creates a multiplayer game session for players. 
This action creates a game session record and assigns an available server
process in the specified fleet to host the game session. 
A fleet must have an ACTIVE status before a game session can be created in it.


    CreateGameSessionQueue: create game session with destination order and player latency policy

    DescribeGameSessionDetails

    DescribeGameSessions
        
    SearchGameSessions

    UpdateGameSession


    */
    bool localMode;

    bool clientInit;
    bool init;

    GameSession myGameSession;


    private const string m_targetFleet = "fleet-1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d";




    AmazonGameLiftClient m_Client;
    string UniqueID;
    UpdateGameSessionResponse UGSR;
    //DescribeGameSessions
    //DescribeGameSessionPlacement
    //DescribePlayerSessions
    //CreatePlayerSession
    //StartGameSessionPlacement
    ThreadStart threadStart;
    Thread myThread;

    string myGameSessionID;
    int myPort;
    string myIP;
    string myName;

    string myPlayerSessionID;


    public void ToggleLocalClientMode()
    {
        if (clientInit == true)
        {
            LogToMyConsoleMainThread("Can't change mode after client is already initialized");
            return;
        }

        localMode = !localMode;

        if (localMode == false)
            LogToMyConsoleMainThread("Client Mode: Non-Local");
        else LogToMyConsoleMainThread("Client Mode: Local");
    }



    public void DoStartBoltClient()
    {
        if (myIP == null || myPlayerSessionID == null)
        {
            LogToMyConsoleMainThread("No Player Session");
            return;
        }
        if (BoltNetwork.IsRunning)
        {
            LogToMyConsoleMainThread("Bolt is already running");
            return;
        }
        if (staticData.boltFree == true)
            BoltLauncher.SetUdpPlatform(new PhotonPlatform());
        BoltLauncher.StartClient();
    }

    void LogToMyConsoleMainThread(string text)
    {
        GameLiftServerExampleBehavior.LogToMyConsoleMainThread(text);
    }


    void Awake()
    {


#if BOLT_CLOUD
        staticData.boltFree = true;
#endif
    }


    public void DoInitClient()
    {
        if (clientInit == true)
        {
            LogToMyConsoleMainThread("Client Already Initialized");
            return;
        }


        clientInit = true;
        UniqueID = System.Guid.NewGuid().ToString();

        UnityInitializer.AttachToGameObject(gameObject);

        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;

        AmazonGameLiftConfig gameLiftConfig = new AmazonGameLiftConfig();

        if (localMode == false)
            gameLiftConfig.RegionEndpoint = RegionEndpoint.USWest2;
        else gameLiftConfig.ServiceURL = "http://localhost:9080";


       
        m_Client = new AmazonGameLiftClient(                   
                    "ACCESSKEY",
                    "SECRETKEY",
                    gameLiftConfig);

        LogToMyConsoleMainThread("Client Initialized");
    }


    public void DoDescribeGameSessions()
    {
        threadStart = new ThreadStart(DescribeGameSessions);
        myThread = new Thread(threadStart);
        myThread.Start();
    }

    public void DoCreatePlayerSession()
    {
        var threadStart2 = new ThreadStart(CreatePlayerSessionLocal);
        var myThread2 = new Thread(threadStart2);
        myThread2.Start();
    }

    public void DoCreateGameSession(int maxPlayers)
    {
        Thread t = new Thread(new ParameterizedThreadStart(CreateGameSession));
        t.Start(maxPlayers);
    }

    void CreateGameSession(object maxPlayers)
    {
        LogToMyConsoleMainThread("CreateGameSession");


        //Request must contain either GameSessionID or FleetID, but not both
        var request = new CreateGameSessionRequest()
        {
            FleetId = m_targetFleet,
            MaximumPlayerSessionCount = (int)maxPlayers,



            //GameSessionId = "gsess-abc"

        };

        CreateGameSessionResponse CGSR = null;
        try
        {
            CGSR = m_Client.CreateGameSession(request);
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        if (CGSR == null)
        {
            LogToMyConsoleMainThread("Can't create game session");
        }
        else
        {
            LogToMyConsoleMainThread("Game Session Created: " + CGSR.GameSession.GameSessionId);
        }
    }


    // Use this for initialization
    void Start()
    {

    }

    void ConnectTOLocalServer()
    {
        //CreatePlayerSession
        //BoltLauncher.StartClient();
    }

    // Update is called once per frame
    void Update()
    {
        /*
        return;
        if (init == false)
        {
            if (myThread.IsAlive == false)
            {
                init = true;
                //var threadStart2 = new ThreadStart(UpdateGameSession);
                var threadStart2 = new ThreadStart(CreatePlayerSessionLocal);
                var myThread2 = new Thread(threadStart2);
                myThread2.Start();
            }
        }
        */
    }


    public void DescribeGameSessions()
    {
        if (clientInit == false)
        {
            LogToMyConsoleMainThread("Need to Initialize Client");
            return;
        }

        LogToMyConsoleMainThread("DescribeGameSessions");

        //Request must contain either GameSessionID or FleetID, but not both
        var request = new DescribeGameSessionsRequest();

        if (localMode == false)
            request.FleetId = m_targetFleet;
        else
            request.GameSessionId = "gsess-abc";


        DescribeGameSessionsResponse gameSessionlist = null;
        try
        {
            gameSessionlist = m_Client.DescribeGameSessions(request);
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        if (gameSessionlist == null)
        {
            LogToMyConsoleMainThread("Unable to describe Game Sessions... What now?");
        }
        else
        {
            myName = gameSessionlist.GameSessions[0].Name;
            myGameSessionID = gameSessionlist.GameSessions[0].GameSessionId;
            myPort = gameSessionlist.GameSessions[0].Port;

            LogToMyConsoleMainThread("Number of Game Sessions: " + gameSessionlist.GameSessions.Count);

            foreach (GameSession GS in gameSessionlist.GameSessions)
            {
                LogToMyConsoleMainThread("Game Session ID: + " + GS.GameSessionId
                    + " Game Session Status: " + GS.StatusReason + " " + GS.StatusReason
                    + " Game Session Endpoint: " + GS.IpAddress + ":" + GS.Port
                    + " Game Session Players: " + GS.CurrentPlayerSessionCount + "/" + GS.MaximumPlayerSessionCount

                    );
            }


        }

    }



    public void FindGameSession()
    {

        Debug.Log("FindGameSessionGameLift");

        //Search for active Game sessions:
        //https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/GameLift/TSearchGameSessionsRequest.html
        var request = new SearchGameSessionsRequest()
        {
            FilterExpression = "hasAvailablePlayerSessions=true",
            FleetId = m_targetFleet,
            Limit = 20,
            //FilterExpression = "maximumSessions>=10 AND hasAvailablePlayerSessions=true" // Example filter to limit search results
        };

        SearchGameSessionsResponse gameSessionlist = null;
        try
        {
            Debug.Log("Searching for game sessions");
            gameSessionlist = m_Client.SearchGameSessions(request);
            Debug.Log("Done Searching for game sessions");
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        if (gameSessionlist == null)
        {
            Debug.Log("Unable to search for Game Sessions... What now?");
            //return null;
        }
        else
        {
            myGameSession = gameSessionlist.GameSessions[0];
            Debug.Log(gameSessionlist.GameSessions.Count + " sessions found");
            Debug.Log(gameSessionlist.GameSessions[0].CurrentPlayerSessionCount + " / " +
                gameSessionlist.GameSessions[0].MaximumPlayerSessionCount +
                " players in game session");
            //Debug.Log(gameSessionlist.GameSessions[0].

        }
        //Debug.Log(gameSessionlist.GameSessions[0].Port);

        //gameSessionlist.GameSessions[0].


        //m_Client.CreatePlayerSession()

        var request2 = new CreatePlayerSessionRequest()
        {
            GameSessionId = gameSessionlist.GameSessions[0].GameSessionId,
            PlayerData = "BoltIsBestNetworking",
            PlayerId = UniqueID
        };

        //  var response2 = m_Client.CreatePlayerSession(request2);


        CreatePlayerSessionResponse SessionResponse = null;
        try
        {
            Debug.Log("Creating player session");
            SessionResponse = m_Client.CreatePlayerSession(request2);
            Debug.Log("Done Creating player session");
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        if (SessionResponse == null)
        {
            Debug.Log("Where are my dragons???");
            //return null;
        }

        return;

    }
    void Handler(Exception exception)
    {
        Debug.Log(exception);
    }


    void UpdateGameSession()
    {
        if (myGameSession == null)
        {
            Debug.Log("1");
            return;
        }
        Debug.Log("2");

        UpdateGameSessionRequest updateGameSessionRequest = new UpdateGameSessionRequest()
        {
            GameSessionId = myGameSession.GameSessionId,
            MaximumPlayerSessionCount = 12
            // GameSessionId = gameSession.GameSessionId,
        };

        try
        {
            Debug.Log("Updating game session");
            UGSR = m_Client.UpdateGameSession(updateGameSessionRequest);
            Debug.Log("Done Updating game session");
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        if (UGSR != null)
            Debug.Log(UGSR.GameSession.MaximumPlayerSessionCount);
        //return updateGameSessionResponse.GameSession;


    }


    void CreatePlayerSessionLocal()
    {
        if (myGameSessionID == null)
        {
            LogToMyConsoleMainThread("No Game Sessions");
            return;
        }



        LogToMyConsoleMainThread("Creating Player Request. Game Session ID: " +
            myGameSessionID + " PlayerId: ");

        var request2 = new CreatePlayerSessionRequest()
        {
            GameSessionId = myGameSessionID,
            PlayerData = "dab",
            PlayerId = UniqueID
        };


        CreatePlayerSessionResponse SessionResponse = null;
        try
        {
            Debug.Log("Creating player session");
            SessionResponse = m_Client.CreatePlayerSession(request2);
            Debug.Log("Done Creating player session");
        }
        catch (Exception ex)
        {
            Handler(ex);
        }
        if (SessionResponse == null)
        {
            Debug.Log("Where are my dragons???");
            //return null;
        }
        else
        {
            myPlayerSessionID = SessionResponse.PlayerSession.PlayerSessionId;
            myIP = SessionResponse.PlayerSession.IpAddress;
            //NotAmazonUnityMainThreadDispatcher.Instance().Enqueue(Test0());


        }
    }

    public override void BoltStartDone()
    {
       
        if (BoltNetwork.IsClient)
        {
            if (staticData.boltFree == false)
            {
                TestToken token = new TestToken();
                token.ArbitraryData = myPlayerSessionID;
                UdpEndPoint endPoint = new UdpEndPoint(UdpIPv4Address.Parse(myIP), (ushort)myPort);
#if !BOLT_CLOUD
                BoltNetwork.Connect(endPoint, token);
#endif
            }
        }
    }

    public override void SessionListUpdated(Map<Guid, UdpSession> sessionList)
    {
        if (staticData.boltFree == true)
        {

            Debug.Log("number of sessions: " + BoltNetwork.SessionList.Count);
            foreach (var session in BoltNetwork.SessionList)
            {
                var photonSession = session.Value as PhotonSession;

                if (photonSession.Source == UdpSessionSource.Photon)
                {
                    if (photonSession.HostName == myName)
                    {
                        TestToken token = new TestToken();
                        token.ArbitraryData = myPlayerSessionID;
                        BoltNetwork.Connect(photonSession, token);
                    }
                }
            }
        }
    }

}
