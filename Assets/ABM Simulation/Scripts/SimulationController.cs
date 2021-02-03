using Fixed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using Debug = UnityEngine.Debug;

public class SimulationController : MonoBehaviour
{
    /// UI Controller ///
    private UIController UIController;

    /// Game-related Assets ///
    public GameObject AgentPrefab;
    private GameObject simSpace;
    private Vector3 simSpacePosition;
    private Quaternion simSpaceRotation;

    /// Game-related Variables ///
    private int CONN_TIMEOUT = 5000; //millis

    /// MQTT Clients ///
    private MQTTControlClient controlClient = new MQTTControlClient();
    private MQTTSimClient simClient = new MQTTSimClient();
    /// Threads
    Thread controlClientThread, simClientThread, connectionThread;
    /// Queues
    private ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private SortedList<long, Vector3[]> secondaryQueue = new SortedList<long, Vector3[]>();

    /// Sim-related variables ///
    /// State
    private FlockerSimulation flockSim;
    public FlockerSimulation FlockSim { get => flockSim; set => flockSim = value; }

    public enum simulationState {
        CONN_ERROR = -2,            // Error in connection
        NOT_READY = -1,             // Client is not connected
        READY = 0,                  // Client is ready to play
        PLAY = 1,                   // Simulation is in PLAY
        PAUSE = 2,                  // Simulation is in PAUSE
        STOP = 3                    // Simulation is in STOP (settings not yet send)
    }
    
    private simulationState state = simulationState.NOT_READY;
    private List<GameObject> agents = new List<GameObject>();
    /// Step Buffers
    private Vector3[] ready_buffer;
    private Vector3[] Ready_buffer { get => ready_buffer; set => ready_buffer = value; }
    /// READY Bools
    private bool CONTROL_CLIENT_READY = false;
    private bool SIM_CLIENT_READY = false;
    private bool AGENTS_READY = false;
    /// Support variables
    Tuple<int, int, String[]> des_msg;
    long batch_flockSimStep;
    int batch_flockId;
    Vector3 position;
    Vector3[] positions;
    MemoryStream deserialize_inputStream;
    MemoryStream decompress_inputStream;
    BinaryReader deserialize_binaryReader;
    BinaryReader decompress_binaryReader;
    GZipStream gZipStream;
    /// Settings
    private static string SETTINGS = "0", PLAY = "1", PAUSE = "2", STOP = "3", SPEED = "4";
    private long currentSimStep = 0;
    private int flockNum = 0;
    private int deadFlockers = 0;
    private float width;
    private float height;
    private float lenght;
    private string[] variables = new string[] {"width", "height", "lenght", "numAgents", "simStepRate", "simStepDelay", "cohesion", 
                                                "avoidance", "avoidDistance" ,"randomness", "consistency",
                                                "momentum", "neighborhood", "jump", "deadAgentsProbability"};
    /// Load Balancing
    private int TARGET_FPS = 60;
    private int[] targetsArray = new int[] { 15, 30, 45, 60 };
    private int[][] topicsArray;
    private long TIMEOUT_TARGET_UP = 3000;
    private long TIMEOUT_TARGET_DOWN = 1000;
    private long timestampLastUpdate = 0;

    /// Benchmarking
    private long start_time;
    private float fps;
    private float deltaTime = 0f;

