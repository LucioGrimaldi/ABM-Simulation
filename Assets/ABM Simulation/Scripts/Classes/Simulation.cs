using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Simulation
{
    // Events
    public static event EventHandler<SimObjectDeleteEventArgs> OnSimObjectNotInStepEventHandler;

    // Simulation data
    public string name, description;
    public SimTypeEnum type;
    public int id;
    private bool is_discrete;
    public ConcurrentDictionary<string, int> dimensions = new ConcurrentDictionary<string, int>();
    public ConcurrentDictionary<string, SimObject> agent_prototypes = new ConcurrentDictionary<string, SimObject>();
    public ConcurrentDictionary<string, SimObject> generic_prototypes = new ConcurrentDictionary<string, SimObject>();
    public ConcurrentDictionary<string, SimObject> obstacle_prototypes = new ConcurrentDictionary<string, SimObject>();
    public ConcurrentDictionary<string, object> parameters = new ConcurrentDictionary<string, object>();
    public List<string> editableInPlay = new List<string>();
    public List<string> editableInInit = new List<string>();
    public List<string> editableInPause = new List<string>();

    public enum SimTypeEnum
    {
        CONTINUOUS = 0,
        DISCRETE = 1
    }
    public enum StateEnum
    {
        NOT_READY = -1,
        READY = 0,
        PLAY = 1,
        PAUSE = 2,
        STEP = 3,
        BUSY = 4
    }
    public enum SpeedEnum
    {
        X0_25,
        X0_5,
        X1,
        X2,
        MAX
    }

    // Runtime data
    public static StateEnum state = StateEnum.NOT_READY;
    public SpeedEnum speed = SpeedEnum.X1;
    public long currentSimStep = 0;
    public ConcurrentDictionary<string,int> n_agents_for_each_class = new ConcurrentDictionary<string, int>();
    public ConcurrentDictionary<string,int> n_generics_for_each_class = new ConcurrentDictionary<string, int>();
    public ConcurrentDictionary<(string class_name, int id), SimObject> agents = new ConcurrentDictionary<(string, int), SimObject>();
    public ConcurrentDictionary<(string class_name, int id), SimObject> generics = new ConcurrentDictionary<(string, int), SimObject>();
    public ConcurrentDictionary<(string class_name, int id), SimObject> obstacles = new ConcurrentDictionary<(string, int), SimObject>();
    private List<(SimObject.SimObjectType type, string class_name, int id)> toDeleteIfNotInStep = new List<(SimObject.SimObjectType type, string class_name, int id)>();
    private List<(SimObject.SimObjectType type, string class_name, int id)> temp = new List<(SimObject.SimObjectType type, string class_name, int id)>();
    private SimObject defaultSimObject;

    public string Name { get => name; set => name = value; }
    public string Description { get => description; set => description = value; }
    public SimTypeEnum Type { get => type; set => type = value; }
    public StateEnum State { get => state; set => state = value; }
    public SpeedEnum Speed { get => speed; set => speed = value; }
    public ConcurrentDictionary<string, int> Dimensions { get => dimensions; set => dimensions = value; }
    public ConcurrentDictionary<string, SimObject> Agent_prototypes { get => agent_prototypes; set => agent_prototypes = value; }
    public ConcurrentDictionary<string, SimObject> Generic_prototypes { get => generic_prototypes; set => generic_prototypes = value; }
    public ConcurrentDictionary<string, SimObject> Obstacle_prototypes { get => obstacle_prototypes; set => obstacle_prototypes = value; }
    public ConcurrentDictionary<string, object> Parameters { get => parameters; set => parameters = value; }
    public int Id { get => id; set => id = value; }
    public bool Is_discrete { get => is_discrete; set => is_discrete = value; }
    public List<string> EditableInPlay { get => editableInPlay; set => editableInPlay = value; }
    public List<string> EditableInInit { get => editableInInit; set => editableInInit = value; }
    public List<string> EditableInPause { get => editableInPause; set => editableInPause = value; }
    public long CurrentSimStep { get => currentSimStep; set => currentSimStep = value; }
    public ConcurrentDictionary<string, int> N_agents_for_each_class { get => n_agents_for_each_class; set => n_agents_for_each_class = value; }
    public ConcurrentDictionary<string, int> N_generics_for_each_class { get => n_generics_for_each_class; set => n_generics_for_each_class = value; }
    public ConcurrentDictionary<(string class_name, int id), SimObject> Agents { get => agents; set => agents = value; }
    public ConcurrentDictionary<(string class_name, int id), SimObject> Generics { get => generics; set => generics = value; }
    public ConcurrentDictionary<(string class_name, int id), SimObject> Obstacles { get => obstacles; set => obstacles = value; }
    public List<(SimObject.SimObjectType type, string class_name, int id)> ToDeleteIfNotInStep { get => toDeleteIfNotInStep; set => toDeleteIfNotInStep = value; }
    public List<(SimObject.SimObjectType type, string class_name, int id)> Temp { get => temp; set => temp = value; }

    public Simulation(SimObject defaultSimObject)
    {
        this.defaultSimObject = defaultSimObject;
    }

    /// Add/Remove Agents/Generics Prototypes ///
    public void AddAgentPrototype(SimObject a)
    {
        agent_prototypes.TryAdd(a.Class_name, a);
    }
    public void AddGenericPrototype(SimObject g)
    {
        generic_prototypes.TryAdd(g.Class_name, g);
    }
    public void AddObstaclePrototype(SimObject o)
    {
        obstacle_prototypes.TryAdd(o.Class_name, o);
    }

    /// Add/Remove Agents/Generics/Obstacles ///
    public void AddAgent(SimObject a)
    {
        Agents.TryAdd((a.Class_name, a.Id), a);
        if(a.Is_in_step && !a.To_keep_if_not_in_step) toDeleteIfNotInStep.Add((a.Type, a.Class_name, a.Id));
    }
    public void RemoveAgent(SimObject a)
    {
        Agents.TryRemove((a.Class_name, a.Id), out _);
        if (a.Is_in_step && !a.To_keep_if_not_in_step) toDeleteIfNotInStep.Remove((a.Type, a.Class_name, a.Id));
    }
    public void RemoveAgent(int id, string class_name)
    {
        Agents.TryRemove((class_name,id), out SimObject a);
        if (a.Is_in_step && !a.To_keep_if_not_in_step) toDeleteIfNotInStep.Remove((SimObject.SimObjectType.AGENT, class_name, id));
    }
    public void AddGeneric(SimObject g)
    {
        Generics.TryAdd((g.Class_name, g.Id), g);
        if (g.Is_in_step && !g.To_keep_if_not_in_step) toDeleteIfNotInStep.Add((g.Type, g.Class_name, g.Id));
    }
    public void RemoveGeneric(SimObject g)
    {
        Generics.TryRemove((g.Class_name, g.Id), out _);
        if (g.Is_in_step && !g.To_keep_if_not_in_step) toDeleteIfNotInStep.Remove((g.Type, g.Class_name, g.Id));
    }
    public void RemoveGeneric(int id, string class_name)
    {
        Generics.TryRemove((class_name, id), out SimObject g);
        if (g.Is_in_step && !g.To_keep_if_not_in_step) toDeleteIfNotInStep.Remove((SimObject.SimObjectType.GENERIC, class_name, id));
    }
    public void AddObstacle(SimObject o)
    {
        Obstacles.TryAdd((o.Class_name, o.Id), o);
        if (o.Is_in_step && !o.To_keep_if_not_in_step) toDeleteIfNotInStep.Add((o.Type, o.Class_name, o.Id));
    }
    public void RemoveObstacle(SimObject o)
    {
        Obstacles.TryRemove((o.Class_name, o.Id), out _);
        if (o.Is_in_step && !o.To_keep_if_not_in_step) toDeleteIfNotInStep.Remove((o.Type, o.Class_name, o.Id));
    }
    public void RemoveObstacle(string class_name, int id)
    {
        Obstacles.TryRemove((class_name, id), out SimObject o);
        if (o.Is_in_step && !o.To_keep_if_not_in_step) toDeleteIfNotInStep.Remove((SimObject.SimObjectType.OBSTACLE, class_name, id));
    }

    /// Get/Add/Update Sim params ///

    public object GetParameter(string param_name)
    {
        object parameter;
        if (Parameters.TryGetValue(param_name, out parameter)) return parameter; else return null;
    }
    public bool AddParameter(string param_name, object value)
    {
        return Parameters.TryAdd(param_name, value);
    }
    public bool UpdateParameter(string param_name, object value)
    {
        if (Parameters.AddOrUpdate(param_name, value, (k, v) => { return value; }).Equals(value)) return true; else return false;

    }


    /// Utils ///

    /// <summary>
    /// Load/Init Simulation from JSON
    /// </summary>
    public void InitSimulationFromPrototype(JSONObject sim_edited_prototype)
    {
        // Get Sim infos
        Id = sim_edited_prototype["id"];
        Name = sim_edited_prototype["name"];
        Description = sim_edited_prototype["description"];
        Type = sim_edited_prototype["type"] == "DISCRETE" ? SimTypeEnum.DISCRETE : SimTypeEnum.CONTINUOUS;

        object value;
        object cell;
        float x = 0, y = 0, z = 0;

        // Get Sim dimensions
        JSONArray dimensions = (JSONArray)sim_edited_prototype["dimensions"];
        foreach (JSONObject d in dimensions)
        {
            value = (int)d["default"];
            Dimensions.TryAdd(d["name"], (int)value);
        }
        
        // Get Sim parameters
        JSONArray parameters = (JSONArray)sim_edited_prototype["sim_params"];
        foreach (JSONObject p in parameters)
        {
            switch ((string)p["type"])
            {
                case "System.Single":
                    value = (float)p["default"];
                    break;
                case "System.Int32":
                    value = (int)p["default"];
                    break;
                case "System.Boolean":
                    value = (bool)p["default"];
                    break;
                case "System.String":
                    value = (string)p["default"];
                    break;
                default:
                    value = null;
                    break;
            }
            AddParameter(p["name"], value);
            if ((bool)p["editable_in_play"])

            {
                EditableInPlay.Add(p["name"]);
            }
            if ((bool)p["editable_in_init"])
            {
                EditableInInit.Add(p["name"]);
            }
            if ((bool)p["editable_in_pause"])
            {
                EditableInPause.Add(p["name"]);
            }
        }

        // Get n_agents/generics_for_each_class
        // Create Agents/Generics prototypes to be used when instantiating Agents/Generics
        JSONArray agent_prototypes = (JSONArray)sim_edited_prototype["agent_prototypes"];
        JSONArray generic_prototypes = (JSONArray)sim_edited_prototype["generic_prototypes"];
        JSONArray obstacle_prototypes = (JSONArray)sim_edited_prototype["obstacle_prototypes"];
        foreach (JSONObject agent in agent_prototypes)
        {
            N_agents_for_each_class.TryAdd(agent["class"], (int)agent["default"]);
            try
            {
                SimObject a = defaultSimObject.Clone();
                a.Class_name = agent["class"];
                a.Type = SimObject.SimObjectType.AGENT;
                a.Is_in_step = agent["is_in_step"];
                a.To_keep_if_not_in_step = agent["to_keep_if_not_in_step"];
                a.Layer = agent["layer"];
                a.Shares_position = agent["shares_position"];

                foreach (JSONObject p in (JSONArray)agent["params"])
                {
                    switch ((string)p["type"])
                    {
                        case "System.Single":
                            value = (float)p["default"];
                            break;
                        case "System.Int32":
                            value = (int)p["default"];
                            break;
                        case "System.Boolean":
                            value = (bool)p["default"];
                            break;
                        case "System.String":
                            value = (string)p["default"];
                            break;
                        case "System.Position":
                            foreach (string dimension in Dimensions.Keys)
                            {
                                if (dimension.Equals("x")) x = (float)p["default"][dimension];
                                else if (dimension.Equals("y")) y = (float)p["default"][dimension];
                                else if (dimension.Equals("z")) z = (float)p["default"][dimension];
                            }
                            if (Dimensions.Count == 2) value = new Vector2(x, y); else value = new Vector3(x, y, z);
                            break;
                        case "System.Cells":
                            if (Dimensions.Count == 2) value = new MyList<Vector2Int>(); else value = new MyList<Vector3Int>();
                            foreach (JSONNode c in (JSONArray)p["default"])
                            {
                                foreach (string dimension in Dimensions.Keys)
                                {
                                    if (dimension.Equals("x")) x = (float)c[dimension];
                                    else if (dimension.Equals("y")) y = (float)c[dimension];
                                    else if (dimension.Equals("z")) z = (float)c[dimension];
                                }
                                if (Dimensions.Count == 2) { cell = new Vector2Int((int)x, (int)y); ((MyList<Vector2Int>)value).Add((Vector2Int)cell); } else { cell = new Vector3Int((int)x, (int)y, (int)z); ((MyList<Vector3Int>)value).Add((Vector3Int)cell); }
                            }
                            break;
                        default:
                            value = null;
                            break;
                    }
                    a.AddParameter(p["name"], value);
                    if ((bool)p["editable_in_play"])
                    {
                        EditableInPlay.Add(p["name"]);
                    }
                    if ((bool)p["editable_in_init"])
                    {
                        EditableInInit.Add(p["name"]);
                    }
                    if ((bool)p["editable_in_pause"])
                    {
                        EditableInPause.Add(p["name"]);
                    }
                }
                AddAgentPrototype(a);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
           
        }
        foreach (JSONObject generic in generic_prototypes)
        {
            N_generics_for_each_class.TryAdd(generic["class"], (int)generic["default"]);
            
            SimObject g = defaultSimObject.Clone();
            g.Class_name = generic["class"];
            g.Type = SimObject.SimObjectType.GENERIC;
            g.Is_in_step = generic["is_in_step"];
            g.To_keep_if_not_in_step = generic["to_keep_if_not_in_step"];
            g.Layer = generic["layer"];
            g.Shares_position = generic["shares_position"];

            foreach (JSONObject p in (JSONArray)generic["params"])
            {
                switch ((string)p["type"])
                {
                    case "System.Single":
                        value = (float)p["default"];
                        break;
                    case "System.Int32":
                        value = (int)p["default"];
                        break;
                    case "System.Boolean":
                        value = (bool)p["default"];
                        break;
                    case "System.String":
                        value = (string)p["default"];
                        break;
                    case "System.Position":
                        foreach (string dimension in Dimensions.Keys)
                        {
                            if (dimension.Equals("x")) x = (float)p["default"][dimension];
                            else if (dimension.Equals("y")) y = (float)p["default"][dimension];
                            else if (dimension.Equals("z")) z = (float)p["default"][dimension];
                        }
                        if (Dimensions.Count == 2) value = new Vector2(x, y); else value = new Vector3(x, y, z);
                        x = 0; y = 0; z = 0;
                        break;
                    case "System.Cells":
                        if (Dimensions.Count == 2) value = new MyList<Vector2Int>(); else value = new MyList<Vector3Int>();
                        foreach (JSONNode c in (JSONArray)p["default"])
                        {
                            foreach (string dimension in Dimensions.Keys)
                            {
                                if (dimension.Equals("x")) x = (float)c[dimension];
                                else if (dimension.Equals("y")) y = (float)c[dimension];
                                else if (dimension.Equals("z")) z = (float)c[dimension];
                            }
                            if (Dimensions.Count == 2) { cell = new Vector2Int((int)x, (int)y); ((MyList<Vector2Int>)value).Add((Vector2Int)cell); } else { cell = new Vector3Int((int)x, (int)y, (int)z); ((MyList<Vector3Int>)value).Add((Vector3Int)cell); }
                        }
                        break;
                    default:
                        value = null;
                        break;
                }
                g.AddParameter(p["name"], value);
                if ((bool)p["editable_in_play"])
                {
                    EditableInPlay.Add(p["name"]);
                }
                if ((bool)p["editable_in_init"])
                {
                    EditableInInit.Add(p["name"]);
                }
                if ((bool)p["editable_in_pause"])
                {
                    EditableInPause.Add(p["name"]);
                }
            }
            AddGenericPrototype(g);
        }
        foreach (JSONObject obstacle in obstacle_prototypes)
        {
            N_generics_for_each_class.TryAdd(obstacle["class"], (int)obstacle["default"]);
            
            SimObject o = defaultSimObject.Clone();
            o.Class_name = obstacle["class"];
            o.Type = SimObject.SimObjectType.OBSTACLE;
            o.Is_in_step = obstacle["is_in_step"];
            o.To_keep_if_not_in_step = obstacle["to_keep_if_not_in_step"];
            o.Layer = obstacle["layer"];
            o.Shares_position = obstacle["shares_position"];

            foreach (JSONObject p in (JSONArray)obstacle["params"])
            {
                switch ((string)p["type"])
                {
                    case "System.Single":
                        value = (float)p["default"];
                        break;
                    case "System.Int32":
                        value = (int)p["default"];
                        break;
                    case "System.Boolean":
                        value = (bool)p["default"];
                        break;
                    case "System.String":
                        value = (string)p["default"];
                        break;
                    case "System.Position":
                        foreach (string dimension in Dimensions.Keys)
                        {
                            if (dimension.Equals("x")) x = (float)p["default"][dimension];
                            else if (dimension.Equals("y")) y = (float)p["default"][dimension];
                            else if (dimension.Equals("z")) z = (float)p["default"][dimension];
                        }
                        if (Dimensions.Count == 2) value = new Vector2(x, y); else value = new Vector3(x, y, z);
                        x = 0; y = 0; z = 0;
                        break;
                    case "System.Cells":
                        if (Dimensions.Count == 2) value = new MyList<Vector2Int>(); else value = new MyList<Vector3Int>();
                        foreach (JSONNode c in (JSONArray)p["default"])
                        {
                            foreach (string dimension in Dimensions.Keys)
                            {
                                if (dimension.Equals("x")) x = (float)c[dimension];
                                else if (dimension.Equals("y")) y = (float)c[dimension];
                                else if (dimension.Equals("z")) z = (float)c[dimension];
                            }
                            if (Dimensions.Count == 2) { cell = new Vector2Int((int)x, (int)y); ((MyList<Vector2Int>)value).Add((Vector2Int)cell); } else { cell = new Vector3Int((int)x, (int)y, (int)z); ((MyList<Vector3Int>)value).Add((Vector3Int)cell); }
                        }
                        break;
                    default:
                        value = null;
                        break;
                }
                o.AddParameter(p["name"], value);
                if ((bool)p["editable_in_play"])
                {
                    EditableInPlay.Add(p["name"]);
                }
                if ((bool)p["editable_in_init"])
                {
                    EditableInInit.Add(p["name"]);
                }
                if ((bool)p["editable_in_pause"])
                {
                    EditableInPause.Add(p["name"]);
                }
            }
            AddObstaclePrototype(o);
        }

        UnityEngine.Debug.Log("SIMULATION UPDATED FROM PROTOTYPE: " + this.ToString());
    }

    /// <summary>
    /// Load Simulation Params from JSON
    /// </summary>
    public void UpdateParamsFromJSON(JSONObject sim_params)
    {
        // Update Sim parameters
        foreach (var p in sim_params)
        {
            UpdateParameter(p.Key, p.Value);
        }
    }

    /// <summary>
    /// Extract byte[] step and update Simulation accordingly 
    /// </summary>
    public void UpdateSimulationFromStep(byte[] step, JSONObject sim_prototype)
    {
        //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        //stopwatch.Start();

        // variabili
        byte[] decompressed_step = Utils.DecompressStepPayload(step);
        SimObject so = defaultSimObject.Clone();
        Is_discrete = sim_prototype["type"].Equals("DISCRETE");

        List<KeyValuePair<string, JSONNode>> agent_prototypes;
        List<KeyValuePair<string, JSONNode>> generic_prototypes;
        List<KeyValuePair<string, JSONNode>> obstacle_prototypes;
        List<List<(string, string)>> d_agent_params_for_each_class = new List<List<(string, string)>>();
        List<List<(string, string)>> d_generic_params_for_each_class = new List<List<(string, string)>>();
        List<List<(string, string)>> d_obstacle_params_for_each_class = new List<List<(string, string)>>();
        List<List<(string, string)>> parameters;
        object value;
        object cell;
        float x = 0, y = 0, z = 0;

        //stopwatch.Stop();
        //Debug.Log("Decompress: " + stopwatch.ElapsedMilliseconds);
        //stopwatch.Restart();

        // creo stream e reader per leggere lo step
        MemoryStream deserialize_inputStream = new MemoryStream(decompressed_step);
        BinaryReader deserialize_binaryReader = new BinaryReader(deserialize_inputStream);

        //stopwatch.Stop();
        //Debug.Log("Metadata: " + stopwatch.ElapsedMilliseconds);
        //stopwatch.Restart();

        // estraggo la flag 'complete'
        bool complete = BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0);

        agent_prototypes = ((JSONArray)sim_prototype["agent_prototypes"]).Linq.ToList();
        generic_prototypes = ((JSONArray)sim_prototype["generic_prototypes"]).Linq.ToList();
        obstacle_prototypes = ((JSONArray)sim_prototype["obstacle_prototypes"]).Linq.ToList();

        List<string> agent_class_names = new Func<List<string>>(() => {
            List<string> array = new List<string>();
            foreach (JSONObject p in agent_prototypes)
            {
                array.Add(p["class"]);
            }
            return array;
        })();                                                                                  // nomi delle classi degli agenti
        List<string> generic_class_names = new Func<List<string>>(() => {
            List<string> array = new List<string>();
            foreach (JSONObject p in generic_prototypes)
            {
                array.Add(p["class"]);
            }
            return array;
        })();                                                                                // nomi delle classi degli oggetti
        List<string> obstacle_class_names = new Func<List<string>>(() => {
            List<string> array = new List<string>();
            foreach (JSONObject p in obstacle_prototypes)
            {
                array.Add(p["class"]);
            }
            return array;
        })();                                                                               // nomi delle classi degli ostacoli
        List<List<(string, string)>> agent_params_for_each_class = new Func<List<List<(string, string)>>>(() => {
                List<List<(string, string)>> array = new List<List<(string, string)>>();
                for (int i = 0; i < agent_class_names.Count; i++)
                {
                    JSONObject c = (JSONObject)agent_prototypes.ElementAt(i);
                    array.Add(new List<(string, string)>());
                    foreach (JSONObject p in ((JSONArray)c["params"]))
                    {
                        array[i].Add((p["name"], p["type"]));
                    }
                }
                return array;
            })();                                        // (nome_param, tipo_param) per ogni parametro per ogni classe di agente
        List<List<(string, string)>> generic_params_for_each_class = new Func<List<List<(string, string)>>>(() =>
            {
                List<List<(string, string)>> array = new List<List<(string, string)>>();
                for (int i = 0; i < generic_class_names.Count; i++)
                {
                    JSONObject c = (JSONObject)generic_prototypes.ElementAt(i);
                    array.Add(new List<(string, string)>());
                    foreach (JSONObject p in ((JSONArray)c["params"]))
                    {
                        array[i].Add((p["name"], p["type"]));
                    }
                }
                return array;
            })();                                       // (nome_param, tipo_param) per ogni parametro per ogni classe di oggetto
        List<List<(string, string)>> obstacle_params_for_each_class = new Func<List<List<(string, string)>>>(() =>
        {
            List<List<(string, string)>> array = new List<List<(string, string)>>();
            for (int i = 0; i < obstacle_class_names.Count; i++)
            {
                JSONObject c = (JSONObject)obstacle_prototypes.ElementAt(i);
                array.Add(new List<(string, string)>());
                foreach (JSONObject p in ((JSONArray)c["params"]))
                {
                    array[i].Add((p["name"], p["type"]));
                }
            }
            return array;
        })();                                      // (nome_param, tipo_param) per ogni parametro per ogni classe di oggetto

        if (!complete)
        {
            d_agent_params_for_each_class = new Func<List<List<(string, string)>>>(() => {
                    List<List<(string, string)>> array = new List<List<(string, string)>>();
                    for (int i = 0; i < agent_class_names.Count; i++)
                    {
                        JSONObject c = (JSONObject)agent_prototypes.ElementAt(i);
                        array.Add(new List<(string, string)>());
                        foreach (JSONObject p in ((JSONArray)c["params"]).Linq.Where((o) => (bool)((JSONObject)o)["is_in_step"]))
                        {
                            array[i].Add((p["name"], p["type"]));
                        }
                    }
                    return array;
                })();                                                               // (nome_param, tipo_param) per ogni parametro dynamic per ogni classe di agente
            d_generic_params_for_each_class = new Func<List<List<(string, string)>>>(() =>
                {
                    List<List<(string, string)>> array = new List<List<(string, string)>>();
                    for (int i = 0; i < generic_class_names.Count; i++)
                    {
                        JSONObject c = (JSONObject)generic_prototypes.ElementAt(i);
                        array.Add(new List<(string, string)>());
                        foreach (JSONObject p in ((JSONArray)c["params"]).Linq.Where((o) => (bool)((JSONObject)o)["is_in_step"]))
                        {
                            array[i].Add((p["name"], p["type"]));
                        }
                    }
                    return array;
                })();                                                              // (nome_param, tipo_param) per ogni parametro dynamic per ogni classe di oggetto
            d_obstacle_params_for_each_class = new Func<List<List<(string, string)>>>(() =>
            {
                List<List<(string, string)>> array = new List<List<(string, string)>>();
                for (int i = 0; i < obstacle_class_names.Count; i++)
                {
                    JSONObject c = (JSONObject)obstacle_prototypes.ElementAt(i);
                    array.Add(new List<(string, string)>());
                    foreach (JSONObject p in ((JSONArray)c["params"]).Linq.Where((o) => (bool)((JSONObject)o)["is_in_step"]))
                    {
                        array[i].Add((p["name"], p["type"]));
                    }
                }
                return array;
            })();                                                             // (nome_param, tipo_param) per ogni parametro dynamic per ogni classe di ostacolo
        }
        
        int n_agents_classes = agent_prototypes.Count();                                                                                                        // numero di classi di agenti aggiornati nello step
        int[] n_agents_for_each_class = new int[n_agents_classes];                                                                                              // numero di agenti di ogni classe presenti nello step
        int n_generics_classes = generic_prototypes.Count();                                                                                                    // numero di classi di oggetti aggiornati nello step
        int[] n_generics_for_each_class = new int[n_generics_classes];                                                                                          // numero di oggetti di ogni classe presenti nello step
        int n_obstacles_classes = obstacle_prototypes.Count();                                                                                                  // numero di classi di ostacoli aggiornati nello step
        int[] n_obstacles_for_each_class = new int[n_obstacles_classes];                                                                                        // numero di ostacoli di ogni classe presenti nello step
        List<char> dimensions = new Func<List<char>>(() => {
            List<char> array = new List<char>();
            foreach (JSONObject d in (JSONArray)sim_prototype["dimensions"])
            {
                array.Add(d["name"].ToString()[1]);
            }
            return array;
        })();                                                                                              // dimensioni della sim come una lista di char (es. ['x', 'y', 'z'])


        // estraggo l'ID
        CurrentSimStep = BitConverter.ToInt64(deserialize_binaryReader.ReadBytes(8).Reverse().ToArray(), 0);
        // estraggo il numero di agenti per classe presenti nello step
        for (int i = 0; i < n_agents_classes; i++)
        {
            n_agents_for_each_class[i] = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
        }
        // estraggo il numero di oggetti per classe presenti nello step
        for (int i = 0; i < n_generics_classes; i++)
        {
            n_generics_for_each_class[i] = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
        }
        // estraggo il numero di ostacoli per classe presenti nello step
        for (int i = 0; i < n_obstacles_classes; i++)
        {
            n_obstacles_for_each_class[i] = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
        }

        //stopwatch.Stop();
        //Debug.Log("ID + numAgents + numObjects: " + stopwatch.ElapsedMilliseconds);
        //stopwatch.Restart();


        // AGENTI
        // estraggo ogni agente per classe
        for (int i = 0; i < n_agents_classes; i++)                                                                   // i è la classe
        {
            int n_agents_of_a_class = n_agents_for_each_class[i];

            if (n_agents_of_a_class == 0) { continue; }                                                              //se non ci sono elementi di quella classe è inutile continuare

            for (int j = 0; j < n_agents_of_a_class; j++)                                                            // j è l'agente
            {
                int id = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);

                parameters = complete ? agent_params_for_each_class : BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0) ? agent_params_for_each_class : d_agent_params_for_each_class;

                if (!Agents.TryGetValue((agent_class_names[i], id), out so))                                          // l'agente è nuovo e dobbiamo crearlo e leggere tutti i parametri anche quelli non dynamic
                {
                    Agent_prototypes.TryGetValue(agent_class_names[i], out SimObject a);
                    
                    so = a.Clone();
                    so.Type = SimObject.SimObjectType.AGENT;
                    so.Class_name = agent_class_names[i];
                    so.Id = id;
                    AddAgent(so);

                    // to get all params
                    parameters = agent_params_for_each_class;
                }

                if (so.Is_in_step && !so.To_keep_if_not_in_step)
                {
                    toDeleteIfNotInStep.Remove((so.Type, so.Class_name, so.Id));
                    temp.Add((so.Type, so.Class_name, so.Id));
                }

                // all/object params
                foreach ((string, string) p in parameters[i])

                {
                    switch (p.Item2)
                    {
                        case "System.Position":
                            foreach (char d in "xyz")
                            {
                                if (dimensions.Contains(d))
                                {
                                    if(d.Equals('x')) x = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    else if(d.Equals('y')) y = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    else if(d.Equals('z')) z = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                }
                            }
                            if (dimensions.Count == 2) value = new Vector2(x, y); else value = new Vector3(x, y, z);
                            so.UpdateParameter("position", value);
                            break;
                        case "System.Cells":
                            JSONObject a_p = (JSONObject)agent_prototypes.ElementAt(i);
                            int count = ((JSONObject)((JSONArray)a_p["params"]).Linq.ToList()[0])["cells_number"];
                            if (dimensions.Count == 2)
                            {
                                value = new MyList<Vector2Int>();
                                for (int q = 0; q < count; q++)
                                {
                                    x = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    y = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    cell = new Vector2Int((int)x, (int)y);
                                    ((MyList<Vector2Int>)value).Add((Vector2Int)cell);
                                }
                            }
                            else
                            {
                                value = new MyList<Vector3Int>();
                                for (int q = 0; q < count; q++)
                                {
                                    x = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    y = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    z = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    cell = new Vector3Int((int)x, (int)y, (int)z);
                                    ((MyList<Vector3Int>)value).Add((Vector3Int)cell);
                                }
                            }                            
                            so.UpdateParameter("position", value);
                            break;
                        case "System.Single":
                            so.UpdateParameter(p.Item1, BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0));
                            break;
                        case "System.Int32":
                            so.UpdateParameter(p.Item1, BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0));
                            break;
                        case "System.Boolean":
                            so.UpdateParameter(p.Item1, BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0));
                            break;
                        case "System.String":
                            so.UpdateParameter(p.Item1, BitConverter.ToString(deserialize_binaryReader.ReadBytes(20).Reverse().ToArray(), 0));
                            break;
                    }
                }
            }
        }

        //stopwatch.Stop();
        //Debug.Log("AGENTS: " + stopwatch.ElapsedMilliseconds);
        //stopwatch.Restart();

        //stopwatch.Start();

        // OGGETTI
        // estraggo ogni oggetto per classe
        for (int i = 0; i < n_generics_for_each_class.Length; i++)                                                   // i è la classe
        {
            int n_generics_of_a_class = n_generics_for_each_class[i];

            if (n_generics_of_a_class == 0) { continue; }                                                            //se non ci sono elementi di quella classe è inutile continuare

            for (int j = 0; j < n_generics_of_a_class; j++)                                                          // j è l'oggetto
            {
                int id = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);

                parameters = complete ? generic_params_for_each_class : BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0) ? generic_params_for_each_class : d_generic_params_for_each_class;

                if (!Generics.TryGetValue((generic_class_names[i], id), out so))                                    // l'oggetto è nuovo e dobbiamo crearlo e leggere tutti i parametri anche quelli non object
                {
                    Generic_prototypes.TryGetValue(generic_class_names[i], out SimObject g);

                    so = g.Clone();
                    so.Type = SimObject.SimObjectType.GENERIC;
                    so.Class_name = generic_class_names[i];
                    so.Id = id;
                    AddGeneric(so);
                }

                if (so.Is_in_step && !so.To_keep_if_not_in_step)
                {
                    toDeleteIfNotInStep.Remove((so.Type, so.Class_name, so.Id));
                    temp.Add((so.Type, so.Class_name, so.Id));
                }

                // all/object params
                foreach ((string, string) p in parameters[i])
                {
                    switch (p.Item2)
                    {
                        case "System.Position":
                            foreach (char d in "xyz")
                            {
                                if (dimensions.Contains(d))
                                {
                                    if (d.Equals('x')) x = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    else if (d.Equals('y')) y = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    else if (d.Equals('z')) z = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                }
                            }
                            if (dimensions.Count == 2) value = new Vector2(x, y); else value = new Vector3(x, y, z);
                            so.UpdateParameter("position", value);
                            break;
                        case "System.Cells":
                            JSONObject g_p = (JSONObject)generic_prototypes.ElementAt(i);
                            int count = ((JSONObject)((JSONArray)g_p["params"]).Linq.ToList()[0])["cells_number"];
                            if (dimensions.Count == 2)
                            {
                                value = new MyList<Vector2Int>();
                                for (int q = 0; q < count; q++)
                                {
                                    x = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    y = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    cell = new Vector2Int((int)x, (int)y);
                                    ((MyList<Vector2Int>)value).Add((Vector2Int)cell);
                                }
                            }
                            else
                            {
                                value = new MyList<Vector3Int>();
                                for (int q = 0; q < count; q++)
                                {
                                    x = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    y = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    z = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    cell = new Vector3Int((int)x, (int)y, (int)z);
                                    ((MyList<Vector3Int>)value).Add((Vector3Int)cell);
                                }
                            }
                            so.UpdateParameter("position", value);
                            break;
                        case "System.Single":
                            so.UpdateParameter(p.Item1, BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0));
                            break;
                        case "System.Int32":
                            so.UpdateParameter(p.Item1, BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0));
                            break;
                        case "System.Boolean":
                            so.UpdateParameter(p.Item1, BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0));
                            break;
                        case "System.String":
                            so.UpdateParameter(p.Item1, BitConverter.ToString(deserialize_binaryReader.ReadBytes(20).Reverse().ToArray(), 0));
                            break;
                    }
                }
            }
        }

        //stopwatch.Stop();
        //Debug.Log("Generics: " + stopwatch.ElapsedMilliseconds);
        //stopwatch.Restart();

        // OSTACOLI
        // estraggo ogni ostacolo per classe
        for (int i = 0; i < n_obstacles_for_each_class.Length; i++)                                                   // i è la classe
        {
            int n_obstacles_of_a_class = n_obstacles_for_each_class[i];

            if (n_obstacles_of_a_class == 0) { continue; }                                                            //se non ci sono elementi di quella classe è inutile continuare

            for (int j = 0; j < n_obstacles_of_a_class; j++)                                                          // j è l'ostacolo
            {
                int id = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);

                parameters = complete ? obstacle_params_for_each_class : BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0) ? obstacle_params_for_each_class : d_obstacle_params_for_each_class;

                if (!Obstacles.TryGetValue((obstacle_class_names[i], id), out so))                                    // l'oggetto è nuovo e dobbiamo crearlo e leggere tutti i parametri anche quelli non object
                {
                    Obstacle_prototypes.TryGetValue(obstacle_class_names[i], out SimObject o);

                    so = o.Clone();
                    so.Type = SimObject.SimObjectType.OBSTACLE;
                    so.Class_name = obstacle_class_names[i];
                    so.Id = id;
                    AddObstacle(so);
                }

                if (so.Is_in_step && !so.To_keep_if_not_in_step)
                {
                    toDeleteIfNotInStep.Remove((so.Type, so.Class_name, so.Id));
                    temp.Add((so.Type, so.Class_name, so.Id));
                }

                // all/object params
                foreach ((string, string) p in parameters[i])
                {
                    switch (p.Item2)
                    {
                        case "System.Position":
                            foreach (char d in "xyz")
                            {
                                if (dimensions.Contains(d))
                                {
                                    if (d.Equals('x')) x = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    else if (d.Equals('y')) y = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    else if (d.Equals('z')) z = BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                }
                            }
                            if (dimensions.Count == 2) value = new Vector2(x, y); else value = new Vector3(x, y, z);
                            so.UpdateParameter("position", value);
                            break;
                        case "System.Cells":
                            JSONObject o_p = (JSONObject)obstacle_prototypes.ElementAt(i);
                            int count = ((JSONObject)((JSONArray)o_p["params"]).Linq.ToList()[0])["cells_number"];
                            if (dimensions.Count == 2)
                            {
                                value = new MyList<Vector2Int>();
                                for (int q = 0; q < count; q++)
                                {
                                    x = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    y = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    cell = new Vector2Int((int)x, (int)y);
                                    ((MyList<Vector2Int>)value).Add((Vector2Int)cell);
                                }
                            }
                            else
                            {
                                value = new MyList<Vector3Int>();
                                for (int q = 0; q < count; q++)
                                {
                                    x = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    y = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    z = BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                                    cell = new Vector3Int((int)x, (int)y, (int)z);
                                    ((MyList<Vector3Int>)value).Add((Vector3Int)cell);
                                }
                            }
                            so.UpdateParameter("position", value);
                            break;
                        case "System.Single":
                            so.UpdateParameter(p.Item1, BitConverter.ToSingle(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0));
                            break;
                        case "System.Int32":
                            so.UpdateParameter(p.Item1, BitConverter.ToInt32(deserialize_binaryReader.ReadBytes(4).Reverse().ToArray(), 0));
                            break;
                        case "System.Boolean":
                            so.UpdateParameter(p.Item1, BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0));
                            break;
                        case "System.String":
                            so.UpdateParameter(p.Item1, BitConverter.ToString(deserialize_binaryReader.ReadBytes(20).Reverse().ToArray(), 0));
                            break;
                    }
                }
            }
        }

        //stopwatch.Stop();
        //Debug.Log("Obstacles: " + stopwatch.ElapsedMilliseconds);
        //Debug.Log("Step bytes: " + decompressed_step.Length);
        //UnityEngine.Debug.Log("Simulation updated from step: " + currentsimstep);

        // Elimina SimObject non aggiornati
        toDeleteIfNotInStep.ForEach((so) => {
            switch (so.type)
            {
                case SimObject.SimObjectType.AGENT:
                    Agents.TryRemove((so.class_name, so.id), out _);
                    break;
                case SimObject.SimObjectType.GENERIC:
                    Generics.TryRemove((so.class_name, so.id), out _);
                    break;
                case SimObject.SimObjectType.OBSTACLE:
                    Obstacles.TryRemove((so.class_name, so.id), out _);
                    break;
            }
            SimObjectDeleteEventArgs e = new SimObjectDeleteEventArgs();
            e.type = so.type;
            e.class_name = so.class_name;
            e.id = so.id;
            OnSimObjectNotInStepEventHandler?.Invoke(this, e);
        });
        toDeleteIfNotInStep.Clear();
        toDeleteIfNotInStep.AddRange(temp);
        temp.Clear();
    }

    /// <summary>
    /// Update Simulation from uncommited_update
    /// </summary>
    public void UpdateSimulationFromEdit(JSONObject sim_updateJSON, ConcurrentDictionary<(string, (SimObject.SimObjectType, string, int)), SimObject> sim_update)
    {
        SimObject x = defaultSimObject.Clone();
        IEnumerable<KeyValuePair<(string class_name, int id), SimObject>> objs;
        MyList<(string, int)> keys_to_remove = new MyList<(string, int)>();
        var dict = new ConcurrentDictionary<(string class_name, int id), SimObject>();


        // SIM_PARAMS
        if (!sim_updateJSON["sim_params"].Equals(null))
        {
            JSONObject parameters = (JSONObject)sim_updateJSON["sim_params"];
            foreach (KeyValuePair<string, JSONNode> p in parameters.Dict)
            {
                Parameters[p.Key] = p.Value;
            }
        }        

        // AGENTS/GENERICS/OBSTACLES
        foreach (KeyValuePair<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> entry in sim_update)
        {
            // prendo il dict giusto
            switch (entry.Key.obj.type)
            {
                case SimObject.SimObjectType.AGENT:
                    dict = Agents;
                    break;
                case SimObject.SimObjectType.GENERIC:
                    dict = Generics;
                    break;
                case SimObject.SimObjectType.OBSTACLE:
                    dict = Obstacles;
                    break;
                default:
                    objs = null;
                    break;
            }

            // entry per un'intera classe (questo significa che contiene solo i parametri modificati)
            if (entry.Key.obj.id.Equals(-1))
            {
                objs = dict.Where(obj => obj.Key.class_name.Equals(entry.Key.obj.class_name));

                if (entry.Key.op.Equals("MOD")) {
                    foreach (KeyValuePair<(string class_name, int id), SimObject> o in objs)
                    {
                        foreach (KeyValuePair<string, object> p in entry.Value.Parameters)
                        {
                            o.Value.UpdateParameter(p.Key, p.Value);
                        }
                    }
                }
                else if (entry.Key.op.Equals("DEL"))
                {
                    foreach (KeyValuePair<(string class_name, int id), SimObject> e in objs)
                    {
                        keys_to_remove.Add(e.Key);
                    }
                    foreach((string, int) k in keys_to_remove)
                    {
                        dict.TryRemove(k, out _);
                    }
                }
            }
            // entry per un singolo oggetto
            else
            {
                if (entry.Key.op.Equals("MOD"))
                {
                    dict.TryGetValue((entry.Key.obj.class_name, entry.Key.obj.id), out x);
                    x = entry.Value;
                }
                else if (entry.Key.op.Equals("DEL"))
                {
                    dict.TryRemove((entry.Key.obj.class_name, entry.Key.obj.id), out _);
                }                    
            }
        }
    }

    public override string ToString()
    {
        return
            
            "ID: " + id + "\n" + "" +
            "NAME: " + name + "\n" +
            "DESCRIPTION: " + description + "\n" +
            "TYPE: " + type + "\n" +
            "DIMENSIONS: " + string.Join("  ", dimensions) + "\n" +
            "SIM PARAMS: \n" + string.Join("\n", parameters) + "\n" +
            "AGENTS PROTOTYPES: \n" + string.Join("\n", Agent_prototypes) + "\n" +
            "GENERICS PROTOTYPES: \n" + string.Join("\n", Generic_prototypes) + "\n" +
            "editable_in_play: " + string.Join("  ", editableInPlay) + "\n" +
            "editable_in_init: " + string.Join("  ", editableInInit) + "\n" +
            "editable_in_pause: " + string.Join("  ", editableInPause) + "\n" + "\n" +
            "CURRENT SIM STEP: " + currentSimStep.ToString() + "\n" +
            "AGENTS: \n" + string.Join("\n", agents) + "\n" +
            "GENERICS: \n" + string.Join("\n", generics) + "\n" +
            "OBSTACLES: \n" + string.Join("\n", obstacles) + "\n";
    }
}
