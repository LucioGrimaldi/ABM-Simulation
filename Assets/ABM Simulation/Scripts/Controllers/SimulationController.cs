using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using System;
using System.Linq;

public class SimulationController : MonoBehaviour
{
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

    /// Managers
    ConnectionManager ConnManager;
    PerformanceManger PerfManager;

    /// Controllers ///
    private UIController UIController;
    private GameController GameController;
    private CommunicationController CommController;

    /// Sim-related variables ///
    /// State
    private Simulation simulation = new Simulation();
    private State state = State.NOT_READY;
    public enum State 
    {
        CONN_ERROR = -2,            // Error in connection
        NOT_READY = -1,             // Client is not connected
        READY = 0                  // Client is ready to play
    }

    /// Updates
    private JSONObject uncommitted_updatesJSON = new JSONObject();
    private Dictionary<(string, string), SimObject> uncommitted_updates = new Dictionary<(string, string), SimObject>();

    /// Support variables
    private string nickname;
    public enum SimObjectType
    {
        AGENT = 0,
        GENERIC = 1,
        OBSTACLE = 2
    }

    /// Access methods ///
    public Simulation Simulation { get => simulation; set => simulation = value; }
    public string Nickname { get => nickname; set => nickname = value; }


    /// <summary>
    /// We use Awake to bootstrap App
    /// </summary>
    protected virtual void Awake()
    {
        // Retrieve Controllers
        //UIController = GameObject.Find("UIController").GetComponent<UIController>();
        //GameController = GameObject.Find("GameController").GetComponent<GameController>();
        CommController = new CommunicationController();
        PerfManager = new PerformanceManger();
        //ConnManager = new ConnectionManager(CommController, Nickname);

        BootstrapBackgroundTasks();

        // Register to EventHandlers
        CommController.responseMessageEventHandler += onResponseMessageReceived;
        CommController.stepMessageEventHandler += onStepMessageReceived;



        // Init uncommited_updates JSON
        uncommitted_updatesJSON.Add("sim_params", new JSONObject());
        uncommitted_updatesJSON.Add("agents_update", new JSONArray());
        uncommitted_updatesJSON.Add("obstacles_update", new JSONArray());
        uncommitted_updatesJSON.Add("generics_update", new JSONArray());
    }

    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    private void Start()
    {
        //SendCheckStatus();
        //SendConnect();
        //SendDisconnect();
        //SendSimListRequest();
        //SendSimUpdate();
        //SendSimCommand("4", "2");
        //SendResponse("001");
        //SendErrorMessage("GENERIC_ERROR", true);

        //byte[] step, compressed;

        //using (MemoryStream ms = new MemoryStream())
        //using (BinaryWriter writer = new BinaryWriter(ms, System.Text.Encoding.BigEndianUnicode))
        //{
        //    writer.Write(0);
        //    writer.Write(2);
        //    writer.Write(1);
        //    writer.Write(0);
        //    writer.Write(1.5f);
        //    writer.Write(1.5f);
        //    writer.Write(1.5f);
        //    writer.Write(1);
        //    writer.Write(3.0f);
        //    writer.Write(3.0f);
        //    writer.Write(3.0f);
        //    writer.Write(666);
        //    writer.Write(1.0f);
        //    writer.Write(1.0f);
        //    writer.Write(1.0f);
        //    writer.Write(9001);


        //    step = ms.ToArray();
        //}

        //using (var outStream = new MemoryStream())
        //{
        //    using (var tinyStream = new System.IO.Compression.GZipStream(outStream, System.IO.Compression.CompressionMode.Compress))
        //    using (var mStream = new MemoryStream(step))
        //        mStream.CopyTo(tinyStream);

        //    compressed = outStream.ToArray();
        //}
        //UnityEngine.Debug.Log("SIM_STEP: \n" + Utils.StepToJSON(compressed, (JSONObject)JSON.Parse("{ \"id\" : 0, \"name\" : \"Flockers\", \"description\" : \"....\", \"type\" : \"qualitative\", \"dimensions\" : [ { \"name\" : \"x\", \"type\" : \"System.Single\", \"default\" : 500 }, { \"name\" : \"y\", \"type\" : \"System.Single\", \"default\" : 500 }, { \"name\" : \"z\", \"type\" : \"System.Single\", \"default\" : 500 } ], \"sim_params\" : [ { \"name\" : \"cohesion\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"avoidance\", \"type\" : \"System.Single\", \"default\" : 0.5 }, { \"name\" : \"randomness\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"consistency\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"momentum\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"neighborhood\", \"type\" : \"System.Int32\", \"default\" : 10 }, { \"name\" : \"jump\", \"type\" : \"System.Single\", \"default\" : 0.7 }, ], \"agent_prototypes\" : [ { \"name\" : \"Flocker\", \"params\": [] } ], \"generic_prototypes\" : [ { \"name\" : \"Gerardo\", \"params\": [{ \"name\" : \"scimità\", \"type\" : \"System.Int32\", \"editable_in_play\" : true, \"editable_in_pause\" : true, \"value\" : 9001 }] } ]}")).ToString());

        //simulation.InitSimulationFromJSONEditedPrototype((JSONObject)JSON.Parse("{ \"id\": 0, \"name\": \"Flockers\", \"description\": \"....\", \"type\": \"qualitative\", \"dimensions\": [ { \"name\": \"x\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"y\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"z\", \"type\": \"System.Int32\", \"default\": 500 } ], \"sim_params\": [ { \"name\": \"cohesion\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"avoidance\", \"type\": \"System.Single\", \"default\": \"0.5\" }, { \"name\": \"randomness\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"consistency\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"momentum\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"neighborhood\", \"type\": \"System.Int32\", \"default\": 10 }, { \"name\": \"jump\", \"type\": \"System.Single\", \"default\": 0.7 } ], \"agent_prototypes\": [ { \"class\": \"Flocker\", \"position\" : { \"x\" : 1, \"y\" : 1, \"z\" : 1 }, \"default\" : 10, \"params\": [] } ], \"generic_prototypes\": [ { \"class\": \"Gerardo\", \"position\" : { \"x\" : 1, \"y\" : 1, \"z\" : 1 }, \"default\" : 1, \"params\": [ { \"name\": \"scimità\", \"type\": \"System.Int32\", \"editable_in_play\": true, \"editable_in_pause\": true, \"default\": 9001 } ] } ] }"));
        //UnityEngine.Debug.Log("SIMULATION: \n" + simulation);
        //
        //
        //simulation.UpdateSimulationFromJSONUpdate((JSONObject)JSON.Parse("{ \"sim_params\" : { \"cohesion\" : 1.9, \"neighborhood\" : 20 }, \"agents_update\" : [ { \"id\" : 0, \"class\" : \"Flocker\", \"position\" : { \"x\" : 10, \"y\" : 11, \"z\" : 7 }, \"params\" : {} }, { \"id\" : 1, \"class\" : \"Flocker\", \"position\" : { \"x\" : 9, \"y\" : 9, \"z\" : 9 }, \"params\" : {} } ], \"generics_update\" : [ { \"id\" : 0, \"class\" : \"Gerardo\", \"position\" : { \"x\" : 2, \"y\" : 2, \"z\" : 2 }, \"params\" : {\"scimità\" : 10000, \"breathtaking\" : true }}], \"obstacles_update\" : [] }"));
        //UnityEngine.Debug.Log("SIMULATION: \n" + simulation);
        //
        ////StoreParameterUpdate("cohesion", "100.0");
        //StoreParameterUpdate("generic", "Gerardo", "0", "avoidance", "10.0");
        //StoreParameterUpdate("generic", "Gerardo", "1", "avoidance", "10.0");
        //StoreParameterUpdate("generic", "Gerardo", "1", "cohesion", "1.0");
        //StoreParameterUpdate("generic", "Gerardo", "all", "avoidance", "10.0");
        //StoreParameterUpdate("generic", "Gerardo", "1", "avoidance", "0.0");
        //
        //
        //UnityEngine.Debug.Log("Uncommitted updates: " + uncommitted_updatesJSON.ToString(1));


    }