    /// Access methods ///
    public long CurrentSimStep { get => currentSimStep; set => currentSimStep = value; }
    public int FlockNum { get => flockNum; set => flockNum = value; }
    public int DeadFlockers { get => deadFlockers; set => deadFlockers = value; }
    public float Width { get => width; set => width = value; }
    public float Height { get => height; set => height = value; }
    public float Lenght { get => lenght; set => lenght = value; }
    public long START_TIME { get => start_time; set => start_time = value; }
    public Tuple<int, int, String[]> Des_msg { get => des_msg; set => des_msg = value; }
    public int Target_steps { get => TARGET_FPS; set => TARGET_FPS = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, Vector3[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }
    public simulationState State { get => state; set => state = value; }




    /// <summary>
    /// We use Awake to setup default Simulation
    /// </summary>
    protected virtual void Awake()
    {
        simSpace = GameObject.FindGameObjectWithTag("SimulationCube");
        Debug.Log(simSpace.GetComponent<Collider>().bounds.size.x);
        //Set Default Simulation and instantiate support variables
        flockSim = new FlockerSimulation(simSpace.GetComponent<Collider>().bounds.size.x*10, simSpace.GetComponent<Collider>().bounds.size.y*10, simSpace.GetComponent<Collider>().bounds.size.z*10, 1000, 60, 0, 1.0f, 1.0f, 10f, 1.0f, 1.0f, 1.0f, 10f, 0.7f, 0.1f);
        Ready_buffer = new Vector3[flockSim.NumAgents];
        positions = new Vector3[flockSim.NumAgents];
    }

    /// <summary>
    /// Connect to the broker using current settings.
    /// </summary>
    private void Start()
    {
        // Retrieve UIController
        UIController = GameObject.Find("UIController").GetComponent<UIController>();

        // Start MQTT Clients
        controlClientThread = new Thread(() => controlClient.Connect(out responseMessageQueue, out CONTROL_CLIENT_READY));
        controlClientThread.Start();

        simClientThread = new Thread(() => simClient.Connect(out simMessageQueue, out SIM_CLIENT_READY));
        simClientThread.Start();

        // Start Connection Thread
        connectionThread = new Thread(() => WaitForConnection());
        connectionThread.Start();

    }

    /// <summary>
    /// Main routine
    /// </summary>
    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;

        switch (State) {
            case simulationState.CONN_ERROR:
                // Segnalare all'utente la mancata connessione e riprovare a collegarsi
                break;
            case simulationState.NOT_READY:
                // Siamo in attesa di connessione con MASON
                break;
            case simulationState.READY:
                // Siamo pronti a visualizzare la simulazione
                break;
            case simulationState.PLAY:
                // La simulazione è in PLAY
                UpdateAgents();
                break;
            case simulationState.PAUSE:
                // La simulazione è in PAUSE
                break;
            case simulationState.STOP:
                // La simulazione è in STOP
                break;
        }
    }

    public void UpdateSettings()
    {

    }

    private void WaitForConnection()
    {
        Stopwatch stopwatch = new Stopwatch();
        long ts;
        stopwatch.Start();
        while (!(CONTROL_CLIENT_READY && SIM_CLIENT_READY)) 
        {
            stopwatch.Stop();
            ts = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();

            if (ts >= CONN_TIMEOUT)
            {
                State = simulationState.CONN_ERROR;
                stopwatch.Stop();
                return;
            }
        }
        State = simulationState.READY;
        SetupBackgroundTasks();
    }

    private void SetupBackgroundTasks()
    {        
        PerformanceMonitor();
        BuildSteps();
    }

    public void Play()
    {
        //blocco il tasto
        //devo aspettare la risposta per eventualmente sbloccare il tasto se mason non ha ricevuto il messaggio
        //sbloccare i bottoni necessari
        if (State == simulationState.STOP || State == simulationState.READY)
        {
            InstantiateAgents();
            SendSimulationSettings(false);
            Ready_buffer = new Vector3[flockSim.NumAgents];
            positions = new Vector3[flockSim.NumAgents];
        }
        else if (State == simulationState.PLAY) { return; }
        controlClient.SendCommand(PLAY);
        State = simulationState.PLAY;
    }

    public void Pause()
    {
        controlClient.SendCommand(PAUSE);
        State = simulationState.PAUSE;
    }

    public void Stop()
    {
        if (State == simulationState.STOP) {return;}
        controlClient.SendCommand(STOP);
        State = simulationState.STOP;
        DestroyAgents();
        CurrentSimStep = 0;
        MqttMsgPublishEventArgs ignored; while (simMessageQueue.TryDequeue(out ignored));
        SecondaryQueue.Clear();
    }

    public void ChangeSimulationSpeed(int speedIndicator)
    {
        controlClient.SendCommand(SPEED + ":" + speedIndicator.ToString());
    }

