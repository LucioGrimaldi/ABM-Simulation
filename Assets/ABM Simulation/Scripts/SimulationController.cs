using Fixed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;
using UnityEditor.Profiling;
using uPLibrary.Networking.M2Mqtt.Messages;
using Debug = UnityEngine.Debug;
using UnityEditorInternal;

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
    private enum simulationState {
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
    int last_step = 0;
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
    private static string SETTINGS = "0", PLAY = "1", PAUSE = "2", STOP = "3";
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
    private int target_steps = 60;
    private float step_keep_multiplier = 1;
    private int steps_to_discard = 0;
    private int steps_to_keep = 60;
    private int still_to_discard = 0;
    private int still_to_keep = 0;
    /// Benchmarking
    private int mode = 2;
    private long start_time;
    private long max_millis = 32;
    private int max_steps_wait = 3;

    /// Access methods ///
    public long CurrentSimStep { get => currentSimStep; set => currentSimStep = value; }
    public int FlockNum { get => flockNum; set => flockNum = value; }
    public int DeadFlockers { get => deadFlockers; set => deadFlockers = value; }
    public float Width { get => width; set => width = value; }
    public float Height { get => height; set => height = value; }
    public float Lenght { get => lenght; set => lenght = value; }
    public int MODE { get => mode; set => mode = value; }
    public long START_TIME { get => start_time; set => start_time = value; }
    public long MAX_MILLIS { get => max_millis; set => max_millis = value; }
    public int MAX_STEPS_WAIT { get => max_steps_wait; set => max_steps_wait = value; }
    public int Steps_to_discard { get => steps_to_discard; set => steps_to_discard = value; }
    public int Steps_to_keep { get => steps_to_keep; set => steps_to_keep = value; }
    public int Steps_discarded { get => Still_to_discard; set => Still_to_discard = value; }
    public int Steps_kept { get => Still_to_keep; set => Still_to_keep = value; }
    public int Last_step { get => last_step; set => last_step = value; }
    public Tuple<int, int, String[]> Des_msg { get => des_msg; set => des_msg = value; }
    public int TARGET_STEPS { get => target_steps; set => target_steps = value; }
    public float Step_keep_multiplier { get => step_keep_multiplier; set => step_keep_multiplier = value; }
    public int Still_to_discard { get => still_to_discard; set => still_to_discard = value; }
    public int Still_to_keep { get => still_to_keep; set => still_to_keep = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, Vector3[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }

 
    /// <summary>
    /// We use Awake to setup default Simulation
    /// </summary>
    protected virtual void Awake()
    {
        //Set Default Simulation and instantiate support variables
        simSpace = GameObject.FindGameObjectWithTag("SimulationCube");
        flockSim = new FlockerSimulation(simSpace.GetComponent<Collider>().bounds.size.x *10, simSpace.GetComponent<Collider>().bounds.size.y*10, simSpace.GetComponent<Collider>().bounds.size.z*10, 1000, 60, 0, 1.0f, 1.0f, 10f, 1.0f, 1.0f, 1.0f, 10f, 0.7f, 0.1f);
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

    public static void GetEngineStats()
    {

    }
    /// <summary>
    /// Main routine
    /// </summary>
    private void Update()
    {

            switch (state) {
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
        for (int threadIndex = 0; ; ++threadIndex)
        {
            using (RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(Time.frameCount, threadIndex))
            {
                //Debug.Log("Time.frameCount: " + Time.frameCount);
                if (frameData.valid)
                    Debug.Log("FrameGpuTime: " + frameData.frameGpuTimeMs);
            }
        }
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
                state = simulationState.CONN_ERROR;
                stopwatch.Stop();
                return;
            }
        }
        state = simulationState.READY;
        SetupBackgroundTasks();
    }

    private void SetupBackgroundTasks()
    {        
        PerformanceMonitor();
        BuildSteps();
    }

    private void UpdateSettings()
    {
        ready_buffer = new Vector3[flockSim.NumAgents];
    }

    public void Play()
    {
        //blocco il tasto
        //devo aspettare la risposta per eventualmente sbloccare il tasto se mason non ha ricevuto il messaggio
        //sbloccare i bottoni necessari
        if (state == simulationState.STOP || state == simulationState.READY)
        {
            InstantiateAgents();
            SendSimulationSettings();
            Ready_buffer = new Vector3[flockSim.NumAgents];
            positions = new Vector3[flockSim.NumAgents];
        }
        else if (state == simulationState.PLAY) { return; }
        controlClient.SendCommand(PLAY);
        state = simulationState.PLAY;
    }

    public void Pause()
    {
        controlClient.SendCommand(PAUSE);
        state = simulationState.PAUSE;
    }

    public void Stop()
    {
        if (state == simulationState.STOP) {return;}
        controlClient.SendCommand(STOP);
        state = simulationState.STOP;
        DestroyAgents();
        CurrentSimStep = 0;
        MqttMsgPublishEventArgs ignored; while (simMessageQueue.TryDequeue(out ignored));
        SecondaryQueue.Clear();
    }

    public void SendSimulationSettings()
    {
        string settings = "false " + flockSim.Width.ToString() + " " + flockSim.Height.ToString() + " " + flockSim.Lenght.ToString() + " " +
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
        while (true)
        {
            int queue_count = SimMessageQueue.Count;
            if (queue_count > 0)
            {
                int new_queue_count = SimMessageQueue.Count;
                if (new_queue_count - queue_count > 0 && new_queue_count > TARGET_STEPS)
                {
                    if (!(Step_keep_multiplier <= 0.1f))
                    {
                        Step_keep_multiplier -= 0.1f;
                        Steps_to_keep = (int)Math.Round(TARGET_STEPS * Step_keep_multiplier);
                        Steps_to_discard = TARGET_STEPS - Steps_to_keep;
                        if (Steps_to_keep > Steps_to_discard && Steps_to_discard > 0)
                        {
                            Steps_to_keep = (int)Math.Round((float)Steps_to_keep / (float)Steps_to_discard);
                            Steps_to_discard = 1;
                        }
                        else if (Steps_to_discard < Steps_to_keep && Steps_to_keep > 0)
                        {
                            Steps_to_discard = (int)Math.Round((float)Steps_to_discard / (float)Steps_to_keep);
                            Steps_to_keep = 1;
                        }
                    }
                }
                else if (new_queue_count - queue_count < 0)
                {
                    if (!(Step_keep_multiplier >= 1))
                    {
                        Step_keep_multiplier += 0.01f;
                        Steps_to_keep = (int)Math.Round(TARGET_STEPS * Step_keep_multiplier);
                        Steps_to_discard = TARGET_STEPS - Steps_to_keep;
                        if (Steps_to_keep > Steps_to_discard && Steps_to_discard > 0)
                        {
                            Steps_to_keep = (int)Math.Round((float)Steps_to_keep / (float)Steps_to_discard);
                            Steps_to_discard = 1;
                        }
                        else if (Steps_to_discard < Steps_to_keep && Steps_to_keep > 0)
                        {
                            Steps_to_discard = (int)Math.Round((float)Steps_to_discard / (float)Steps_to_keep);
                            Steps_to_keep = 1;
                        }
                    }
                }
                //logica dei topic
            }
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
        int THRESHOLD = 60;
        MqttMsgPublishEventArgs step;
        Tuple<long, Vector3[]> Batch_message;

        while (true)
        {
            if (SecondaryQueue.Count > THRESHOLD)
            {
                if (SecondaryQueue.TryGetValue(CurrentSimStep, out ready_buffer))
                {
                    SecondaryQueue.Remove(CurrentSimStep);
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
        simSpace = GameObject.FindGameObjectWithTag("SimulationCube");
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
                //TO DO
                //Calculate quaternion
                agents[i].transform.localPosition = Ready_buffer[i];
            }
        }
    }
}