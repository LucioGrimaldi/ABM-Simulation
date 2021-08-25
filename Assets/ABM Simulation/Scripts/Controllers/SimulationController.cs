using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

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
    private Dictionary<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> uncommitted_updates = new Dictionary<(string, (SimObject.SimObjectType, string, int)), SimObject>();

    /// Support variables
    private string nickname;

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

    }
    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    private void Start()
    {

        simulation.InitSimulationFromPrototype((JSONObject)JSON.Parse("{ \"id\": 0, \"name\": \"Flockers\", \"description\": \"....\", \"type\": \"CONTINUOUS\", \"dimensions\": [ { \"name\": \"x\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"y\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"z\", \"type\": \"System.Int32\", \"default\": 500 } ], \"sim_params\": [ { \"name\": \"cohesion\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"avoidance\", \"type\": \"System.Single\", \"default\": \"0.5\" }, { \"name\": \"randomness\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"consistency\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"momentum\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"neighborhood\", \"type\": \"System.Int32\", \"default\": 10 }, { \"name\": \"jump\", \"type\": \"System.Single\", \"default\": 0.7 } ], \"agent_prototypes\": [ { \"class\": \"Flocker\", \"default\" : 10, \"params\": [ { \"name\" : \"position\", \"type\": \"System.Position\", \"editable_in_play\": true, \"editable_in_pause\": true, \"position\" : { \"x\" : 1, \"y\" : 1, \"z\" : 1 } } ] } ], \"generic_prototypes\": [ { \"class\": \"Gerardo\", \"default\" : 2, \"params\": [ { \"name\": \"scimità\", \"type\": \"System.Int32\", \"editable_in_play\": true, \"editable_in_pause\": true, \"default\": 9001 }, { \"name\" : \"position\", \"type\": \"System.Position\", \"editable_in_play\": true, \"editable_in_pause\": true, \"position\" : { \"x\" : 2, \"y\" : 2, \"z\" : 2 } } ] } ] }"));
        UnityEngine.Debug.Log("SIMULATION: \n" + simulation);

        //StoreParameterUpdate("cohesion", "100.0");
        SimObjectModifyEventArgs e = new SimObjectModifyEventArgs();
        SimObjectDeleteEventArgs d = new SimObjectDeleteEventArgs();

        e.type = SimObject.SimObjectType.GENERIC;
        e.class_name = "Gerardo";
        e.id = 0;
        e.m_params = false;
        StoreSimObjectModify(e);

        SimObjectCreateEventArgs c = new SimObjectCreateEventArgs();
        c.type = SimObject.SimObjectType.GENERIC;
        c.class_name = "Gerardo";
        c.parameters = new Dictionary<string, dynamic>() {
            {"scimità", 9999},
            {"asd", 1},
            {"capocchia", 0},
        };
        StoreSimObjectCreate(c);
        StoreSimObjectCreate(c);
        StoreSimObjectCreate(c);

        UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));

        e.type = SimObject.SimObjectType.GENERIC;
        e.class_name = "Gerardo";
        e.id = -1;
        e.parameters = new Dictionary<string, dynamic>() {
            {"scimità", 100000000},
        };
        e.m_params = true;
        StoreSimObjectModify(e);

        UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));

        d.type = SimObject.SimObjectType.GENERIC;
        d.class_name = "Gerardo";
        d.id = 1;
        StoreSimObjectDelete(d);

        UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));

        StoreUncommittedUpdatesToJSON();
        UnityEngine.Debug.Log("Uncommitted updates JSON: \n" + uncommitted_updatesJSON.ToString());

        simulation.UpdateSimulationFromEdit(uncommitted_updatesJSON, uncommitted_updates);
        UnityEngine.Debug.Log("SIMULATION: \n" + simulation);

    }
    /// <summary>
    /// Update routine (Unity Process)
    /// </summary>
    private void Update()
    {

    }

    /// UTILS ///

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
    /// onEvent Methods ///

    /// <summary>
    /// Event Handles
    /// </summary>
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

    /// Store Methods ///

    /// <summary>
    /// Store in uncommitted_updates parameter changes
    /// </summary>
    public void StoreSimObjectModify(SimObjectModifyEventArgs e)
    {
        if(e.id.Equals(-1))                                                                                      // -1 = all
        {
            foreach (KeyValuePair<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> entry in uncommitted_updates.Where(entry => (entry.Key.op.Equals("MOD") || entry.Key.op.Equals("CRT")) && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name)))
            {
                if (e.m_params)
                {
                    foreach(KeyValuePair<string, dynamic> param in e.parameters)
                    {
                        if (!entry.Value.UpdateParameter(param.Key, param.Value))
                        {
                            entry.Value.AddParameter(param.Key, param.Value);
                        }
                    }                    
                }
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: " + entry.Key.op + " " + e.type + "." + e.class_name + "." + entry.Key.obj.id + " updated.");
            }
            
            if (!uncommitted_updates.ContainsKey(("MOD", (e.type, e.class_name, e.id))))
            {
                SimObject a = new SimObject();
                a.Type = e.type;
                a.Class_name = e.class_name;
                a.Id = e.id;
            
                if (e.m_params)
                {
                    foreach (KeyValuePair<string, dynamic> param in e.parameters)
                    {
                        if (!a.UpdateParameter(param.Key, param.Value))
                        {
                            a.AddParameter(param.Key, param.Value);
                        }
                    }
                }

                uncommitted_updates.Add(("MOD", (a.Type, a.Class_name, a.Id)), a);
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: MOD " + e.type + "." + e.class_name + "." + e.id + " created.");
            }
        }
        else
        {
            KeyValuePair<(string op, (SimObject.SimObjectType, string, int) obj), SimObject> entry;
            SimObject x = new SimObject();
            try
            {
                entry = uncommitted_updates.Single(entry => (entry.Key.op.Equals("MOD") || entry.Key.op.Equals("CRT")) && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name) && entry.Key.obj.id.Equals(e.id));
            }
            catch (InvalidOperationException)
            {
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | 0 entries for object " + e.type + "." + e.class_name + "." + e.id + ".");
            }

            if (!(entry.Value == null))
            {
                if (e.m_params)
                {
                    foreach (KeyValuePair<string, dynamic> param in e.parameters)
                    {
                        entry.Value.UpdateParameter(param.Key, param.Value);
                    }
                }
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: " + entry.Key.op + " " + e.type + "." + e.class_name + "." + e.id + " updated.");
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
                            UnityEngine.Debug.LogError("SIMULATION_CONTROLLER | onSimObjectModify | " + e.type + "." + e.class_name + "." + e.id + " does not exist.");
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
                            UnityEngine.Debug.LogError("SIMULATION_CONTROLLER | onSimObjectModify | " + e.type + "." + e.class_name + "." + e.id + " does not exist.");
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
                            UnityEngine.Debug.LogError("SIMULATION_CONTROLLER | onSimObjectModify | " + e.type + "." + e.class_name + "." + e.id + " does not exist.");
                            return;
                        }
                        break;
                }

                SimObject y = x.Clone();

                if (e.m_params)
                {
                    foreach (KeyValuePair<string, dynamic> param in e.parameters)
                    {
                        y.UpdateParameter(param.Key, param.Value);
                    }
                }

                uncommitted_updates.Add(("MOD", (y.Type, y.Class_name, y.Id)), y);
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: MOD " + e.type + "." + e.class_name + "." + e.id + " created.");
            }
        }        
    }
    public void StoreSimObjectCreate(SimObjectCreateEventArgs e)
    {
        SimObject x = new SimObject();
        switch (e.type)
        {
            case SimObject.SimObjectType.AGENT:
                x.Type = SimObject.SimObjectType.AGENT;
                x.Id = simulation.Agents.Where(entry => entry.Key.class_name.Equals(e.class_name)).Count();
                break;
            case SimObject.SimObjectType.GENERIC:
                x.Type = SimObject.SimObjectType.GENERIC;
                x.Id = simulation.Generics.Where(entry => entry.Key.class_name.Equals(e.class_name)).Count() + uncommitted_updates.Where(entry => entry.Key.op.Equals("CRT") && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name)).Count();
                break;
            case SimObject.SimObjectType.OBSTACLE:
                x.Type = SimObject.SimObjectType.OBSTACLE;
                x.Id = simulation.Obstacles.Where(entry => entry.Key.class_name.Equals(e.class_name)).Count() + uncommitted_updates.Where(entry => entry.Key.op.Equals("CRT") && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name)).Count();
                break;
        }

        x.Class_name = e.class_name;
        x.Parameters = e.parameters;

        uncommitted_updates.Add(("CRT", (x.Type, x.Class_name, x.Id)), x);
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: CRT " + e.type + "." + e.class_name + "." + x.Id + " created.");

    }
    public void StoreSimObjectDelete(SimObjectDeleteEventArgs e)
    {
        List<(string, (SimObject.SimObjectType, string, int))> keys_to_remove = new List<(string, (SimObject.SimObjectType, string, int))>();
        SimObject x = new SimObject();
        if (e.id.Equals(-1))
        {
            foreach (KeyValuePair<(string, (SimObject.SimObjectType, string, int)), SimObject> entry in uncommitted_updates.Where(entry => entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name)))
            {
                keys_to_remove.Add(entry.Key);
            }
            foreach((string op, (SimObject.SimObjectType type, string class_name, int id) obj) key in keys_to_remove)
            {
                uncommitted_updates.Remove(key);
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: " + key.op + " " + e.type + "." + e.class_name + "." + key.obj.id + " removed.");
            }
            keys_to_remove.Clear();
        }
        else
        {
            KeyValuePair<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> entry;
            try
            {
                entry = uncommitted_updates.Single(entry => (entry.Key.op.Equals("MOD") || entry.Key.op.Equals("CRT")) && entry.Key.obj.type.Equals(e.type) && entry.Key.obj.class_name.Equals(e.class_name) && entry.Key.obj.id.Equals(e.id));
                uncommitted_updates.Remove(entry.Key);
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: DEL " + entry.Key.op + " " + e.type + "." + e.class_name + "." + e.id + " removed.");
            }
            catch (InvalidOperationException)
            {
                UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | 0 entries for object " + e.type + "." + e.class_name + "." + e.id + ".");
            }   
        }
        uncommitted_updates.Add(("DEL", (e.type, e.class_name, e.id)), x);
        UnityEngine.Debug.Log("SIMULATION_CONTROLLER | onSimObjectModify | Entry: DEL " + e.type + "." + e.class_name + "." + e.id + " created.");
    }

    /// <summary>
    /// Store in uncommitted_updatesJSON parameter changes
    /// </summary>
    private void StoreSimParameterUpdateToJSON(SimParamUpdateEventArgs e)
    {
        uncommitted_updatesJSON["sim_params"].Add(e.param.param_name, e.param.Item2);
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
            
            if (entry.Key.obj.type.Equals(SimObject.SimObjectType.OBSTACLE))
            {
                JSONObject cell = new JSONObject();
                JSONArray cells = new JSONArray();
                dynamic cell_list = new MyList<MyList<(string coord, int value)>>();
                SimObject obstacle = new SimObject();

                if (entry.Key.op.Equals("MOD"))
                {
                    entry.Value.Parameters.TryGetValue("cells", out cell_list);                                         // cells are in MyList<MyList<(string coord, int value)>> form
                    foreach (MyList<(string coord, int value)> c in cell_list)
                    {
                        foreach((string coord, int value) in c)
                        {
                            cell.Add(coord, value);
                        }
                        cells.Add(cell);
                    }
                    uncommitted_updatesJSON[type + "_create"].Add("cells", cells);
                    simulation.Obstacles.TryGetValue((entry.Key.obj.class_name, entry.Key.obj.id), out obstacle);
                    obstacle.Parameters.TryGetValue("cells", out cell_list);                                            // cells are in MyList<MyList<(string coord, int value)>> form
                    foreach (MyList<(string coord, int value)> c in cell_list)
                    {
                        foreach ((string coord, int value) in c)
                        {
                            cell.Add(coord, value);
                        }
                        cells.Add(cell);
                    }
                    uncommitted_updatesJSON[type + "_delete"].Add("cells", cells);
                }
                else
                {
                    entry.Value.Parameters.TryGetValue("cells", out cell_list);                                         // cells are in MyList<MyList<(string coord, int value)>> form
                    foreach (MyList<(string coord, int value)> c in cell_list)
                    {
                        foreach ((string coord, int value) in c)
                        {
                            cell.Add(coord, value);
                        }
                        cells.Add(cell);
                    }
                    uncommitted_updatesJSON[type + "_" + op].Add("cells", cells);
                }
            }
            else
            {
                obj.Add("id", entry.Key.obj.id);
                obj.Add("class", entry.Key.obj.class_name);
                if (!entry.Key.op.Equals("DEL"))
                {
                    obj_params = (JSONNode)JSON.Parse(JsonConvert.SerializeObject(entry.Value.Parameters, new TupleConverter<string, float>()));
                    UnityEngine.Debug.Log("Params: \n" + obj_params.ToString());
                    obj.Add("params", obj_params);
                }
                uncommitted_updatesJSON[type+"_"+op].Add(obj);
            }            
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
    //      JSONArray sim_paramss = new JSONArray();
    //      JSONArray agent_prototypes = new JSONArray();
    //      JSONArray generic_prototypes = new JSONArray();
    //      JSONObject agent = new JSONObject();
    //      JSONObject generic = new JSONObject();
    //      JSONArray agent_params = new JSONArray();
    //      JSONArray generic_params = new JSONArray();
    //
    //      payload.Add("id", simulation.Id);
    //      foreach (KeyValuePair<string,string> e in simulation.Parameters) {
    //          sim_paramss.Add(e.Key, e.Value);
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
    //      payload.Add("sim_paramss", sim_paramss);
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

}


/// EVENTS ///

/// <summary>
/// Event Args Definitions
/// </summary>
public class SimParamUpdateEventArgs : EventArgs
{
    public (string param_name, dynamic value) param;
}
public class SimObjectModifyEventArgs : EventArgs
{
    public SimObject.SimObjectType type;
    public string class_name;
    public int id;
    public Dictionary<string, dynamic> parameters;                                    // i parametri sono (string param_name, dynamic value)
    public bool m_params;
}
public class SimObjectCreateEventArgs : EventArgs
{
    public SimObject.SimObjectType type;
    public string class_name;
    public Dictionary<string, dynamic> parameters;
}
public class SimObjectDeleteEventArgs : EventArgs
{
    public SimObject.SimObjectType type;
    public string class_name;
    public int id;
}

