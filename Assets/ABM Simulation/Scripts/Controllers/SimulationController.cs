using Newtonsoft.Json;
using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using static SimObject;


//#### OPERATIONS_LIST ####
//OP 000 CHECK_STATUS
//OP 001 CONNECTION
//OP 002 DISCONNECTION
//OP 003 SIM_LIST_REQUEST
//OP 004 SIM_INITIALIZE
//OP 005 SIM_UPDATE
//OP 006 SIM_COMMAND
//OP 007 RESPONSE
//OP 999 CLIENT_ERROR

//#### COMMAND_LIST ####
//CMD 0 STEP
//CMD 1 PLAY
//CMD 2 PAUSE
//CMD 3 STOP
//CMD 4 CHANGE_SPEED


/// <summary>
/// Event Args Definitions
/// </summary>
public class NicknameEnterEventArgs : EventArgs
{
    public string nickname;
}
public class SimPrototypeConfirmedEventArgs : EventArgs
{
    public JSONObject sim_prototype;
}
public class SimParamsUpdateEventArgs : EventArgs
{
    public Dictionary<string, object> parameters;
}
public class SimObjectModifyEventArgs : EventArgs
{
    public SimObjectType type;
    public string class_name;
    public int id;
    public Dictionary<string, object> parameters;                                    // i parametri sono (string param_name, object value)
}
public class SimObjectCreateEventArgs : EventArgs
{
    public SimObjectType type;
    public string class_name;
    public int id;
    public ConcurrentDictionary<String, object> parameters;
}
public class SimObjectDeleteEventArgs : EventArgs
{
    public SimObjectType type;
    public string class_name;
    public int id;
}
public class ReceivedMessageEventArgs : EventArgs
{
    public MqttMsgPublishEventArgs Msg { get; set; }
    public string Sender { get; set; }
    public string Op { get; set; }
    public JSONObject Payload { get; set; }
}
public class StepMessageEventArgs : EventArgs
{
    public byte[] Step { get; set; }
}


public class SimulationController : MonoBehaviour
{
    // Player Preferences
    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;
    [SerializeField] public SimObject defaultSimObject;

    /// EVENT HANDLERS ///

    /// Queues
    public event EventHandler<ReceivedMessageEventArgs> MessageEventHandler;
    public event EventHandler<StepMessageEventArgs> StepMessageEventHandler;

    /// Messages
    public static event EventHandler<ReceivedMessageEventArgs> OnNewAdminEventHandler;

    /// Responses
    public static event EventHandler<ReceivedMessageEventArgs> OnCheckStatusSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnCheckStatusUnsuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnConnectionSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnConnectionUnsuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnDisonnectionSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnDisconnectionUnsuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnSimListSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnSimListUnsuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnSimInitSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnSimInitUnsuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnSimUpdateSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnSimUpdateUnsuccessEventHandler; 
    public static event EventHandler<ReceivedMessageEventArgs> OnSimCommandSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnSimCommandUnsuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnClientErrorSuccessEventHandler;
    public static event EventHandler<ReceivedMessageEventArgs> OnClientErrorUnsuccessEventHandler;

    /// MANAGERS ///
    private ConnectionManager ConnManager;
    public PerformanceManger PerfManager;

    /// CONTROLLERS ///
    private UIController UIController;
    private MenuController MenuController;
    private SceneController SceneController;
    public CommunicationController CommController;

    /// SIM-RELATED VARIABLES ///

    /// State
    public static int sim_id = 0;
    private StateEnum clientState = StateEnum.NOT_READY;
    private Simulation simulation;

    public static int serverSide_simId = 0;
    public static Simulation.StateEnum serverSide_simState = Simulation.StateEnum.NOT_READY;
    public enum StateEnum 
    {
        CONN_ERROR = -2,            // Error in connection
        NOT_READY = -1,             // Client is not connected
        CONNECTED_MQTT = 0,         // Client is connected to MQTT Broker
        LOGGED_IN = 1,              // Client is connected to Simulation Server
        READY = 2,                  // Client is ready to join a Simulation
        INIT = 3,                   // Client is Initializing a Simulation
        IN_GAME = 4                 // Client has created/joined a Simulation
    }
    public enum Command
    {
        STEP,
        PLAY,
        PAUSE,
        STOP,
        SPEED
    }
    public static bool admin = true;
    public static string admin_name;
    public static JSONArray sim_list_editable;
    public static JSONArray sim_prototypes_list = new JSONArray();
    private JSONObject uncommitted_updatesJSON = new JSONObject();
    public ConcurrentDictionary<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> uncommitted_updates = new ConcurrentDictionary<(string, (SimObject.SimObjectType, string, int)), SimObject>();

    /// Support variables
    
    long start_millis = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    
    private static System.Random random = new System.Random();
    private string nickname = RandomString(10);
    private long latestSimStepArrived = 0;
    private int steps_to_consume = 0;
    public int stepsConsumed = 0;

    /// Threads
    private Thread StepQueueHandlerThread;
    private Thread MessageQueueHandlerThread;
    private Thread PerformanceManagerThread;

    // SimController Action Queue
    public static readonly ConcurrentQueue<Action> SimControllerThreadQueue = new ConcurrentQueue<Action>();

    public long LatestSimStepArrived { get => latestSimStepArrived; set => latestSimStepArrived = value; }
    public int Steps_to_consume { get => steps_to_consume; set => steps_to_consume = value; }

    /// UNITY LOOP METHODS ///

