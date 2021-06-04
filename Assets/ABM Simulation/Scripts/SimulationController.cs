using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using System;

public class SimulationController : MonoBehaviour
{
    //#### OPERATIONS_LIST ####
    //OP 000 CHECK_STATUS
    //OP 001 CONNECTION
    //OP 002 DISCONNECTION
    //OP 003 SIM_LIST
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
    private Simulation simulation;
    private State state = State.NOT_READY;
    /// Updates
    private JSONObject uncommitted_updates;
    public enum State 
    {
        CONN_ERROR = -2,            // Error in connection
        NOT_READY = -1,             // Client is not connected
        READY = 0,                  // Client is ready to play
    }

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
        UIController = GameObject.Find("UIController").GetComponent<UIController>();
        GameController = GameObject.Find("GameController").GetComponent<GameController>();
        CommController = new CommunicationController();
        ConnManager = new ConnectionManager(CommController, Nickname);

        BootstrapBackgroundTasks();

        // Init uncommited_updates JSON
        uncommitted_updates.Add("sim_params", new JSONArray());
        uncommitted_updates.Add("agents", new JSONArray());
        uncommitted_updates.Add("obstacles", new JSONArray());
        uncommitted_updates.Add("generics", new JSONArray());
    }

    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    private void Start()
    {

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
        CommController.StartControlClient(CommController.ResponseMessageQueue, CommController.CONTROL_CLIENT_READY);
        CommController.StartSimulationClient(CommController.SimMessageQueue, CommController.SIM_CLIENT_READY);
        CommController.StartStepsHandlerThread(simulation, PerfManager.TARGET_FPS);
        CommController.StartMessageHandlerThread();
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
        SendSimCommand("1", null);                                                                      //0,1,2,3,4 (STEP,PLAY,PAUSE,STOP,CHANGE_SPEED)
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
        SendSimCommand("2", null);                                                                      //0,1,2,3,4 (STEP,PLAY,PAUSE,STOP,CHANGE_SPEED)
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
        simulation.LatestSimStepArrived = 0;
        simulation.CurrentSimStep = -1;
        CommController.EmptyQueues();
    }

    /// SIMULATION ///

    /// <summary>
    /// Load/Init Simulation from JSON
    /// </summary>
    public void InitSimulationFromJSON(JSONObject sim)
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
            simulation.Parameters.Add(p["name"], p["default"]);
            if (p["editable_in_play"].Equals("true"))
            {
                simulation.EditableInPlay.Add(p["name"]);
            }
            if (p["editable_in_pause"].Equals("true"))
            {
                simulation.EditableInPause.Add(p["name"]);
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

    /// <summary>
    /// Update Simulation with single parameter
    /// </summary>
    public void StoreParameterUpdate(string type, string class_name, string id, string param_name, string value, string op, string[] cells)                 //op = 1,0 (insert,remove) cells[] = ["x,y,z,...","x,y,z,..."]
    {
        switch(type)
        {
            case "sim_params":
                StoreSimParameterUpdate(param_name, value);
                break;
            case "obstacle":
                StoreObstacleParameterUpdate(class_name, id, op, cells);
                break;
            case "agent":
                StoreAgentParameterUpdate(class_name, id, param_name, value);
                break;
            case "generic":
                StoreGenericParameterUpdate(class_name, id, param_name, value);
                break;
        }
    }
    public void StoreSimParameterUpdate(string param_name, string value)
    {
        bool found = false;
        foreach (JSONNode n in uncommitted_updates["sim_params"].Children)
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
            uncommitted_updates["sim_params"].Add(param_name, value);
        }
    }
    public void StoreAgentParameterUpdate(string agent_class, string id, string param_name, string value)
    {
        bool p_found = false;
        bool id_found = false;

        if (!id.Equals(null))
        {
            foreach (JSONNode n in uncommitted_updates["agents"].Children)
            {
                if (n["class"].Equals(agent_class) && n["id"].Equals(id))
                {
                    id_found = true;
                    foreach (JSONNode p in n["params"])
                    {
                        if (p["name"].Equals(param_name))
                        {
                            p_found = true;
                            p["value"] = value;
                            break;
                        }
                    }
                    if (!p_found)
                    {
                        n["params"].Add(param_name, value);
                    }
                }
            }
            if (!id_found)
            {
                JSONObject g = new JSONObject();
                JSONArray g_params = new JSONArray();
                g.Add("class", agent_class);
                g.Add("id", id);
                g_params.Add(param_name, value);
                g.Add("params", g_params);
                uncommitted_updates["agents"].Add(g);
            }
        }
        else
        {
            foreach (JSONNode n in uncommitted_updates["agents"].Children)
            {
                if (n["class"].Equals(agent_class))
                {
                    if (n["id"].Equals(null))
                    {
                        id_found = true;
                        foreach (JSONNode p in n["params"])
                        {
                            if (p["name"].Equals(param_name))
                            {
                                p_found = true;
                                p["value"] = value;
                                break;
                            }
                        }
                        if (!p_found)
                        {
                            n["params"].Add(param_name, value);
                        }
                    }
                    else
                    {
                        foreach (JSONNode p in n["params"])
                        {
                            if (p["name"].Equals(param_name))
                            {
                                n["params"].Remove(p);
                            }
                        }
                    }

                }
            }
            if (!id_found)
            {
                JSONObject g = new JSONObject();
                JSONArray g_params = new JSONArray();
                g.Add("class", agent_class);
                g.Add("id", id);
                g_params.Add(param_name, value);
                g.Add("params", g_params);
                uncommitted_updates["agents"].Add(g);
            }
        }
    }
    public void StoreGenericParameterUpdate(string generic_class, string id, string param_name, string value)
    {
        bool p_found = false;
        bool id_found = false;

        if (!id.Equals(null))
        {
            foreach (JSONNode n in uncommitted_updates["generics"].Children)
            {
                if (n["class"].Equals(generic_class) && n["id"].Equals(id))
                {
                    id_found = true;
                    foreach (JSONNode p in n["params"])
                    {
                        if (p["name"].Equals(param_name))
                        {
                            p_found = true;
                            p["value"] = value;
                            break;
                        }
                    }
                    if(!p_found)
                    {
                        n["params"].Add(param_name, value);
                    }
                }
            }
            if(!id_found)
            {
                JSONObject g = new JSONObject();
                JSONArray g_params = new JSONArray();
                g.Add("class", generic_class);
                g.Add("id", id);
                g_params.Add(param_name, value);
                g.Add("params", g_params);
                uncommitted_updates["generics"].Add(g);
            }
        }
        else
        {
            foreach (JSONNode n in uncommitted_updates["generics"].Children)
            {
                if (n["class"].Equals(generic_class))
                {
                    if(n["id"].Equals(null))
                    {
                        id_found = true;
                        foreach (JSONNode p in n["params"])
                        {
                            if (p["name"].Equals(param_name))
                            {
                                p_found = true;
                                p["value"] = value;
                                break;
                            }
                        }
                        if (!p_found)
                        {
                            n["params"].Add(param_name, value);
                        }
                    }
                    else
                    {
                        foreach (JSONNode p in n["params"])
                        {
                            if (p["name"].Equals(param_name))
                            {
                                n["params"].Remove(p);
                            }
                        }
                    }
                    
                }
            }
            if (!id_found)
            {
                JSONObject g = new JSONObject();
                JSONArray g_params = new JSONArray();
                g.Add("class", generic_class);
                g.Add("id", id);
                g_params.Add(param_name, value);
                g.Add("params", g_params);
                uncommitted_updates["generics"].Add(g);
            }
        }
    }
    public void StoreObstacleParameterUpdate(string obstacle_class, string id, string op, string[] cells)
    {
        foreach (JSONNode n in uncommitted_updates["obstacles"].Children)
        {
            if (n["class"].Equals(obstacle_class) && n["id"].Equals(id))
            {
                if (op.Equals("0"))
                {
                    _ = (JSONArray)uncommitted_updates["obstacles"].Remove(n);
                }
                else
                {
                    Debug.LogError("Obstacle " + "ID: " + id + " already exists! Remove it first, then add again.");
                }
            }
            else
            {
                if (op.Equals("1"))
                {
                    JSONObject obstacle = new JSONObject();
                    JSONObject cell = new JSONObject();
                    JSONArray cells_json = new JSONArray();
                    obstacle.Add("op", "1");
                    obstacle.Add("class", obstacle_class);
                    obstacle.Add("id", id);
                    foreach(string c in cells)
                    {
                        for(int i=0; i<simulation.Dimensions; i++)
                        {
                            cell.Add("dim_" + i, c.Split(',')[i]);
                        }
                        cells_json.Add(cell);
                    }
                    obstacle.Add("cells", cells_json);
                    uncommitted_updates["obstacles"].Add(obstacle);
                }
                else
                {
                    Debug.LogError("Obstacle " + "ID: " + id + " does not exist! Add it first, then you can remove.");
                }
            }
        }
    }                                               // rename or consider adding parameters

    /// <summary>
    /// Update Simulation from JSON
    /// </summary>
    public void UpdateSimulationFromJSON(JSONObject update)
    {
        // SIM_PARAMS
        JSONArray parameters = (JSONArray)update["sim_params"];

        foreach (JSONObject p in parameters)
        {
            simulation.Parameters.Remove(p["name"]);
            simulation.Parameters.Add(p["name"], p["value"]);
        }

        // AGENTS
        JSONArray agents = (JSONArray)update["agents"];
        foreach (JSONObject agent in agents)
        {
            if (!agent["id"].Equals(null))
            {
                simulation.Agents.TryGetValue(agent["id"], out Agent a);
                foreach (JSONObject p in (JSONArray)agent["params"])
                {
                    a.Parameters.Remove(p["name"]);
                    a.Parameters.Add(p["name"], p["value"]);
                }
            }
            else
            {
                foreach (Agent a in simulation.Agents.Values)
                {
                    foreach (JSONObject p in (JSONArray)agent["params"])
                    {
                        a.Parameters.Remove(p["name"]);
                        a.Parameters.Add(p["name"], p["value"]);
                    }
                }
            }
        }

        // OBSTACLES
        JSONArray obstacles = (JSONArray)update["obstacles"];
        foreach (JSONObject obstacle in obstacles)
        {
            simulation.Obstacles.TryGetValue(obstacle["id"], out Obstacle o);
            if(obstacle["op"].Equals("0"))
            {
                simulation.Obstacles.Remove(obstacle["id"]);
            }
            else
            {
                Obstacle obs = new Obstacle(obstacle["id"], obstacle["class"]);
                foreach (JSONObject c in (JSONArray)obstacle["cells"])
                {
                    foreach(KeyValuePair<string, JSONNode> e in c)
                    {
                        obs.Cells.Add((e.Key, e.Value.ToString()));
                    }
                }
                simulation.Obstacles.Add(obstacle["id"], obs);
            }
            
        }

        // GENERICS
        JSONArray generics = (JSONArray)update["generics"];
        foreach (JSONObject generic in generics)
        {
            if (!generic["id"].Equals(null))
            {
                simulation.Generics.TryGetValue(generic["id"], out Generic g);
                foreach (JSONObject p in (JSONArray)generic["params"])
                {
                    g.Parameters.Remove(p["name"]);
                    g.Parameters.Add(p["name"], p["value"]);
                }
            }
            else
            {
                foreach(Generic g in simulation.Generics.Values)
                {
                    foreach (JSONObject p in (JSONArray)generic["params"])
                    {
                        g.Parameters.Remove(p["name"]);
                        g.Parameters.Add(p["name"], p["value"]);
                    }
                }
            }            
        }
    }

    /// <summary>
    /// Get JSONObject from Simulation
    /// </summary>
    public JSONObject SimulationToJSON()
    {
        JSONObject sim_json = new JSONObject();

        // TODO

        return sim_json;
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
        CommController.SendMessage("Loocio", "003", payload);
    }

    /// <summary>
    /// Send initialization message
    /// </summary>
    public void SendSimInitialize()
    {
        // Create payload
        JSONObject payload = new JSONObject();
        JSONArray sim_params = new JSONArray();
        JSONArray agent_prototypes = new JSONArray();
        JSONArray generic_prototypes = new JSONArray();
        JSONObject agent = new JSONObject();
        JSONObject generic = new JSONObject();
        JSONArray agent_params = new JSONArray();
        JSONArray generic_params = new JSONArray();

        payload.Add("id", simulation.Id);
        foreach (KeyValuePair<string,string> e in simulation.Parameters) {
            sim_params.Add(e.Key, e.Value);
        }
        foreach (Agent a in simulation.Agent_prototypes)
        {
            agent.Add("name", a.Name);
            foreach (KeyValuePair<string, string> e in a.Parameters)
            {
                agent_params.Add(e.Key, e.Value);
            }
            agent.Add("params", agent_params);
            agent_prototypes.Add(agent);
        }
        foreach (Generic g in simulation.Generic_prototypes)
        {
            generic.Add("name", g.Name);
            foreach (KeyValuePair<string, string> e in g.Parameters)
            {
                generic_params.Add(e.Key, e.Value);
            }
            generic.Add("params", generic_params);
            generic_prototypes.Add(generic);
        }
        payload.Add("sim_params", sim_params);
        payload.Add("agent_prototypes", agent_prototypes);
        payload.Add("generic_prototypes", generic_prototypes);
        // Send command
        CommController.SendMessage("Loocio", "004", payload);
    }

    /// <summary>
    /// Send sim update
    /// </summary>
    public void SendSimUpdate()
    {
        // Send command
        CommController.SendMessage("Loocio", "005", uncommitted_updates);
        uncommitted_updates = new JSONObject();
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
        CommController.SendMessage("Loocio", "999", payload);
    }

    /// <summary>
    /// onApplicationQuit routine
    /// </summary>
    void OnApplicationQuit()
    {
        CommController.DisconnectControlClient();
        CommController.DisconnectSimulationClient();
        CommController.StopMessageHandlerThread();
        CommController.StopStepsHandlerThread();
        // Stop Performance Thread
    }
}