    /// <summary>
    /// Update routine (Unity Process)
    /// </summary>
    private void Update()
    {

    }

    /// <summary>
    /// Bootstrap background tasks
    /// </summary>
    private void BootstrapBackgroundTasks()
    {
        CommController.StartControlClient();
        CommController.StartSimulationClient();
        CommController.StartStepQueueHandlerThread(simulation.State, PerfManager.TARGET_FPS);
        CommController.StartResponseQueueHandlerThread();
    }


    /// CONTROL ///

    /// <summary>
    /// Play simulation
    /// </summary>
    public void Play()
    {
        // check state
        if (simulation.State == Simulation.StateEnum.PLAY) { return; }

        // if not in PLAY
        SendSimCommand("1", "");                                                                      //0,1,2,3,4 (STEP,PLAY,PAUSE,STOP,CHANGE_SPEED)
        simulation.State = Simulation.StateEnum.PLAY; //MOMENTANEA
    }

    /// <summary>
    /// Pause simulation
    /// </summary>
    public void Pause()
    {
        // check state
        if (simulation.State == Simulation.StateEnum.PAUSE) { return; }

        // if not in PAUSE
        SendSimCommand("2", "");                                                                      //0,1,2,3,4 (STEP,PLAY,PAUSE,STOP,CHANGE_SPEED)
        simulation.State = Simulation.StateEnum.PAUSE; //MOMENTANEA                                                    
    }

