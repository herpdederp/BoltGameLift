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
using System.IO;

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
    public DescribeGameSessionsResponse gameSessionlist;

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



    string myPlayerSessionID;

    public GameSession selectedGameSession;

    public int randomNumber;

    IEnumerator GetRandomNumberThousand()
    {
        randomNumber = UnityEngine.Random.Range(0, 999);
        yield return null;
    }

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
        if (selectedGameSession == null || myPlayerSessionID == null)
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
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string path = desktopPath + "\\test.txt";
   
        StreamReader reader = new StreamReader(path);
        string a = reader.ReadToEnd();
        reader.Close();
        string[] words = a.Split(' ');
        if (words.Length == 3)
        {
            staticData.myFleetID = words[0];
            staticData.awsAccessKeyId = words[1];
            staticData.awsSecretAccessKey = words[2];
        }



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
                    staticData.awsAccessKeyId,
                    staticData.awsSecretAccessKey,
                    gameLiftConfig);

        LogToMyConsoleMainThread("Client Initialized");
    }


    public void DoDescribeGameSessions()
    {


        Thread t = new Thread(new ParameterizedThreadStart(DescribeGameSessions));
        t.Start(null);
    }

    public void DoCreatePlayerSession(GameSession gameSession)
    {
        Thread t = new Thread(new ParameterizedThreadStart(CreatePlayerSessionLocal));
        t.Start(gameSession);
    }

    public void DoCreateGameSession(int maxPlayers, string GameSessionData)
    {
        CreateGameSessionData CGSD = new CreateGameSessionData();
        CGSD.maxPlayers = maxPlayers;
        CGSD.GameSessionData = GameSessionData;


        NotAmazonUnityMainThreadDispatcher.Instance().Enqueue(GetRandomNumberThousand());

        Thread t = new Thread(new ParameterizedThreadStart(CreateGameSession));
        t.Start(CGSD);
    }

    void CreateGameSession(object myCreateGameSessionData)
    {
        LogToMyConsoleMainThread("CreateGameSession");

        CreateGameSessionData CGSD = (CreateGameSessionData)myCreateGameSessionData;



        GameProperty GP0 = new GameProperty();
        GP0.Key = "BoltPro";
#if !BOLT_CLOUD
        GP0.Value = "true";
#endif

#if BOLT_CLOUD
        GP0.Value = "false";
#endif
        List<GameProperty> GPL = new List<GameProperty>();
        GPL.Add(GP0);


        //Request must contain either GameSessionID or FleetID, but not both
        var request = new CreateGameSessionRequest()
        {
            FleetId = staticData.myFleetID,
            MaximumPlayerSessionCount = CGSD.maxPlayers,
            CreatorId = UniqueID,
            GameSessionData = CGSD.GameSessionData,
            GameProperties = GPL,
            Name = "Test" + randomNumber



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




    public void DescribeGameSessions(object GameSessionID)
    {
        if (clientInit == false)
        {
            LogToMyConsoleMainThread("Need to Initialize Client");
            return;
        }


        //Request must contain either GameSessionID or FleetID, but not both
        var request = new DescribeGameSessionsRequest();

        string myGameSessionID = (string)GameSessionID;

        if (myGameSessionID != null)
        {
            request.GameSessionId = myGameSessionID;
            LogToMyConsoleMainThread("DescribeGameSession " + myGameSessionID);
        }
        else if (localMode == false)
        {
            request.FleetId = staticData.myFleetID;
            LogToMyConsoleMainThread("DescribeGameSessions for fleet " + staticData.myFleetID);
        }
        else
        {
            request.GameSessionId = "gsess-abc";
            LogToMyConsoleMainThread("Local Mode: describing default session");
            LogToMyConsoleMainThread("DescribeGameSession " + myGameSessionID);
        }

        gameSessionlist = null;
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


            LogToMyConsoleMainThread("Number of Game Sessions: " + gameSessionlist.GameSessions.Count);

            foreach (GameSession GS in gameSessionlist.GameSessions)
            {
                LogToMyConsoleMainThread("Game Session ID: " + GS.GameSessionId
                    + " Game Session Status: " + GS.Status.ToString()
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
            FleetId = staticData.myFleetID,
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


    void CreatePlayerSessionLocal(object gameSession)
    {
        if (selectedGameSession.GameSessionId == null)
        {
            LogToMyConsoleMainThread("No Game Sessions");
            return;
        }



        LogToMyConsoleMainThread("Creating Player Request. Game Session ID: " +
            selectedGameSession.GameSessionId + " PlayerId: ");

        var request2 = new CreatePlayerSessionRequest()
        {
            GameSessionId = selectedGameSession.GameSessionId,
            PlayerData = "BoltIsBestNetworking",
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
            Debug.Log("Can't Create Play Session");
        }
        else
        {
            myPlayerSessionID = SessionResponse.PlayerSession.PlayerSessionId;
            


        }
    }

    public override void BoltStartDone()
    {

        if (BoltNetwork.IsClient)
        {
            if (staticData.boltFree == false)
            {
#if !BOLT_CLOUD
                TestToken token = new TestToken();
                token.ArbitraryData = myPlayerSessionID;
                UdpEndPoint endPoint = new UdpEndPoint(UdpIPv4Address.Parse(selectedGameSession.IpAddress), (ushort)selectedGameSession.Port);
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
                    if (photonSession.HostName == selectedGameSession.GameSessionId)
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

public class CreateGameSessionData
{
    public int maxPlayers;
    public string GameSessionData;
}