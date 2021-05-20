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
using SimpleJSON;

public class SimulationController : MonoBehaviour
{
    /// Controllers ///
    private UIController UIController;
    private CommunicationController CommController;

    /// Game-related Assets ///
    private GameObject simSpace; //Game controller
    private Vector3 simSpacePosition;//Game controller
    private Quaternion simSpaceRotation;//Game controller

    /// Game-related Variables ///
    private int CONN_TIMEOUT = 5000; //millis

    /// Threads
    Thread connectionThread, buildStepThread;
    /// Queues
    private ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private SortedList<long, Vector3[]> secondaryQueue = new SortedList<long, Vector3[]>();

    /// Sim-related variables ///
    /// State
    private Simulation simulation;
    public Simulation Simulation { get => simulation; set => simulation = value; }

    public enum State 
    {
        CONN_ERROR = -2,            // Error in connection
        NOT_READY = -1,             // Client is not connected
        READY = 0,                  // Client is ready to play
    }
    
    private State state = State.NOT_READY;
    private JSONObject update;

    /// Step Buffers
    private Tuple<long, Vector3[]> ready_buffer;
    private Tuple<long, Vector3[]> Ready_buffer { get => ready_buffer; set => ready_buffer = value; }
    /// READY Bools
    private bool CONTROL_CLIENT_READY = false;
    private bool SIM_CLIENT_READY = false;
    private bool AGENTS_READY = false;//game controller
    /// Support variables
    private Tuple<int, int, String[]> des_msg;
    private long batch_flockSimStep;
    private int batch_flockId;
    private Vector3 position;
    private Vector3[] positions;

    /// Settings
    private static string SETTINGS = "0", PLAY = "1", PAUSE = "2", STOP = "3", SPEED = "4";// da aggiornare

    private int flockNum = 0;//generalizzare
    private int deadFlockers = 0;//generalizzare



    /// Access methods ///
 