    /// <summary>
    /// Stop simulation
    /// </summary>
    public void Stop()
    {
        // check state
        if (simulation.State == Simulation.StateEnum.STOP) {return;}

        // if not in STOP
        SendSimCommand("3", null);                                                                      //0,1,2,3,4 (STEP,PLAY,PAUSE,STOP,CHANGE_SPEED)
        simulation.State = Simulation.StateEnum.STOP; //MOMENTANEA
//      simulation.LatestSimStepArrived = 0;
        simulation.CurrentSimStep = -1;
        CommController.EmptyQueues();
    }

    /// SIMULATION ///









    /// UTILITIES ///
    
    // Event Handles
    private void onSimParamModify(object sender, SimParamUpdateEventArgs e)
    {
        StoreSimParameterUpdateToJSON(e);
    }
    private void onSimObjectModify(object sender, SimObjectModifyEventArgs e)
    {
        StoreSimObjectModify(e);
    }
    private void onSimObjectCreate(object sender, SimObjectCreateEventArgs e)
    {
        StoreSimObjectCreate(e);
    }
    private void onSimObjectDelete(object sender, SimObjectDeleteEventArgs e)
    {
        StoreSimObjectDelete(e);
    }



    public void onUpdateCommitRequest(object sender, EventArgs e)
    {
        //
        // TODO
        //
    }
    public void onResponseMessageReceived(object sender, ResponseMessageEventArgs e)
    {

        switch (e.Op)
        {
            case "000":

                //check status

                break;
            case "007":

                // SIM_LIST
                string response_to_op = e.Payload["response_to_op"];
                switch (response_to_op)
                {
                    case "000":

                        break;
                    case "001":

                        break;
                    case "002":

                        break;
                    case "003":

                        CommController.Sim_prototypes_list = (JSONArray)e.Payload["response"];

                        break;
                    case "004":

                        break;
                    case "005":

                        // Conferma di update avvenuto
                        // GameController.commitUpdate

                        break;
                    case "006":




                        break;
                    case "999":

                        break;
                }

                break;
            case "998"://error
                       //disconnect
                break;
            default:
                break;
        }



    }
    public void onStepMessageReceived(object sender, StepMessageEventArgs e)
    {

    }