    public void SendSimulationSettings(bool partialUpdate)
    {
        //partialUpdate = false -> total update of settings
        //partialUpdate = true -> partial update of settings
        string settings = partialUpdate.ToString() + " " + flockSim.Width.ToString() + " " + flockSim.Height.ToString() + " " + flockSim.Lenght.ToString() + " " +
                flockSim.NumAgents.ToString() + " " + flockSim.SimStepRate.ToString() + " " + flockSim.SimStepDelay.ToString() + " " + flockSim.Cohesion.ToString() + " " +
                flockSim.Avoidance.ToString() + " " + flockSim.AvoidDistance.ToString() + " " + flockSim.Randomness.ToString() + " " + flockSim.Consistency.ToString() + " " +
                flockSim.Momentum.ToString() + " " + flockSim.Neighborhood.ToString() + " " + flockSim.Jump.ToString() + " " +
                flockSim.DeadAgentProbability.ToString();
        controlClient.SendCommand(SETTINGS + ":" + settings);
    }

    public void SendSingleSimulationSetting(string variable, string value)
    {
        string settings = "";
        for (int i = 0; i < 15; i++)
        {
            if(i == Array.IndexOf(variables, variable))
            {
                settings = settings + value;
                break;
            }
            settings = settings + " -1 ";
        }
        controlClient.SendCommand(SETTINGS + ":" + settings);
    }
    public void PerformanceMonitor()
    {
        Thread PerformanceMonitor = new Thread(this.CalculatePerformance);
        PerformanceMonitor.Start();
    }