    /// <summary>
    /// We use Awake to bootstrap App
    /// </summary>
    private void Awake()
    {
        Debug.developerConsoleVisible = true;

        // Instantiate Simulation
        simulation = new Simulation(defaultSimObject);

        // Retrieve Controllers/Managers
        CommController = new CommunicationController();
        MenuController = GameObject.Find("MenuController").GetComponent<MenuController>();
        PerfManager = new PerformanceManger();
        //ConnManager = new ConnectionManager(CommController, Nickname);

        // DONT DESTROY ON LOAD
        DontDestroyOnLoad(this.gameObject);

        // Bootstrack background tasks
        BootstrapControlTasks();

        clientState = StateEnum.CONNECTED_MQTT;
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    private void OnEnable()
    {
        // Register to EventHandlers
        CommunicationController.OnControlClientConnectedHandler += onControlClientConnected;
        MenuController.OnNicknameEnterEventHandler += onNicknameEnter;
        MenuController.OnSimPrototypeConfirmedEventHandler += onSimPrototypeConfirmed;
        SceneController.OnLoadSimulationSceneEventHandler += onLoadSimulationScene;
        SceneController.OnSimObjectModifyEventHandler += onSimObjectModify;
        SceneController.OnSimObjectCreateEventHandler += onSimObjectCreate;
        SceneController.OnSimObjectDeleteEventHandler += onSimObjectDelete;
        UIController.OnPlayEventHandler += onPlay;
        UIController.OnPauseEventHandler += onPause;
        UIController.OnStopEventHandler += onStop;
        UIController.OnSpeedChangeEventHandler += onSpeedChange;
        UIController.OnSimParamsUpdateEventHandler += onSimParamsUpdate;
        UIController.OnExitEventHandler += onExit;
        //UIController.OnEditExitEventHandler += onSimParamsUpdate;
        MessageEventHandler += onMessageReceived;
        StepMessageEventHandler += onStepMessageReceived;
    }
    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    private void Start()
    {
        SendCheckStatus();
    }
    /// <summary>
    /// Update routine (Unity Process)
    /// </summary>
    private void Update()
    {
        long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        if (now - start_millis > 1000) { SendCheckStatus(); start_millis = now;}

        if (!SimControllerThreadQueue.IsEmpty)
        {
            while (SimControllerThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    void OnApplicationQuit()
    {
        // do other stuff
        SendDisconnect();
        CommController.DisconnectControlClient();
        CommController.DisconnectSimulationClient();
        CommController.EmptyQueues();
        CommController.Quit();
        StopMessageQueueHandlerThread();
        StopStepQueueHandlerThread();
        StopPerformanceManagerThread();
        CommController = null;
        MenuController = null;
        PerfManager = null;
    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {
        // Unregister to EventHandlers
        CommunicationController.OnControlClientConnectedHandler -= onControlClientConnected;
        MenuController.OnNicknameEnterEventHandler -= onNicknameEnter;
        MenuController.OnSimPrototypeConfirmedEventHandler -= onSimPrototypeConfirmed;
        SceneController.OnLoadSimulationSceneEventHandler -= onLoadSimulationScene;
        SceneController.OnSimObjectModifyEventHandler -= onSimObjectModify;
        SceneController.OnSimObjectCreateEventHandler -= onSimObjectCreate;
        SceneController.OnSimObjectDeleteEventHandler -= onSimObjectDelete;
        UIController.OnPlayEventHandler -= onPlay;
        UIController.OnPauseEventHandler -= onPause;
        UIController.OnStopEventHandler -= onStop;
        UIController.OnSpeedChangeEventHandler -= onSpeedChange;
        UIController.OnSimParamsUpdateEventHandler -= onSimParamsUpdate;
        UIController.OnExitEventHandler -= onExit;
        //UIController.OnEditExitEventHandler -= onSimParamsUpdate;
        MessageEventHandler -= onMessageReceived;
        StepMessageEventHandler -= onStepMessageReceived;
    }
    /// <summary>
    /// onDestroy routine (Unity Process)
    /// </summary>
    private void OnDestroy()
    {
        
    }

    /// UTILS ///
    public Simulation GetSimulation()
    {
        return this.simulation;
    }
    public int GetSimId()
    {
        return simulation.Id;
    }
    public StateEnum GetState()
    {
        return clientState;
    }
    public Simulation.StateEnum GetSimState()
    {
        return Simulation.state;
    }
    public Simulation.SimTypeEnum GetSimType()
    {
        return simulation.Type;
    }
    public ConcurrentDictionary<string, int> GetSimDimensions()
    {
        return simulation.Dimensions;
    }
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Bootstrap background tasks
    /// </summary>
    private void BootstrapControlTasks()
    {
        CommController.StartControlClient();
        StartMessageQueueHandlerThread();
    }
    private void BootstrapSimulationTasks()
    {
        CommController.StartSimulationClient();
        StartPerformanceManagerThread();
        StartStepQueueHandlerThread();
    }

    /// Queue Handling

    /// <summary>
    /// Start steps message handler
    /// </summary>
    public void StartStepQueueHandlerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step Management Thread started..");
        StepQueueHandlerThread = new Thread(delegate () { StepQueueHandler(ref Simulation.state, ref PerformanceManger.SORTING_THRESHOLD); });
        StepQueueHandlerThread.Start();
    }
    /// <summary>
    /// Start messages handler
    /// </summary>
    public void StartMessageQueueHandlerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Message Handler Thread started..");
        MessageQueueHandlerThread = new Thread(MessageQueueHandler);
        MessageQueueHandlerThread.Start();
    }
    /// <summary>
    /// Stop steps message handler
    /// </summary>
    public void StopStepQueueHandlerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step Management Thread stopped.");
        try
        {
            StepQueueHandlerThread.Abort();
        }
        catch (ThreadAbortException e) { Debug.LogError(e); }
    }
    /// <summary>
    /// Stop messages handler
    /// </summary>
    public void StopMessageQueueHandlerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Message Handler Thread stopped.");
        try
        {
            MessageQueueHandlerThread.Abort();
        }
        catch (ThreadAbortException e) { Debug.LogError(e); }
    }
    /// <summary>
    /// Handles messages from MASON
    /// </summary>
    public void MessageQueueHandler()
    {
        JSONObject json_response, payload;
        MqttMsgPublishEventArgs msg;
        string sender, op;
        while (CommController.messageQueue == null) {}
        while (true)
        {
            if (CommController.messageQueue.TryDequeue(out msg))
            {
                json_response = (JSONObject)JSON.Parse(System.Text.Encoding.Unicode.GetString(Utils.DecompressStepPayload(msg.Message)));
                sender = json_response["sender"];
                op = json_response["op"];
                payload = (JSONObject)json_response["payload"];

                ReceivedMessageEventArgs e = new ReceivedMessageEventArgs();
                e.Msg = msg;
                e.Sender = sender;
                e.Op = op;
                e.Payload = payload;

                MessageEventHandler.BeginInvoke(this, e, null, null);
            }
        }
    }
    /// <summary>
    /// Orders steps and updates last arrived one
    /// </summary>
    public void StepQueueHandler(ref Simulation.StateEnum sim_state, ref int SORTING_THRESHOLD)
    {
        JSONObject step = new JSONObject();
        MqttMsgPublishEventArgs message;

        while (true)
        {
            if (!clientState.Equals(StateEnum.IN_GAME)) continue;
            if ((CommController.SecondaryQueue.Count > SORTING_THRESHOLD && sim_state.Equals(Simulation.StateEnum.PLAY)) || (CommController.SecondaryQueue.Count > 0 && steps_to_consume > 0))
            {
                if (!sim_state.Equals(Simulation.StateEnum.PLAY))
                {
                    while(steps_to_consume > 0)
                    {
                        try
                        {
                            StepMessageEventArgs e = new StepMessageEventArgs();
                            e.Step = CommController.SecondaryQueue.Values[0];
                            StepMessageEventHandler?.Invoke(this, e);

                            CommController.SecondaryQueue.RemoveAt(0);
                            --steps_to_consume;
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step Queue Empty!");
                            --steps_to_consume;
                        }
                    }
                }
                else
                {
                    try
                    {
                        StepMessageEventArgs e = new StepMessageEventArgs();
                        e.Step = CommController.SecondaryQueue.Values[0];
                        StepMessageEventHandler?.Invoke(this, e);

                        CommController.SecondaryQueue.RemoveAt(0);
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step Queue Empty!");
                    }
                }
            }
            if (CommController.SimMessageQueue.Count > 0)
            {
                if (CommController.SimMessageQueue.TryDequeue(out message))
                {
                    if (message.Message.Length.Equals(0))
                    {
                        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Found a blank Step");
                        continue;
                    }
                    if (Utils.GetStepId(message.Message) <= simulation.currentSimStep)
                    {
                        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Found an old Step: n° " + Utils.GetStepId(message.Message));
                        continue;
                    }
                    latestSimStepArrived = Utils.GetStepId(message.Message);
                    if(latestSimStepArrived.Equals(1))
                    {
                        StepMessageEventArgs e = new StepMessageEventArgs();
                        e.Step = message.Message;
                        StepMessageEventHandler?.Invoke(this, e);
                        ChangeState(Command.STEP);
                        --steps_to_consume;
                    }
                    else
                    {
                        CommController.SecondaryQueue.Add(latestSimStepArrived, message.Message);
                    }
                }
                else { UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Cannot Dequeue!"); }
            }
        }
    }


    /// Performance Monitor
    public void StartPerformanceManagerThread()
    {
        PerformanceManagerThread = new Thread(() => PerfManager.CalculatePerformance(CommController, ref Simulation.state, ref stepsConsumed, ref CommController.GetStepsReceived()));
        PerformanceManagerThread.Start();
    }
    public void StopPerformanceManagerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Performance Manager Thread stopped.");

        try
        {
            PerformanceManagerThread.Abort();
        }
        catch (ThreadAbortException e) { }
    }
    public void ResetPreformanceManager()
    {
        PerfManager.Reset();
    }

    /// CONTROL ///

    /// <summary>
    /// get one Step of simulation
    /// </summary>
    public void Step()
    {
        // check state
        SendSimCommand(Command.STEP, 0);
    }
    /// <summary>
    /// Play simulation
    /// </summary>
    public void Play()
    {
        // check state
        if (simulation.State != Simulation.StateEnum.PLAY && simulation.State != Simulation.StateEnum.NOT_READY)
        {
            SendSimCommand(Command.PLAY, 0);
        }
        else if(simulation.State == Simulation.StateEnum.NOT_READY)
        {
            SendSimInitialize((JSONObject)sim_list_editable[sim_id]);
            SendSimCommand(Command.PLAY, 0);
        }
    }
    /// <summary>
    /// Pause simulation
    /// </summary>
    public void Pause()
    {
        // check state
        if (simulation.State == Simulation.StateEnum.PAUSE || simulation.State == Simulation.StateEnum.STEP || simulation.State == Simulation.StateEnum.READY) { Step(); return; }
        SendSimCommand(Command.PAUSE, 0);
    }
    /// <summary>
    /// Stop simulation
    /// </summary>
    public void Stop()
    {
        // check state
        if (simulation.State == Simulation.StateEnum.READY) {return;}
        SendSimCommand(Command.STOP, 0);
    }
    /// <summary>
    /// Change simulation state
    /// </summary>
    private void ChangeState(Command command)
    {
        switch (command)
        {
            case Command.STEP:
                simulation.State = Simulation.StateEnum.STEP;
                steps_to_consume++;
                break;
            case Command.PLAY:
                simulation.State = Simulation.StateEnum.PLAY;
                break;
            case Command.PAUSE:
                simulation.State = Simulation.StateEnum.PAUSE;
                break;
            case Command.STOP:
                simulation.State = Simulation.StateEnum.NOT_READY;
                CommController.EmptyQueues();
                latestSimStepArrived = 0;
                simulation.ClearSimulation();
                SceneController.ClearSimSpace();
                break;
            case Command.SPEED:
                break;
        }
    }
    /// <summary>
    /// Change simulation speed
    /// </summary>
    public void ChangeSpeed(Simulation.SpeedEnum speed)
    {
        // check state
        if (simulation.Speed == speed) { return; }
        SendSimCommand(Command.SPEED, (int)speed);
    }


    /// SIMULATION ///
    
    /// onEvent Methods

    /// <summary>
    /// MenuController Event Handles
    /// </summary>
    private void onCheckStatus(object sender, EventArgs e)
    {
        SendCheckStatus();
    }
    private void onControlClientConnected(object sender, EventArgs e)
    {
        CommController.SubscribeTopic(nickname);
    }
    private void onNicknameEnter(object sender, NicknameEnterEventArgs e)
    {
        CommController.UnsubscribeTopic(nickname);
        nickname = e.nickname;
        CommController.SubscribeTopic(nickname);

        // TODO Check nick uniqueness
    }
    private void onSimPrototypeConfirmed(object sender, SimPrototypeConfirmedEventArgs e)
    {
        SendSimInitialize(e.sim_prototype);
    }
    private void onLoadSimulationScene(object sender, EventArgs e)
    {
        BootstrapSimulationTasks();
        clientState = StateEnum.IN_GAME;
        SceneController = GameObject.Find("SceneController").GetComponent<SceneController>();
        UIController = GameObject.Find("UIController").GetComponent<UIController>();
        while(!CommunicationController.SIM_CLIENT_READY) { }
        Step();
    }
    
    /// <summary>
    /// UIController Event Handles
    /// </summary>
    private void onPlay(object sender, EventArgs e)
    {
        Play();
    }
    private void onPause(object sender, EventArgs e)
    {
        Pause();
    }
    private void onStop(object sender, EventArgs e)
    {
        Stop();
    }
    private void onSpeedChange(object sender, SpeedChangeEventArgs e)
    {
        ChangeSpeed((Simulation.SpeedEnum)e.Speed);
    }
    private void onSimParamsUpdate(object sender, SimParamsUpdateEventArgs e)
    {
        if (simulation.State.Equals(Simulation.StateEnum.NOT_READY))
        {
            foreach (KeyValuePair<string, object> param in e.parameters)
            {
                if (param.Key.Contains("amount"))
                {
                    foreach ((int i, KeyValuePair<string, JSONNode> proto) kvp in sim_list_editable[sim_id]["agent_prototypes"].Linq.Select((p, i) => (i, p)))
                    {
                        if (kvp.proto.Value["class"].Equals(param.Key.Substring(0, param.Key.Length - 8))) sim_list_editable[sim_id]["agent_prototypes"][kvp.i]["default"] = (int)param.Value;
                    }
                }
                else if (param.Key.Equals("x") || param.Key.Equals("y") || param.Key.Equals("z"))
                {
                    sim_list_editable[sim_id]["dimensions"][param.Key.Equals("x") ? 0 : param.Key.Equals("y") ? 1 : 2]["default"] = (int)param.Value;
                }
                else
                {
                    foreach (JSONObject _param in sim_list_editable[sim_id]["sim_params"])
                    {
                        if (param.Key.Equals(_param["name"]))
                        {
                            if (param.Value.GetType().Equals(typeof(float))) _param["default"] = (float)param.Value;
                            else if (param.Value.GetType().Equals(typeof(int))) _param["default"] = (int)param.Value;
                            else if (param.Value.GetType().Equals(typeof(bool))) _param["default"] = (bool)param.Value;
                            else if (param.Value.GetType().Equals(typeof(string))) _param["default"] = (string)param.Value;
                        }
                    }
                }                
            }
            SendSimInitialize((JSONObject)sim_list_editable[sim_id]);
        }
        else
        {
            StoreSimParamsUpdateToJSON(e);
            SendSimUpdate();
        }        
    }
    private void onSimObjectModify(object sender, SimObjectModifyEventArgs e)
    {
        StoreSimObjectModify(e);
        SendSimUpdate();
    }
    private void onSimObjectCreate(object sender, SimObjectCreateEventArgs e)
    {
        StoreSimObjectCreate(e);
        SendSimUpdate();
    }
    private void onSimObjectDelete(object sender, SimObjectDeleteEventArgs e)
    {
        StoreSimObjectDelete(e);
        SendSimUpdate();
    }
    private void onExit(object sender, EventArgs e)
    {
        OnDisable();
        OnApplicationQuit();
        SimControllerThreadQueue.Enqueue(() =>
        {
            Destroy(gameObject);
        });
    }

    /// <summary>
    /// External Event Handles
    /// </summary>
    public void onMessageReceived(object sender, ReceivedMessageEventArgs e)
    {
        switch (e.Op)
        {
            case "000": // check_status
                
                // check status
                
                break;
            case "007": // response
                string response_to_op = (string)e.Payload["response_to_op"];
                switch (response_to_op)
                {
                    case "000":
                        onCheckStatusResponse(e);
                        break;
                    case "001":
                        onConnectionResponse(e);
                        break;
                    case "002":
                        onDisconnectionResponse(e);
                        break;
                    case "003":                                                           
                        onSimListResponse(e);
                        break;
                    case "004": 
                        onSimInitResponse(e);
                        break;
                    case "005":
                        onSimUpdateResponse(e);
                        break;
                    case "006":
                        onSimCommandResponse(e);
                        break;
                    case "999":
                        onClientErrorResponse(e);
                        break;
                }
                break;
            case "008": // new_admin
                onNewAdmin(e);
                break;
            case "998": // error
                
                // disconnect

                break;
            default:
                break;
        }
    }
    public void onStepMessageReceived(object sender, StepMessageEventArgs e)
    {
        try
        {
            simulation.UpdateSimulationFromStep(e.Step, (JSONObject)sim_prototypes_list[sim_id]);
            stepsConsumed++;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }

        //Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step " + Utils.GetStepId(e.Step) + " correctly updated Simulation.");
    }

    /// <summary>
    /// Derivate Event Handles
    /// </summary>
    private void onNewAdmin(ReceivedMessageEventArgs e)
    {
        admin_name = e.Payload["new_admin"];
        admin = admin_name.Equals(nickname);

        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | NEW_ADMIN MESSAGE RECEIVED. | " + (admin ? "You are" : admin_name + " is") + " the new room admin.");

        OnNewAdminEventHandler?.Invoke(this, e);
    }
    private void onCheckStatusResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];

