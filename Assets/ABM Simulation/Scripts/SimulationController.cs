using Fixed;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using uPLibrary.Networking.M2Mqtt.Messages;

public class SimulationController : MonoBehaviour
{
    /// Simulation variables
    // Assets
    public GameObject AgentPrefab;
    private GameObject simSpace;
    private Vector3 simSpacePosition;
    private Quaternion simSpaceRotation;
    public Button button_Play;
    public Button button_Pause;
    public Button button_xStop;

    // Variables
    private List<GameObject> agents = new List<GameObject>();
    private FlockerSimulation flockSim;
    private MQTTControlClient controlClient = new MQTTControlClient();
    private MQTTSimClient simClient = new MQTTSimClient();
    private ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private SortedList<long, Vector3[]> secondaryQueue = new SortedList<long, Vector3[]>();
    private Vector3[] ready_buffer;
    private Vector3[] Ready_buffer { get => ready_buffer; set => ready_buffer = value; }
    private bool CONTROL_CLIENT_READY = false;
    private bool SIM_CLIENT_READY = false;
    private bool AGENTS_READY = false;
    private enum simStateEnum : int { 
        PLAY = 1,
        PAUSE = 2,
        STOP = 3
    }
    private int simulationState = (int)simStateEnum.STOP;

    // Settings
    private long currentSimStep = 0;
    private int flockNum = 0;
    private int deadFlockers = 0;
    private float width;
    private float height;
    private float lenght;

    public long CurrentSimStep { get => currentSimStep; set => currentSimStep = value; }
    public int FlockNum { get => flockNum; set => flockNum = value; }
    public int DeadFlockers { get => deadFlockers; set => deadFlockers = value; }
    public float Width { get => width; set => width = value; }
    public float Height { get => height; set => height = value; }
    public float Lenght { get => lenght; set => lenght = value; }

    // Benchmarking
    private int mode = 2;
    private long start_time;
    private long max_millis = 32;
    private int max_steps_wait = 3;

    public int MODE { get => mode; set => mode = value; }
    public long START_TIME { get => start_time; set => start_time = value; }
    public long MAX_MILLIS { get => max_millis; set => max_millis = value; }
    public int MAX_STEPS_WAIT { get => max_steps_wait; set => max_steps_wait = value; }

    // Load Balancing
    private int target_steps = 60;
    private float step_keep_multiplier = 1;
    private int steps_to_discard = 0;
    private int steps_to_keep = 60;
    private int still_to_discard = 0;
    private int still_to_keep = 0;

    public int Steps_to_discard { get => steps_to_discard; set => steps_to_discard = value; }
    public int Steps_to_keep { get => steps_to_keep; set => steps_to_keep = value; }
    public int Steps_discarded { get => Still_to_discard; set => Still_to_discard = value; }
    public int Steps_kept { get => Still_to_keep; set => Still_to_keep = value; }
    
    // Support
    int last_step = 0;
    Tuple<int, int, String[]> des_msg;

    public int Last_step { get => last_step; set => last_step = value; }
    public Tuple<int, int, String[]> Des_msg { get => des_msg; set => des_msg = value; }
    public int TARGET_STEPS { get => target_steps; set => target_steps = value; }
    public float Step_keep_multiplier { get => step_keep_multiplier; set => step_keep_multiplier = value; }
    public int Still_to_discard { get => still_to_discard; set => still_to_discard = value; }
    public int Still_to_keep { get => still_to_keep; set => still_to_keep = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, Vector3[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }

    long batch_flockSimStep;
    int batch_flockId;
    Vector3 position;
    Vector3[] positions;
    MemoryStream deserialize_inputStream;
    MemoryStream decompress_inputStream;
    BinaryReader deserialize_binaryReader;
    BinaryReader decompress_binaryReader;
    GZipStream gZipStream;
 
    /// <summary>
    /// Remember to call base.Awake() if you override this method.
    /// </summary>
    protected virtual void Awake()
    {
        
    }

    /// <summary>
    /// Connect to the broker using current settings.
    /// </summary>
    // Start is called before the first frame update
    private void Start()
    {
        //GUI creation
        button_Play.onClick.AddListener(() => {
            if(simulationState == (int)simStateEnum.STOP)
            {
                InstantiateAgents();
                SendSimulationSettings(flockSim.Width.ToString() + " " + flockSim.Height.ToString() + " " + flockSim.Lenght.ToString() + " " +
                flockSim.NumAgents.ToString() + " " + flockSim.SimStepRate.ToString() + " " + flockSim.SimStepDelay.ToString() + " " + flockSim.Cohesion.ToString() + " " +
                flockSim.Avoidance.ToString() + " " + flockSim.AvoidDistance.ToString() + " " + flockSim.Randomness.ToString() + " " + flockSim.Consistency.ToString() + " " +
                flockSim.Momentum.ToString() + " " + flockSim.Neighborhood.ToString() + " " + flockSim.Jump.ToString() + " " +
                flockSim.DeadAgentProbability.ToString());
                Ready_buffer = new Vector3[flockSim.NumAgents];
            }
            else if (simulationState == (int)simStateEnum.PLAY) {return;}
            ThreadPool.QueueUserWorkItem(Play);
            simulationState = (int)simStateEnum.PLAY;
        });
        button_Pause.onClick.AddListener(() => {
            ThreadPool.QueueUserWorkItem(Pause);
            simulationState = (int)simStateEnum.PAUSE;
        });
        button_xStop.onClick.AddListener(() => {
            ThreadPool.QueueUserWorkItem(Stop);
            simulationState = (int)simStateEnum.STOP;
            DestroyAgents();
            CurrentSimStep = 0;
            SimMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
            SecondaryQueue.Clear();
        });

        //Starting MQTT Clients
        Thread controlClientThread = new Thread(() => controlClient.Connect(out responseMessageQueue, out CONTROL_CLIENT_READY));
        controlClientThread.Start();

        Thread simClientThread = new Thread(() => simClient.Connect(out simMessageQueue, out SIM_CLIENT_READY));
        simClientThread.Start();
        while (!(CONTROL_CLIENT_READY && SIM_CLIENT_READY)){}
        Setup();
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateAgents();
    }
    private void Setup()
    {
        CreateSimulation();
        DoSetup();
    }
    
    private void Play(object state)
    {
        //blocco il tasto
        //devo aspettare la risposta per eventualmente sbloccare il tasto se mason non ha ricevuto il messaggio
        //sbloccare i bottoni necessari
        controlClient.Play();
    }

    public void Pause(object state)
    {
        controlClient.Pause();
    }

    public void Stop(object state)
    {
        controlClient.Stop();
    }

    public void SendSimulationSettings(string settings)
    {
        controlClient.SendSettings(settings);
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


    public void CreateSimulation()
    {
        flockSim = new FlockerSimulation(400, 400, 400, 1000, 60, 0, 1.0f, 1.0f, 10f, 1.0f, 1.0f, 1.0f, 10f, 0.7f, 0.1f);
    }

    /// <summary>
    /// Setup Simulation
    /// </summary>
    public void DoSetup()
    {
        ready_buffer = new Vector3[flockSim.NumAgents];
        positions = new Vector3[flockSim.NumAgents];
        PerformanceMonitor();
        BuildSteps();
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