    // Store Utilities
    /// <summary>
    /// Store in uncommitted_updates parameter changes
    /// </summary>
    public void StoreSimObjectModify(SimObjectModifyEventArgs e)
    {
        if(e.id.Equals("all"))
        {
            foreach (KeyValuePair<(string, string), SimObject> kvp in uncommitted_updates.Where(kvp => (kvp.Key.Item1.Equals("MOD") || kvp.Key.Item1.Equals("CRT")) && kvp.Key.Item2.Contains(e.type + "." + e.class_name)))
            {
                kvp.Value.X = e.position.Item1;
                kvp.Value.X = e.position.Item2;
                kvp.Value.X = e.position.Item3;

                kvp.Value.UpdateParameter(e.param.Item1, e.param.Item2);
            }
        }
        else
        {
            SimObject x = new SimObject();
            if (uncommitted_updates.TryGetValue(("MOD", e.type + "." + e.class_name + "." + e.id), out x) || uncommitted_updates.TryGetValue(("CRT", e.type + "." + e.class_name + "." + e.id), out x))
            {
                x.X = e.position.Item1;
                x.Y = e.position.Item2;
                x.Z = e.position.Item3;

                x.UpdateParameter(e.param.Item1, e.param.Item2);
            }
            else
            {
                switch (e.type)
                {
                    case SimObjectType.AGENT:
                        simulation.Agents.TryGetValue(e.class_name + "." + e.id, out x);
                        break;
                    case SimObjectType.GENERIC:
                        simulation.Generics.TryGetValue(e.class_name + "." + e.id, out x);
                        break;
                    case SimObjectType.OBSTACLE:
                        simulation.Obstacles.TryGetValue(e.class_name + "." + e.id, out x);
                        break;
                }

                SimObject y = x.Clone();

                y.X = e.position.Item1;
                y.Y = e.position.Item2;
                y.Z = e.position.Item3;

                y.UpdateParameter(e.param.Item1, e.param.Item2);

                uncommitted_updates.Add(("MOD", y.Type + "." + y.Class_name + "." + y.Id), y);
            }
        }        
    }
    public void StoreSimObjectCreate(SimObjectCreateEventArgs e)
    {
        SimObject x = new SimObject();
        switch (e.type)
        {
            case SimObjectType.AGENT:
                x.Type = SimObjectType.AGENT;
                x.Id = simulation.Agents.Where(kvp => kvp.Key.Contains(e.class_name)).Count();
                break;
            case SimObjectType.GENERIC:
                x.Type = SimObjectType.GENERIC;
                x.Id = simulation.Generics.Where(kvp => kvp.Key.Contains(e.class_name)).Count() + uncommitted_updates.Where(kvp => kvp.Key.Item1.Equals("CRT") && kvp.Key.Item2.Contains(e.type + "." + e.class_name)).Count();
                break;
            case SimObjectType.OBSTACLE:
                x.Type = SimObjectType.OBSTACLE;
                x.Id = simulation.Obstacles.Where(kvp => kvp.Key.Contains(e.class_name)).Count() + uncommitted_updates.Where(kvp => kvp.Key.Item1.Equals("CRT") && kvp.Key.Item2.Contains(e.type + "." + e.class_name)).Count();
                break;
        }

        x.Class_name = e.class_name;

        x.X = e.position.Item1;
        x.Y = e.position.Item2;
        x.Z = e.position.Item3;

        x.Parameters = e.parameters;

        uncommitted_updates.Add(("CRT", x.Type + "." + x.Class_name + "." + x.Id), x);
    }
    public void StoreSimObjectDelete(SimObjectDeleteEventArgs e)
    {
        SimObject x = new SimObject();
        if (e.id.Equals("all"))
        {
            foreach (KeyValuePair<(string, string), SimObject> kvp in uncommitted_updates.Where(kvp => kvp.Key.Item2.Contains(e.type + "." + e.class_name)))
            {
                uncommitted_updates.Remove(kvp.Key);
                uncommitted_updates.Add(("DEL", e.type + "." + e.class_name + "." + e.id), x);
            }
        }
        else
        {
            if (uncommitted_updates.TryGetValue(("MOD", e.type + "." + e.class_name + "." + e.id), out x))
            {
                uncommitted_updates.Remove(("MOD", e.type + "." + e.class_name + "." + e.id));
                uncommitted_updates.Add(("DEL", e.type + "." + e.class_name + "." + e.id), x);
            }
            else if (uncommitted_updates.TryGetValue(("CRT", e.type + "." + e.class_name + "." + e.id), out x))
            {
                uncommitted_updates.Remove(("CRT", e.type + "." + e.class_name + "." + e.id));
            }
            else
            {
                uncommitted_updates.Add(("DEL", e.type + "." + e.class_name + "." + e.id), x);
            }
        }
    }

    /// <summary>
    /// Store in uncommitted_updatesJSON parameter changes
    /// </summary>
    private void StoreSimParameterUpdateToJSON(SimParamUpdateEventArgs e)
    {
        uncommitted_updatesJSON["sim_params"].Add(e.param.Item1, e.param.Item2);
    }
    private void StoreUncommittedUpdatesToJSON()
    {
        foreach (KeyValuePair<(string, string), SimObject> kvp in uncommitted_updates)
        {
            //
            // TO DO
            //
        }

    }