        serverSide_simState = (Simulation.StateEnum) (int) ((JSONObject) e.Payload["payload_data"])["state"];
        if(!serverSide_simState.Equals(Simulation.StateEnum.NOT_READY) && !serverSide_simState.Equals(Simulation.StateEnum.BUSY))
        {
            admin_name = (string)((JSONObject)e.Payload["payload_data"])["adminName"];
            serverSide_simId = (int)((JSONObject)e.Payload["payload_data"])["simId"];
            PerfManager.PRODUCED_SPS = (int)((JSONObject)e.Payload["payload_data"])["simStepRate"];
            if(clientState.Equals(StateEnum.READY) || clientState.Equals(StateEnum.IN_GAME))
            {
                sim_id = serverSide_simId;
                simulation.State = serverSide_simState;
            }
        }

        //Check status for errors

        if (result) OnCheckStatusSuccessEventHandler?.Invoke(null, e);
        else OnCheckStatusUnsuccessEventHandler?.Invoke(null, e);
    }
    private void onConnectionResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];
        string nick = (string)((JSONObject)e.Payload["payload_data"])["sender"];

        if (nick.Equals(nickname))
        {
            clientState = result ? StateEnum.LOGGED_IN : StateEnum.CONN_ERROR;
            admin = (bool)((JSONObject)e.Payload["payload_data"])["isAdmin"];
            if (result) SendSimListRequest();
        }

        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | " + ((JSONObject)e.Payload["payload_data"])["sender"] + " " + (result ? "successfully" : "unsuccessfully") + " connected.");

        if (result) OnConnectionSuccessEventHandler?.Invoke(this, e);
        else OnConnectionUnsuccessEventHandler?.Invoke(this, e);
    }
    private void onDisconnectionResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];

        if(e.Payload["payload_data"]["sender"].Equals(nickname)) clientState = result ? StateEnum.NOT_READY : StateEnum.CONN_ERROR;
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | " + ((JSONObject)e.Payload["payload_data"])["sender"] + " " + (result ? "successfully" : "unsuccessfully") + " disconnected.");

        if (result) OnDisonnectionSuccessEventHandler?.Invoke(this, e);
        else OnDisconnectionUnsuccessEventHandler?.Invoke(this, e);
    }
    private void onSimListResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];
        clientState = result&&admin ? StateEnum.INIT : result ? StateEnum.READY : StateEnum.CONN_ERROR;

        if (result)
        {
            sim_prototypes_list = (JSONArray)((JSONObject)e.Payload["payload_data"])["list"];
            sim_list_editable = (JSONArray)sim_prototypes_list.Clone();
        }
        if (clientState.Equals(StateEnum.READY) && !serverSide_simState.Equals(Simulation.StateEnum.NOT_READY) && !serverSide_simState.Equals(Simulation.StateEnum.BUSY)) onJoinSimulation();

        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sim Prototypes list " + (result ? "successfully" : "unsuccessfully") + " received from " + e.Sender + ".");

        if (result) OnSimListSuccessEventHandler?.Invoke(this, e);
        else OnSimListUnsuccessEventHandler?.Invoke(this, e);
    }
    private void onSimInitResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];

        if (result) {

            if (e.Payload["payload_data"]["sender"].Equals(nickname)) simulation.InitSimulationFromPrototype((JSONObject)sim_list_editable[sim_id]);
            else
            {
                sim_id = e.Payload["payload_data"]["payload"]["id"];
                sim_list_editable[sim_id] = (JSONObject)e.Payload["payload_data"]["payload"];
                simulation.InitSimulationFromPrototype((JSONObject)e.Payload["payload_data"]["payload"]);
            }
            if (clientState.Equals(StateEnum.IN_GAME))
            {
                SimControllerThreadQueue.Enqueue(() => { SceneController.ResetSimSpace(); });
            }

            UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sim Initialization " + (result ? "confirmed" : "declined") + " by " + e.Sender + ".");

            OnSimInitSuccessEventHandler?.Invoke(this, e);
        }
        else OnSimInitUnsuccessEventHandler?.Invoke(this, e);
    }
    private void onSimUpdateResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];
        (bool, PlaceableObject) x;
        PlaceableObject temp_po;
        object value;
        object cell;

        if (result)
        {
            if (!admin)
            {
                switch (((JSONObject)e.Payload["payload_data"]["payload"]).Dict.First().Key)
                {
                    case "agents_create":
                        SimControllerThreadQueue.Enqueue(() => {
                            temp_po = SceneController.GetPlaceableObjectPrefab(SimObjectType.AGENT, (string)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["agents_create"])[0]["class"]);
                            SimObject temp = SceneController.GetSimObjectPrototype(SimObjectType.AGENT, (string)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["agents_create"])[0]["class"]);
                            temp.Parameters = new ConcurrentDictionary<string, object>(((JSONObject)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["agents_create"])[0]["params"]).Dict.ToDictionary(x => x.Key, x =>
                            {
                                JSONArray agent_prototypes = (JSONArray)sim_prototypes_list[sim_id]["agent_prototypes"];
                                foreach (JSONObject agent in agent_prototypes)
                                {
                                    if (agent["class"].Equals(temp.Class_name))
                                    {
                                        foreach (JSONObject p in (JSONArray)agent["params"])
                                        {
                                            if (p["name"].Equals(x.Key))
                                            {
                                                switch ((string)p["type"])
                                                {
                                                    case "System.Single":
                                                        value = (float)x.Value;
                                                        break;
                                                    case "System.Int32":
                                                        value = (int)x.Value;
                                                        break;
                                                    case "System.Boolean":
                                                        value = (bool)x.Value;
                                                        break;
                                                    case "System.String":
                                                        value = (string)x.Value;
                                                        break;
                                                    case "System.Position":
                                                        if (simulation.Dimensions.Count == 2) value = (Vector2)x.Value; else value = (Vector3)x.Value;
                                                        break;
                                                    case "System.Rotation":
                                                        value = (Quaternion)x.Value;
                                                        break;
                                                    case "System.Cells":
                                                        if (simulation.Dimensions.Count == 2) {
                                                            value = new MyList<Vector2Int>();
                                                            foreach(JSONObject c in x.Value.AsArray)
                                                            {
                                                                cell = new Vector2Int(c["x"].AsInt, c["y"].AsInt);
                                                                ((MyList<Vector2Int>)value).Add((Vector2Int)cell);
                                                            }
                                                        }
                                                        else {
                                                            value = new MyList<Vector3Int>();
                                                            foreach (JSONObject c in x.Value.AsArray)
                                                            {
                                                                cell = new Vector3Int(c["x"].AsInt, c["y"].AsInt, c["z"].AsInt);
                                                                ((MyList<Vector3Int>)value).Add((Vector3Int)cell);
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        value = null;
                                                        break;
                                                }
                                                return value;
                                            }
                                        }
                                    }
                                }
                                return null;
                            }));
                            SceneController.CreateGhost(temp, temp_po, false).PlaceGhost();
                        });
                        break;
                    case "agents_modify":
                        // TODO
                        break;
                    case "agents_delete":
                        SimControllerThreadQueue.Enqueue(() => {
                            SceneController.SimSpaceSystem.GetPlacedObjects().TryGetValue((SimObjectType.AGENT, ((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["agents_delete"])[0]["class"], ((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["agents_delete"])[0]["id"]), out x);
                            temp_po = x.Item2;
                            SceneController.DeletePlacedObject(temp_po);
                        });
                        break;
                    case "generics_create":
                        SimControllerThreadQueue.Enqueue(() => {
                            temp_po = SceneController.GetPlaceableObjectPrefab(SimObjectType.GENERIC, (string)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["generics_create"])[0]["class"]);
                            SimObject temp = SceneController.GetSimObjectPrototype(SimObjectType.GENERIC, (string)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["generics_create"])[0]["class"]);
                            temp.Parameters = new ConcurrentDictionary<string, object>(((JSONObject)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["generics_create"])[0]["params"]).Dict.ToDictionary(x => x.Key, x =>
                            {
                                JSONArray generic_prototypes = (JSONArray)sim_prototypes_list[sim_id]["generic_prototypes"];
                                foreach (JSONObject generic in generic_prototypes)
                                {
                                    if (generic["class"].Equals(temp.Class_name))
                                    {
                                        foreach (JSONObject p in (JSONArray)generic["params"])
                                        {
                                            if (p["name"].Equals(x.Key))
                                            {
                                                switch ((string)p["type"])
                                                {
                                                    case "System.Single":
                                                        value = (float)x.Value;
                                                        break;
                                                    case "System.Int32":
                                                        value = (int)x.Value;
                                                        break;
                                                    case "System.Boolean":
                                                        value = (bool)x.Value;
                                                        break;
                                                    case "System.String":
                                                        value = (string)x.Value;
                                                        break;
                                                    case "System.Position":
                                                        if (simulation.Dimensions.Count == 2) value = (Vector2)x.Value; else value = (Vector3)x.Value;
                                                        break;
                                                    case "System.Rotation":
                                                        value = (Quaternion)x.Value;
                                                        break;
                                                    case "System.Cells":
                                                        if (simulation.Dimensions.Count == 2)
                                                        {
                                                            value = new MyList<Vector2Int>();
                                                            foreach (JSONObject c in x.Value.AsArray)
                                                            {
                                                                cell = new Vector2Int(c["x"].AsInt, c["y"].AsInt);
                                                                ((MyList<Vector2Int>)value).Add((Vector2Int)cell);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            value = new MyList<Vector3Int>();
                                                            foreach (JSONObject c in x.Value.AsArray)
                                                            {
                                                                cell = new Vector3Int(c["x"].AsInt, c["y"].AsInt, c["z"].AsInt);
                                                                ((MyList<Vector3Int>)value).Add((Vector3Int)cell);
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        value = null;
                                                        break;
                                                }
                                                return value;
                                            }
                                        }
                                    }
                                }
                                return null;
                            })); SceneController.CreateGhost(temp, temp_po, false).PlaceGhost();
                        });
                        break;
                    case "generics_modify":
                        // TODO
                        break;
                    case "generics_delete":
                        SimControllerThreadQueue.Enqueue(() => {
                            SceneController.SimSpaceSystem.GetPlacedObjects().TryGetValue((SimObjectType.GENERIC, ((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["generics_delete"])[0]["class"], ((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["generics_delete"])[0]["id"]), out x);
                            temp_po = x.Item2;
                            SceneController.DeletePlacedObject(temp_po);
                        });
                        break;
                    case "obstacles_create":
                        SimControllerThreadQueue.Enqueue(() => {
                            temp_po = SceneController.GetPlaceableObjectPrefab(SimObjectType.OBSTACLE, (string)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["obstacles_create"])[0]["class"]);
                            SimObject temp = SceneController.GetSimObjectPrototype(SimObjectType.OBSTACLE, (string)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["obstacles_create"])[0]["class"]);
                            temp.Parameters = new ConcurrentDictionary<string, object>(((JSONObject)((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["obstacles_create"])[0]["params"]).Dict.ToDictionary(x => x.Key, x =>
                            {
                                JSONArray obstacle_prototypes = (JSONArray)sim_prototypes_list[sim_id]["obstacle_prototypes"];
                                foreach (JSONObject obstacle in obstacle_prototypes)
                                {
                                    if (obstacle["class"].Equals(temp.Class_name))
                                    {
                                        foreach (JSONObject p in (JSONArray)obstacle["params"])
                                        {
                                            if (p["name"].Equals(x.Key))
                                            {
                                                switch ((string)p["type"])
                                                {
                                                    case "System.Single":
                                                        value = (float)x.Value;
                                                        break;
                                                    case "System.Int32":
                                                        value = (int)x.Value;
                                                        break;
                                                    case "System.Boolean":
                                                        value = (bool)x.Value;
                                                        break;
                                                    case "System.String":
                                                        value = (string)x.Value;
                                                        break;
                                                    case "System.Position":
                                                        if (simulation.Dimensions.Count == 2) value = (Vector2)x.Value; else value = (Vector3)x.Value;
                                                        break;
                                                    case "System.Rotation":
                                                        value = (Quaternion)x.Value;
                                                        break;
                                                    case "System.Cells":
                                                        if (simulation.Dimensions.Count == 2)
                                                        {
                                                            value = new MyList<Vector2Int>();
                                                            foreach (JSONObject c in x.Value.AsArray)
                                                            {
                                                                cell = new Vector2Int(c["x"].AsInt, c["y"].AsInt);
                                                                ((MyList<Vector2Int>)value).Add((Vector2Int)cell);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            value = new MyList<Vector3Int>();
                                                            foreach (JSONObject c in x.Value.AsArray)
                                                            {
                                                                cell = new Vector3Int(c["x"].AsInt, c["y"].AsInt, c["z"].AsInt);
                                                                ((MyList<Vector3Int>)value).Add((Vector3Int)cell);
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        value = null;
                                                        break;
                                                }
                                                return value;
                                            }
                                        }
                                    }
                                }
                                return null;
                            })); SceneController.CreateGhost(temp, temp_po, false).PlaceGhost();
                        });
                        break;
                    case "obstacles_modify":
                        // TODO
                        break;
                    case "obstacles_delete":
                        SimControllerThreadQueue.Enqueue(() => {
                            SceneController.SimSpaceSystem.GetPlacedObjects().TryGetValue((SimObjectType.OBSTACLE, ((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["obstacles_delete"])[0]["class"], ((JSONArray)((JSONObject)e.Payload["payload_data"]["payload"])["obstacles_delete"])[0]["id"]), out x);
                            temp_po = x.Item2;
                            SceneController.DeletePlacedObject(temp_po);
                        });
                        break;
                }
            }
            if (((JSONObject)e.Payload["payload_data"]["payload"]).HasKey("sim_params"))
            {
                simulation.UpdateParamsFromJSON((JSONObject)((JSONObject)e.Payload["payload_data"]["payload"])["sim_params"]);
                foreach (var param in (JSONObject)((JSONObject)e.Payload["payload_data"]["payload"])["sim_params"])
            {
                foreach (JSONObject _param in sim_list_editable[sim_id]["sim_params"])
                {
                    if (param.Key.Equals(_param["name"]))
                    {
                        _param["default"] = param.Value;
                    }
                }
            }
            }
            uncommitted_updatesJSON.Clear();
            uncommitted_updates.Clear();
        }
        else
        {
            // TODO ERROR + QUIT
        }

        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sim Update " + (result ? "confirmed" : "declined") + " by " + e.Sender + ".");

        if (result) OnSimUpdateSuccessEventHandler?.Invoke(this, e);
        else OnSimUpdateUnsuccessEventHandler?.Invoke(this, e);
    }
    private void onSimCommandResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];
        JSONObject pd = (JSONObject)e.Payload["payload_data"];
        JSONObject payload = (JSONObject)pd["payload"];
        Command command = (Command)(int)payload["command"];

        if (result)
        {
            if (command.Equals(Command.SPEED))
            {
                Simulation.SpeedEnum value = (Simulation.SpeedEnum)((int)payload["value"]);
                simulation.speed = value;
            }
            else ChangeState(command);
        }

        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sim Command: " + command + " " + (result ? "confirmed" : "declined") + " by " + e.Sender + ".");

        if (result) OnSimCommandSuccessEventHandler?.Invoke(this, e);
        else OnSimCommandUnsuccessEventHandler?.Invoke(this, e);
    }
    private void onClientErrorResponse(ReceivedMessageEventArgs e)
    {
        bool result = e.Payload["result"];

        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Client " + (result ? "successfully" : "unsuccessfully") + " disconnected by " + e.Sender + ".");

        if (result) OnClientErrorSuccessEventHandler?.Invoke(this, e);
        else OnClientErrorUnsuccessEventHandler?.Invoke(this, e);

    }

    /// Support Methods
    private void onJoinSimulation()
    {
        Simulation.state = serverSide_simState;
        sim_id = serverSide_simId;
        // sendCheckStatus and wait for response
        simulation.InitSimulationFromPrototype((JSONObject)sim_list_editable[serverSide_simId]);
        MenuController.JoinSimulation();
    }

    /// Store Methods

    /// <summary>
    /// Store in uncommitted_updates parameter changes
    /// </summary>
    public void StoreSimObjectModify(SimObjectModifyEventArgs e)
    {
        if(e.id.Equals(-1))                                                                                      // -1 = all
        {
            foreach (KeyValuePair<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> entry in uncommitted_updates.Where(entry => (entry.Key.op.Equals("MOD") || entry.Key.op.Equals("CRT")) && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name)))
            {
                foreach(KeyValuePair<string, object> param in e.parameters)
                    {
                        if (!entry.Value.UpdateParameter(param.Key, param.Value))
                        {
                            entry.Value.AddParameter(param.Key, param.Value);
                        }
                    }                    
                
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: " + entry.Key.op + " " + e.type + "." + e.class_name + "." + entry.Key.obj.id + " updated.");
            }
            
            if (!uncommitted_updates.ContainsKey(("MOD", (e.type, e.class_name, e.id))))
            {
                SimObject a = defaultSimObject.Clone();
                a.Type = e.type;
                a.Class_name = e.class_name;
                a.Id = e.id;
            
                foreach (KeyValuePair<string, object> param in e.parameters)
                    {
                        if (!a.UpdateParameter(param.Key, param.Value))
                        {
                            a.AddParameter(param.Key, param.Value);
                        }
                    }

                uncommitted_updates.TryAdd(("MOD", (a.Type, a.Class_name, a.Id)), a);
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: MOD " + e.type + "." + e.class_name + "." + e.id + " created.");
            }
        }
        else
        {
            KeyValuePair<(string op, (SimObject.SimObjectType, string, int) obj), SimObject> entry;
            SimObject x = defaultSimObject.Clone();
            try
            {
                entry = uncommitted_updates.Single(entry => (entry.Key.op.Equals("MOD") || entry.Key.op.Equals("CRT")) && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name) && entry.Key.obj.id.Equals(e.id));
            }
            catch (InvalidOperationException)
            {
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | 0 entries for object " + e.type + "." + e.class_name + "." + e.id + ".");
            }

            if (entry.Value != null)
            {
                foreach (KeyValuePair<string, object> param in e.parameters)
                    {
                        entry.Value.UpdateParameter(param.Key, param.Value);
                    }
                
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: " + entry.Key.op + " " + e.type + "." + e.class_name + "." + e.id + " updated.");
            }
            else
            {
                switch (e.type)
                {
                    case SimObject.SimObjectType.AGENT:
                        if(simulation.Agents.ContainsKey((e.class_name, e.id)))
                        {
                            simulation.Agents.TryGetValue((e.class_name, e.id), out x);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | " + e.type + "." + e.class_name + "." + e.id + " does not exist.");
                            return;
                        }
                        break;
                    case SimObject.SimObjectType.GENERIC:
                        if (simulation.Generics.ContainsKey((e.class_name, e.id)))
                        {
                            simulation.Generics.TryGetValue((e.class_name, e.id), out x);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | " + e.type + "." + e.class_name + "." + e.id + " does not exist.");
                            return;
                        }
                        break;
                    case SimObject.SimObjectType.OBSTACLE:
                        if (simulation.Obstacles.ContainsKey((e.class_name, e.id)))
                        {
                            simulation.Obstacles.TryGetValue((e.class_name, e.id), out x);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | " + e.type + "." + e.class_name + "." + e.id + " does not exist.");
                            return;
                        }
                        break;
                }

                SimObject y = x.Clone();

                foreach (KeyValuePair<string, object> param in e.parameters)
                    {
                        y.UpdateParameter(param.Key, param.Value);
                    }

                uncommitted_updates.TryAdd(("MOD", (y.Type, y.Class_name, y.Id)), y);
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: MOD " + e.type + "." + e.class_name + "." + e.id + " created.");
            }
        }        
    }
    public void StoreSimObjectCreate(SimObjectCreateEventArgs e)
    {
        SimObject x = defaultSimObject.Clone();
        x.Type = e.type;
        x.Class_name = e.class_name;
        x.Id = e.id;
        x.Parameters = e.parameters;

        uncommitted_updates.TryAdd(("CRT", (x.Type, x.Class_name, x.Id)), x);
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: CRT " + e.type + "." + e.class_name + "." + x.Id + " created.");
    }
    public void StoreSimObjectDelete(SimObjectDeleteEventArgs e)
    {
        List<(string, (SimObject.SimObjectType, string, int))> keys_to_remove = new List<(string, (SimObject.SimObjectType, string, int))>();
        SimObject x = defaultSimObject.Clone();
        if (e.id.Equals(-1))
        {
            foreach (KeyValuePair<(string, (SimObject.SimObjectType, string, int)), SimObject> entry in uncommitted_updates.Where(entry => entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name)))
            {
                keys_to_remove.Add(entry.Key);
            }
            foreach((string op, (SimObject.SimObjectType type, string class_name, int id) obj) key in keys_to_remove)
            {
                uncommitted_updates.TryRemove(key, out _);
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: " + key.op + " " + e.type + "." + e.class_name + "." + key.obj.id + " removed.");
            }
            keys_to_remove.Clear();
        }
        else
        {
            KeyValuePair<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> entry;
            try
            {
                entry = uncommitted_updates.Single(entry => (entry.Key.op.Equals("MOD") || entry.Key.op.Equals("CRT")) && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name) && entry.Key.obj.id.Equals(e.id));
                uncommitted_updates.TryRemove(entry.Key, out _);
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: " + entry.Key.op + " " + e.type + "." + e.class_name + "." + e.id + " removed.");
            }
            catch (InvalidOperationException)
            {
                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | 0 entries for object " + e.type + "." + e.class_name + "." + e.id + ".");
            }   
        }
        if(e.id >= -1)
        {
            uncommitted_updates.TryAdd(("DEL", (e.type, e.class_name, e.id)), x);
            UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Entry: DEL " + e.type + "." + e.class_name + "." + e.id + " created.");
        }
    }

    /// <summary>
    /// Store in uncommitted_updatesJSON parameter changes
    /// </summary>
    private void StoreSimParamsUpdateToJSON(SimParamsUpdateEventArgs e)
    {
        foreach (KeyValuePair<string, object> p in e.parameters)
        {
            try
            {
                if (p.Value.GetType().Equals(typeof(float))) uncommitted_updatesJSON["sim_params"].Add(p.Key, (float)p.Value);
                else if (p.Value.GetType().Equals(typeof(int))) uncommitted_updatesJSON["sim_params"].Add(p.Key, (int)p.Value);
                else if (p.Value.GetType().Equals(typeof(bool))) uncommitted_updatesJSON["sim_params"].Add(p.Key, (bool)p.Value);
                else if (p.Value.GetType().Equals(typeof(string))) uncommitted_updatesJSON["sim_params"].Add(p.Key, (string)p.Value);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }
    private void StoreUncommittedUpdatesToJSON()
    {
        string type = "", op = "";

        foreach (KeyValuePair<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> entry in uncommitted_updates)
        {
            JSONObject obj = new JSONObject();
            JSONNode obj_params = new JSONArray();

            switch (entry.Key.obj.type)
            {
                case SimObject.SimObjectType.AGENT:
                    type = "agents";
                    break;
                case SimObject.SimObjectType.GENERIC:
                    type = "generics";
                    break;
                case SimObject.SimObjectType.OBSTACLE:
                    type = "obstacles";
                    break;
            }
            switch (entry.Key.op)
            {
                case "MOD":
                    op = "update";
                    break;
                case "CRT":
                    op = "create";
                    break;
                case "DEL":
                    op = "delete";
                    break;
            }

            if (!entry.Key.op.Equals("CRT")) obj.Add("id", entry.Key.obj.id);
            else obj.Add("quantity", 1);

            obj.Add("class", entry.Key.obj.class_name);

            if (!entry.Key.op.Equals("DEL"))
            {
                obj_params = (JSONNode)JSON.Parse(JsonConvert.SerializeObject(entry.Value.Parameters, new TupleConverter<string, float>(), new TupleConverter<string, Vector3>(), new TupleConverter<string, bool>(), new Vector2IntConverter(), new TupleConverter<string, Quaternion>(), new Vec3Conv(), new QuaternionConv()));
                obj.Add("params", obj_params);
            }
            uncommitted_updatesJSON[type+"_"+op].Add(obj);
        }
    }
   

    /// OPERATIONS ///

    /// <summary>
    /// Send CHECK STATUS message
    /// </summary>
    public void SendCheckStatus()
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("type", "heartbeat");
        // Send command
        //UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending CHECK_STATUS to MASON...");
        CommController.SendMessage(nickname, "000", payload);
        if (clientState.Equals(StateEnum.IN_GAME))
        {
            CommController.KeepAliveSimClient();
        }
    }
    /// <summary>
    /// Send connect message
    /// </summary>
    public void SendConnect()
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("admin", "true");
        payload.Add("sys_info", "...");
        // Send command
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending CONNECT to MASON...");
        CommController.SendMessage(nickname, "001", payload);
    }
    /// <summary>
    /// Send disconnect message
    /// </summary>
    public void SendDisconnect()
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("keep_on", "false");
        // Send command
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending DISCONNECT to MASON...");
        CommController.SendMessage(nickname, "002", payload);
    }
    /// <summary>
    /// Send sim list request
    /// </summary>
    public void SendSimListRequest()
    {
        // Create payload
        JSONObject payload = new JSONObject();;
        // Send command
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending SIM_LIST_REQUEST to MASON...");
        CommController.SendMessage(nickname, "003", payload);
    }
    /// <summary>
    /// Send initialization message
    /// </summary>
    public void SendSimInitialize(JSONObject sim_initialized)
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending SIM_INITIALIZE to MASON...");
        CommController.SendMessage(nickname, "004", sim_initialized);
    }
    /// <summary>
    /// Send sim update
    /// </summary>
    public void SendSimUpdate()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending SIM_UPDATE to MASON...");
        StoreUncommittedUpdatesToJSON();
        CommController.SendMessage(nickname, "005", uncommitted_updatesJSON);
    }
    /// <summary>
    /// Send sim commands
    /// </summary>
    public void SendSimCommand(Command command, int value)
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("command", (int)command);
        payload.Add("value", value);
        // Send command
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending SIM_COMMAND to MASON...");
        CommController.SendMessage(nickname, "006", payload);
    }
    /// <summary>
    /// Send generic response
    /// </summary>
    public void SendResponse(string response_to_op)
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("response_to_op", response_to_op);
        payload.Add("response", clientState.ToString());
        // Send command
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending RESPONSE to MASON...");
        CommController.SendMessage(nickname, "007", payload);
    }                              // response to CHECK_STATUS message
    /// <summary>
    /// Send an error message
    /// </summary>
    public void SendErrorMessage(string error_type, bool alive)
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("type", error_type);
        payload.Add("alive", alive);
        payload.Add("sys_info", "...");
        // Send command
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Sending CLIENT_ERROR to MASON...");
        CommController.SendMessage(nickname, "999", payload);
    }
}