    public Tuple<int, int, String[]> Des_msg { get => des_msg; set => des_msg = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, Vector3[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }




    /// <summary>
    /// We use Awake to setup default Simulation
    /// </summary>
    protected virtual void Awake()
    {
        simSpace = GameObject.FindGameObjectWithTag("SimulationCube");//game controller
        Debug.Log(simSpace.GetComponent<Collider>().bounds.size.x);
        //Set Default Simulation and instantiate support variables
        Ready_buffer = new Tuple<long, Vector3[]>(0, new Vector3[0]);//da aggiornare
        positions = new Vector3[0];//da aggiornare
        update.Add("sim_params", new JSONArray());
        update.Add("agents", new JSONArray());
        update.Add("obstacles", new JSONArray());
        update.Add("generics", new JSONArray());
    }

    /// <summary>
    /// Connect to the broker using current settings.
    /// </summary>
    private void Start()
    {
        // Retrieve UIController
        UIController = GameObject.Find("UIController").GetComponent<UIController>();

        // Start MQTT Clients
        CommController.StartSimulationClient(simMessageQueue, SIM_CLIENT_READY);
        CommController.StartControlClient(responseMessageQueue, CONTROL_CLIENT_READY);

        // Start Connection Thread
        connectionThread = new Thread(() => WaitForConnection());
        connectionThread.Start();

    }

    /// <summary>
    /// Main routine
    /// </summary>
    private void Update()
    {
        
    }
    private void WaitForConnection() //da aggiornare
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
                state = State.CONN_ERROR;
                stopwatch.Stop();
                return;
            }
        }
        state = State.READY;
        SetupBackgroundTasks();
    }

    private void SetupBackgroundTasks()
    {        
        BuildSteps(); //da aggiornare
    }

    public void Play()
    {
        //blocco il tasto
        //devo aspettare la risposta per eventualmente sbloccare il tasto se mason non ha ricevuto il messaggio
        //sbloccare i bottoni necessari
        
    }

    public void Pause()
    {

    }

    public void Stop()
    {
        //if (State == simulationState.STOP) {return;}
        //controlClient.SendCommand(STOP);
        //State = simulationState.STOP;
        DestroyAgents();
        LatestSimStepArrived = 0;
        currentSimStep = -1;
        MqttMsgPublishEventArgs ignored; while (simMessageQueue.TryDequeue(out ignored));
        SecondaryQueue.Clear();
    }

    public void ChangeSimulationSpeed(int speedIndicator)
    {
        //controlClient.SendCommand(SPEED + ":" + speedIndicator.ToString());
    }

    public void SendSimulationSettings(bool partialUpdate)
    {
        //inviare json
    }

    

   

    public void LoadSimulation(JSONObject sim)
    {
        simulation.Id = sim["id"];
        simulation.Name = sim["name"];
        simulation.Description = sim["description"];
        simulation.Type = sim["type"];
        simulation.Dimensions = sim["dimensions"];

        JSONArray parameters = (JSONArray)sim["sim_params"];

        foreach(JSONObject p in parameters)
        {
            //var obj = Activator.CreateInstance(Type.GetType("System." + p["type"] + p["size"]));   //sistemare i tipi
            //Convert.ChangeType(obj, Type.GetType("System." + p["type"] + p["size"]));             //per castare al tipo corretto
            simulation.Parameters.Add(p["type"], p["default"]);
            if (p["editable_in_play"].Equals("true"))
            {
                simulation.EditableInPlay.Add(p["type"]);
            }
            if (p["editable_in_pause"].Equals("true"))
            {
                simulation.EditableInPause.Add(p["type"]);
            }
        }
        JSONArray agent_prototypes = (JSONArray)sim["agent_prototypes"];
        foreach (JSONObject agent in agent_prototypes)
        {
            Agent a = new Agent(agent["name"]);
            foreach (JSONObject p in (JSONArray)agent["params"])
            {
                a.Parameters.Add(p["name"], p["default"]);
                a.Parameters.Add("editable_in_play", p["editable_in_play"]);
                a.Parameters.Add("editable_in_pause", p["editable_in_pause"]);
            }
            simulation.Agent_prototypes.Add(a);
        }

        JSONArray obstacle_prototypes = (JSONArray)sim["obstacle_prototypes"];
        foreach (JSONObject obstacle in obstacle_prototypes)
        {
            Obstacle o = new Obstacle(obstacle["name"], obstacle["type"]);
            simulation.Obstacle_prototypes.Add(o);
        }

        JSONArray generic_prototypes = (JSONArray)sim["generic_prototypes"];
        foreach (JSONObject generic in generic_prototypes)
        {
            Generic g = new Generic(generic["name"]);
            foreach (JSONObject p in (JSONArray)generic["params"])
            {
                g.Parameters.Add(p["name"], p["default"]);
                g.Parameters.Add("editable_in_play", p["editable_in_play"]);
                g.Parameters.Add("editable_in_pause", p["editable_in_pause"]);
            }
            simulation.Generic_prototypes.Add(g);
        }
    }

    public void UpdateSimulation(string type, string class_name, string id, string param_name, string value)
    {
        bool found = false;
        if (type.Equals("sim_params"))
        {
            foreach (JSONNode n in update[type].Children)
            {
                if (n["name"].Equals(param_name))
                {
                    n["value"] = value;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                update["sim_params"].Add(param_name, value);
            }
        }
        else
        {
            foreach (JSONNode n in update[type].Children)
            {
                if (n["class"].Equals(class_name) && n["id"].Equals(id))
                {
                    foreach (JSONNode p in n["params"])
                    {
                        if (p["name"].Equals(param_name))
                        {
                            p["value"] = value;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        n["params"].Add(param_name, value);
                    }
                }
            }
            if (!found)
            {
                update["sim_params"].Add(param_name, value);
            }
        }
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
            long current_id = Ready_buffer.Item1;
            if (current_id > CurrentSimStep)
            {
                for (int i = 0; i < Ready_buffer.Item2.Length; i++)
                {
                    var old_pos = agents[i].transform.position;
                    agents[i].transform.localPosition = Ready_buffer.Item2[i];
                    Vector3 velocity = agents[i].transform.position - old_pos;
                    if (!velocity.Equals(Vector3.zero))
                    {
                        agents[i].transform.rotation = Quaternion.Slerp(agents[i].transform.rotation, Quaternion.LookRotation(velocity, Vector3.up), 4 * Time.deltaTime);
                    }
                }
                CurrentSimStep = current_id;
            }
        }
    }
    void OnApplicationQuit()
    {
        CommController.DisconnectControlClient();
        CommController.DisconnectSimulationClient();
        connectionThread.Abort();
        buildStepThread.Abort();
        performanceMonitorThread.Abort();
    }
}