    //private void StoreAgentUpdateToJSON()
    //{
    //    bool id_found = false;

    //    if (!e.id.Equals("all"))
    //    {
    //        foreach (JSONNode n in uncommitted_updatesJSON["agents_update"].Children)
    //        {
    //            if (n["class"].Equals(e.class_name) && n["id"].Equals(e.id))
    //            {
    //                id_found = true;
    //                n["params"].Add(e.param.Item1, e.param.Item2);
    //                break;
    //            }
    //        }
    //        if (!id_found)
    //        {
    //            JSONObject a = new JSONObject();
    //            a.Add("class", e.class_name);
    //            a.Add("id", e.id);
    //            a["params"].Add(e.param.Item1, e.param.Item2);
    //            uncommitted_updatesJSON["agents_update"].Add(a);
    //        }
    //    }
    //    else
    //    {
    //        foreach (JSONNode n in uncommitted_updatesJSON["agents_update"].Children)
    //        {
    //            if (n["class"].Equals(e.class_name))
    //            {
    //                if (n["id"].Equals("all"))
    //                {
    //                    id_found = true;
    //                    n["params"].Add(e.param.Item1, e.param.Item2);
    //                    break;
    //                }
    //                else
    //                {
    //                    n["params"].Remove(e.param.Item1);
    //                }
    //            }
    //        }
    //        if (!id_found)
    //        {
    //            JSONObject a = new JSONObject();
    //            a.Add("class", e.class_name);
    //            a.Add("id", e.id);
    //            a["params"].Add(e.param.Item1, e.param.Item2);
    //            uncommitted_updatesJSON["agents_update"].Add(a);
    //        }
    //    }
    //}
    //private void StoreGenericUpdateToJSON()
    //{
    //    bool id_found = false;

    //    if (!e.id.Equals("all"))
    //    {
    //        foreach (JSONNode n in uncommitted_updatesJSON["generics_update"].Children)
    //        {
    //            if (n["class"].Equals(e.class_name) && n["id"].Equals(e.id))
    //            {
    //                id_found = true;
    //                n["params"].Add(e.param.Item1, e.param.Item2);
    //                break;
    //            }
    //        }
    //        if(!id_found)
    //        {
    //            JSONObject g = new JSONObject();
    //            g.Add("class", e.class_name);
    //            g.Add("id", e.id);
    //            g["params"].Add(e.param.Item1, e.param.Item2);
    //            uncommitted_updatesJSON["generics_update"].Add(g);
    //        }
    //    }
    //    else
    //    {
    //        foreach (JSONNode n in uncommitted_updatesJSON["generics_update"].Children)
    //        {
    //            if (n["class"].Equals(e.class_name))
    //            {
    //                if(n["id"].Equals("all"))
    //                {
    //                    id_found = true;
    //                    n["params"].Add(e.param.Item1, e.param.Item2);
    //                    break;
    //                }
    //                else
    //                {
    //                    n["params"].Remove(e.param.Item1);
    //                }
    //            }
    //        }
    //        if (!id_found)
    //        {
    //            JSONObject g = new JSONObject();
    //            g.Add("class", e.class_name);
    //            g.Add("id", e.id);
    //            g["params"].Add(e.param.Item1, e.param.Item2);
    //            uncommitted_updatesJSON["generics_update"].Add(g);
    //        }
    //    }
    //}
    //public void StoreObstacleUpdateToJSON()
    //{
    //     ADD PARAMETERS?
    //}
   
    













    /// <summary>
    /// onApplicationQuit routine
    /// </summary>
    void OnApplicationQuit()
    {
        CommController.DisconnectControlClient();
        CommController.DisconnectSimulationClient();
        CommController.StopResponseQueueHandlerThread();
        CommController.StopStepQueueHandlerThread();
        // Stop Performance Thread
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
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending CHECK_STATUS to MASON...");
        CommController.SendMessage("Loocio", "000", payload);
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
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending CONNECT to MASON...");
        CommController.SendMessage("Loocio", "001", payload);
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
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending DISCONNECT to MASON...");
        CommController.SendMessage("Loocio", "002", payload);
    }