    public void CalculatePerformance()
    {
        int[] arrayTarget60 = new int[60];
        int[] arrayTarget45 = new int[45];
        int[] arrayTarget30 = new int[30];
        int[] arrayTarget15 = new int[15];
        topicsArray = new int[][]{arrayTarget15, arrayTarget30, arrayTarget45, arrayTarget60};
        //Fill the array incrementally without repeating common values from others target arrays 
        for (int i = 0 , y = 0, z = 0, k = 0; i < TARGET_FPS; i++)
        {
            if ((i % 4) == 3)
            {
                arrayTarget60[i] = i;
            }
            if ((i % 4) == 1)
            {
                arrayTarget45[y] = i;
                y++;
            }
            
            if ((i % 4) == 2)
            {
                arrayTarget30[z] = i;
                z++;
            }
            if ((i % 4) == 0)
            {
                arrayTarget15[k] = i;
                k++;
            }
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (true)
        {
            if (TARGET_FPS < 60 && fps > targetsArray[Array.IndexOf(targetsArray, TARGET_FPS) + 1])
            {
                stopwatch.Stop();
                timestampLastUpdate = stopwatch.ElapsedMilliseconds;
                stopwatch.Start();
                if (timestampLastUpdate > TIMEOUT_TARGET_UP)
                {
                    //controllare il target attuale
                    int index = Array.IndexOf(targetsArray, TARGET_FPS);
                    TARGET_FPS = targetsArray[++index];
                    //prendiamo l'array corretto in base al target aggiornato
                    simClient.SubscribeTopics(topicsArray[index]);
                    stopwatch.Restart();
                }
            }
            else if (TARGET_FPS > 15 && fps + 1 < TARGET_FPS)
            {
                stopwatch.Stop();
                timestampLastUpdate = stopwatch.ElapsedMilliseconds;
                stopwatch.Start();
                if (timestampLastUpdate > TIMEOUT_TARGET_DOWN)
                {
                    //controllare il target attuale
                    int index = Array.IndexOf(targetsArray, TARGET_FPS);
                    TARGET_FPS = targetsArray[index-1];
                    //prendiamo l'array corretto in base al target aggiornato
                    simClient.UnsubscribeTopics(topicsArray[index]);
                    stopwatch.Restart();
                }
            }
            Thread.Sleep(500);
        }
    }

    public void BuildSteps()
    {
        Debug.Log("Building Steps..");
        Thread buildStepThread = new Thread(BuildStepBatch);
        buildStepThread.Start();
    }

    /// <summary>
    /// Batch Step Construction Coroutine
    /// </summary>
    public void BuildStepBatch()
    {
        MqttMsgPublishEventArgs step;
        Tuple<long, Vector3[]> Batch_message;

        while (true)
        {
            if (SecondaryQueue.Count > TARGET_FPS && !State.Equals(simulationState.PAUSE))
            {
                if (SecondaryQueue.Values[0] != null)
                {
                    ready_buffer = SecondaryQueue.Values[0];
                    SecondaryQueue.RemoveAt(0);
                }
                else
                {
                    Debug.LogError("Cannot get Step!");
                }
            }
            if (SimMessageQueue.Count > 0)
            {
                if (SimMessageQueue.TryDequeue(out step))
                {
                    Batch_message = DeserializeBatchMsg(DecompressData(step.Message));
                    CurrentSimStep = Batch_message.Item1;
                    SecondaryQueue.Add(CurrentSimStep, Batch_message.Item2);
                }
                else { Debug.LogError("Cannot Dequeue!"); }
            }
        }
    }

    /// <summary>
    /// Convert received string data into Vector3[]
    /// </summary>
    private Tuple<long, Vector3[]> DeserializeBatchMsg(byte[] aData)
    {
        deserialize_inputStream = new MemoryStream(aData);
        deserialize_binaryReader = new BinaryReader(deserialize_inputStream);
        batch_flockSimStep = deserialize_binaryReader.ReadInt64();

        for (int i = 0; i < flockSim.NumAgents; i++)
        {
            batch_flockId = deserialize_binaryReader.ReadInt32();
            position = new Vector3(deserialize_binaryReader.ReadSingle() / flockSim.Width - 0.5f, deserialize_binaryReader.ReadSingle() / flockSim.Height - 0.5f, deserialize_binaryReader.ReadSingle() / flockSim.Lenght - 0.5f);
            positions[batch_flockId] = position;
        }

        Tuple<long, Vector3[]> deserializedMessage = new Tuple<long, Vector3[]>(batch_flockSimStep, positions);
        deserialize_inputStream.Close();
        deserialize_binaryReader.Close();
        return deserializedMessage;
    }

    private byte[] DecompressData(byte[] payload)
    {
        decompress_inputStream = new MemoryStream(payload);
        gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress);
        decompress_binaryReader = new BinaryReader(gZipStream);

        byte[] data = decompress_binaryReader.ReadBytes(8 //Bytes for sim step num (long)
                            + (12 * flockSim.NumAgents)   //Bytes for flock positions (3 floats)
                            + (4 * flockSim.NumAgents));  //Bytes for flock id (int)

        decompress_inputStream.Close();
        gZipStream.Close();
        decompress_binaryReader.Close();

        return data;
    }

    private void InstantiateAgents()
    {
        simSpacePosition = simSpace.transform.position;
        simSpaceRotation = simSpace.transform.rotation;
        for (int i = 0; i < flockSim.NumAgents; i++)
        {
            agents.Add(Instantiate(AgentPrefab, simSpacePosition, simSpaceRotation));
            agents[i].transform.SetParent(simSpace.transform);
            
        }
        AGENTS_READY = true;
        Debug.Log("Agents Instantiated.");
    }

    private void DestroyAgents()
    {
        for (int i = 0; i < flockSim.NumAgents; i++)
        {
            Destroy(agents[i]);
        }
        agents.Clear();
        AGENTS_READY = false;
        Debug.Log("Agents Destroyed.");
    }
    
    private void UpdateAgents()
    {
        if (AGENTS_READY == true)
        {
            for (int i = 0; i < Ready_buffer.Length; i++)
            {
                var old_pos = agents[i].transform.position;
                agents[i].transform.localPosition = Ready_buffer[i];
                Vector3 velocity = agents[i].transform.position - old_pos;
                if (!velocity.Equals(Vector3.zero))
                {
                    agents[i].transform.rotation = Quaternion.Slerp(agents[i].transform.rotation, Quaternion.LookRotation(velocity, Vector3.up), 4 * Time.deltaTime);
                }
            }
        }
    }
}