    /// <summary>
    /// Send sim list request
    /// </summary>
    public void SendSimListRequest()
    {
        // Create payload
        JSONObject payload = new JSONObject();;
        // Send command
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending SIM_LIST_REQUEST to MASON...");
        CommController.SendMessage("Loocio", "003", payload);
    }

    /// <summary>
    /// Send initialization message
    /// </summary>
    //  public void SendSimInitialize()
    //  {
    //      // Create payload
    //      JSONObject payload = new JSONObject();
    //      JSONArray sim_params = new JSONArray();
    //      JSONArray agent_prototypes = new JSONArray();
    //      JSONArray generic_prototypes = new JSONArray();
    //      JSONObject agent = new JSONObject();
    //      JSONObject generic = new JSONObject();
    //      JSONArray agent_params = new JSONArray();
    //      JSONArray generic_params = new JSONArray();
    //
    //      payload.Add("id", simulation.Id);
    //      foreach (KeyValuePair<string,string> e in simulation.Parameters) {
    //          sim_params.Add(e.Key, e.Value);
    //      }
    //      foreach (Agent a in simulation.Agent_prototypes)
    //      {
    //          agent.Add("name", a.Name);
    //          foreach (KeyValuePair<string, string> e in a.Parameters)
    //          {
    //              agent_params.Add(e.Key, e.Value);
    //          }
    //          agent.Add("params", agent_params);
    //          agent_prototypes.Add(agent);
    //      }
    //      foreach (Generic g in simulation.Generic_prototypes)
    //      {
    //          generic.Add("name", g.Name);
    //          foreach (KeyValuePair<string, string> e in g.Parameters)
    //          {
    //              generic_params.Add(e.Key, e.Value);
    //          }
    //          generic.Add("params", generic_params);
    //          generic_prototypes.Add(generic);
    //      }
    //      payload.Add("sim_params", sim_params);
    //      payload.Add("agent_prototypes", agent_prototypes);
    //      payload.Add("generic_prototypes", generic_prototypes);
    //      // Send command
    //      UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending SIM_INITIALIZE to MASON...");
    //      CommController.SendMessage("Loocio", "004", payload);
    //  }

    /// <summary>
    /// Send sim update
    /// </summary>
    public void SendSimUpdate()
    {
        // Send command
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending SIM_UPDATE to MASON...");
        CommController.SendMessage("Loocio", "005", uncommitted_updatesJSON);
        uncommitted_updatesJSON = new JSONObject();
    }

    /// <summary>
    /// Send sim commands
    /// </summary>
    public void SendSimCommand(string command, string value)                        //0,1,2,3,4 (STEP,PLAY,PAUSE,STOP,CHANGE_SPEED)
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("command", command);
        payload.Add("value", value);
        // Send command
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending SIM_COMMAND to MASON...");
        CommController.SendMessage("Loocio", "006", payload);
    }

    /// <summary>
    /// Send generic resoponse
    /// </summary>
    public void SendResponse(string response_to_op)
    {
        // Create payload
        JSONObject payload = new JSONObject();
        payload.Add("response_to_op", response_to_op);
        payload.Add("response", state.ToString());
        // Send command
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending RESPONSE to MASON...");
        CommController.SendMessage("Loocio", "007", payload);
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
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | Sending CLIENT_ERROR to MASON...");
        CommController.SendMessage("Loocio", "999", payload);
    }

}


/// <summary>
/// Event Args Definitions
/// </summary>
public class SimParamUpdateEventArgs : EventArgs
{
    public Tuple<string, dynamic> param;
}
public class SimObjectModifyEventArgs : EventArgs
{
    public SimulationController.SimObjectType type;
    public string class_name;
    public int id;
    public Tuple<float, float, float> position;
    public Tuple<string, dynamic> param;
}
public class SimObjectCreateEventArgs : EventArgs
{
    public SimulationController.SimObjectType type;
    public string class_name;
    public Tuple<float, float, float> position;
    public Dictionary<string, dynamic> parameters;
}
public class SimObjectDeleteEventArgs : EventArgs
{
    public SimulationController.SimObjectType type;
    public string class_name;
    public int id;
}

