using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Simulation
{
    //Simulation Info
    private string name, description;
    private SimType type;
    private int id;
    private Dictionary<string, dynamic> dimensions = new Dictionary<string, dynamic>();
    private Dictionary<string, SimObject> agent_prototypes = new Dictionary<string, SimObject>();
    private Dictionary<string, SimObject> generic_prototypes = new Dictionary<string, SimObject>();
    private Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
    private List<string> editableInPlay = new List<string>();
    private List<string> editableInPause = new List<string>();
    // Runtime Info
    private long currentSimStep = 0;
    private Dictionary<string,int> n_agents_for_each_class = new Dictionary<string, int>();
    private Dictionary<string,int> n_generics_for_each_class = new Dictionary<string, int>();
    private Dictionary<(string class_name, int id), SimObject> agents = new Dictionary<(string, int), SimObject>();
    private Dictionary<(string class_name, int id), SimObject> generics = new Dictionary<(string, int), SimObject>();
    private Dictionary<(string class_name, int id), SimObject> obstacles = new Dictionary<(string, int), SimObject>();

    public enum StateEnum
    {
        PLAY = 1,
        PAUSE = 2,
        STOP = 3
    }
    public enum SimType
    {
        DISCRETE = 1,
        CONTINUOUS = 2
    }

    private StateEnum state;

    public string Name { get => name; set => name = value; }
    public string Description { get => description; set => description = value; }
    public SimType Type { get => type; set => type = value; }
    public Dictionary<string, dynamic> Dimensions { get => dimensions; set => dimensions = value; }
    public Dictionary<string, SimObject> Agent_prototypes { get => agent_prototypes; set => agent_prototypes = value; }
    public Dictionary<string, SimObject> Generic_prototypes { get => generic_prototypes; set => generic_prototypes = value; }
    public Dictionary<string, dynamic> Parameters { get => parameters; set => parameters = value; }
    public StateEnum State { get => state; set => state = value; }
    public int Id { get => id; set => id = value; }
    public List<string> EditableInPlay { get => editableInPlay; set => editableInPlay = value; }
    public List<string> EditableInPause { get => editableInPause; set => editableInPause = value; }
    public long CurrentSimStep { get => currentSimStep; set => currentSimStep = value; }
    public Dictionary<string, int> N_agents_for_each_class { get => n_agents_for_each_class; set => n_agents_for_each_class = value; }
    public Dictionary<string, int> N_generics_for_each_class { get => n_generics_for_each_class; set => n_generics_for_each_class = value; }
    public Dictionary<(string class_name, int id), SimObject> Agents { get => agents; set => agents = value; }
    public Dictionary<(string class_name, int id), SimObject> Obstacles { get => obstacles; set => obstacles = value; }
    public Dictionary<(string class_name, int id), SimObject> Generics { get => generics; set => generics = value; }

    public Simulation(){}

    /// Add/Remove Agents/Generics Prototypes ///
    public void AddAgentPrototype(SimObject a)
    {
        agent_prototypes.Add(a.Class_name, a);
    }
    public void AddGenericPrototype(SimObject g)
    {
        generic_prototypes.Add(g.Class_name, g);
    }

    /// Add/Remove Agents/Generics/Obstacles ///
    public void AddAgent(SimObject a)
    {
        Agents.Add((a.Class_name, a.Id), a);
    }
    public void RemoveAgent(SimObject a)
    {
        Agents.Remove((a.Class_name, a.Id));
    }
    public void RemoveAgent(int id, string class_name)
    {
        Agents.Remove((class_name,id));
    }
    public void AddGeneric(SimObject g)
    {
        Generics.Add((g.Class_name, g.Id), g);
    }
    public void RemoveGeneric(SimObject g)
    {
        Generics.Remove((g.Class_name, g.Id));
    }
    public void RemoveGeneric(int id, string class_name)
    {
        Generics.Remove((class_name, id));
    }
    public void AddObstacle(SimObject o)
    {
        Obstacles.Add((o.Class_name, o.Id), o);
    }
    public void RemoveObstacle(SimObject o)
    {
        Obstacles.Remove((o.Class_name, o.Id));
    }
    public void RemoveObstacle(string class_name, int id)
    {
        Obstacles.Remove((class_name, id));
    }

    /// Get/Add/Update Sim params ///
    
    public dynamic GetParameter(string param_name)
    {
        dynamic parameter;
        Parameters.TryGetValue(param_name, out parameter);
        return (!parameter.Equals(null)) ? parameter : false;
    }
    public bool AddParameter(string param_name, dynamic value)
    {
        if (!Parameters.ContainsKey(param_name))
        {
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }
    public bool UpdateParameter(string param_name, dynamic value)
    {
        if (Parameters.ContainsKey(param_name))
        {
            Parameters.Remove(param_name);
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }


    /// Utils ///

    /// <summary>
    /// Get JSONObject from Simulation
    /// </summary>
    public JSONObject SimulationToJSON()
    {
        JSONObject sim_json = new JSONObject();

        // TODO

        return sim_json;
    }

    /// <summary>
    /// Load/Init Simulation from JSON
    /// </summary>
    public void InitSimulationFromPrototype(JSONObject sim_edited_prototype)
    {
        // Get Sim infos
        Id = sim_edited_prototype["id"];
        Name = sim_edited_prototype["name"];
        Description = sim_edited_prototype["description"];
        Type = sim_edited_prototype["type"] == "DISCRETE" ? SimType.DISCRETE : SimType.CONTINUOUS;

        dynamic value;
        
        // Get Sim dimensions
        JSONArray dimensions = (JSONArray)sim_edited_prototype["dimensions"];
        foreach (JSONObject d in dimensions)
        {
            switch ((string)d["type"])
            {
                case "System.Single":
                    value = (float)d["default"];
                    break;
                case "System.Int32":
                    value = (int)d["default"];
                    break;
                default:
                    value = null;
                    break;
            }
            Dimensions.Add(d["name"], value);
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
            if ((bool)p["editable_in_pause"])
            {
                EditableInPause.Add(p["name"]);
            }
        }

        // Get n_agents/generics_for_each_class
        // Create Agents/Generics prototypes to be used when instantiating Agents/Generics
        JSONArray agent_prototypes = (JSONArray)sim_edited_prototype["agent_prototypes"];
        foreach (JSONObject agent in agent_prototypes)
        {
            N_agents_for_each_class.Add(agent["class"], (int)agent["default"]);
            SimObject a = new SimObject(agent["class"]);

            a.Type = SimObject.SimObjectType.AGENT;

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
                        value = new MyList<(string, float)>();
                        foreach (string dimension in Dimensions.Keys)
                        {
                            value.Add((dimension, (float)p["position"][dimension]));
                        }
                        break;
                    case "System.Cells":
                        value = new MyList<MyList<(string, int)>>();
                        foreach (JSONNode c in (JSONArray)p["cells"])
                        {
                            var coordinates = new MyList<(string, int)>();
                            foreach (string dimension in Dimensions.Keys)
                            {
                                coordinates.Add((dimension, c[dimension]));
                            }
                            value.Add(coordinates);
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
                if ((bool)p["editable_in_pause"])
                {
                    EditableInPause.Add(p["name"]);
                }
            }
            AddAgentPrototype(a);
        }
        JSONArray generic_prototypes = (JSONArray)sim_edited_prototype["generic_prototypes"];
        foreach (JSONObject generic in generic_prototypes)
        {
            N_generics_for_each_class.Add(generic["class"], (int)generic["default"]);
            SimObject g = new SimObject(generic["class"]);

            g.Type = SimObject.SimObjectType.GENERIC;

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
                        value = new MyList<(string, float)>();
                        foreach (string dimension in Dimensions.Keys)
                        {
                            value.Add((dimension, (float)p["position"][dimension]));
                        }
                        break;
                    case "System.Cells":
                        value = new MyList<MyList<(string, int)>>();
                        foreach (JSONNode c in (JSONArray)p["cells"])
                        {
                            var coordinates = new MyList<(string, int)>();
                            foreach (string dimension in Dimensions.Keys)
                            {
                                coordinates.Add((dimension, c[dimension]));
                            }
                            value.Add(coordinates);
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
                if ((bool)p["editable_in_pause"])
                {
                    EditableInPause.Add(p["name"]);
                }
            }
            AddGenericPrototype(g);
        }

        // Instantiate Agents/Generics needed
        foreach (KeyValuePair<string,int> a4c in N_agents_for_each_class)
        {
            this.agent_prototypes.TryGetValue(a4c.Key, out SimObject a);
            for (int i = 0; i<a4c.Value; i++)
            {
                SimObject clone = a.Clone();
                clone.Type = SimObject.SimObjectType.AGENT;
                clone.Id = i;
                AddAgent(clone);                                                                                            // gestire i parametri dell'agente se ci sono
            }
        }
        foreach (KeyValuePair<string, int> g4c in N_generics_for_each_class)
        {
            this.generic_prototypes.TryGetValue(g4c.Key, out SimObject g);
            for (int i = 0; i < g4c.Value; i++)
            {
                SimObject clone = g.Clone();
                clone.Type = SimObject.SimObjectType.GENERIC;
                clone.Id = i;
                AddGeneric(clone);                                                                                          // gestire i parametri dell'agente se ci sono
            }
        }

    }

    /// <summary>
    /// Extract byte[] step and update Simulation accordingly 
    /// </summary>
    public void UpdateSimulationFromStep(byte[] step, JSONObject sim_prototype)
    {
        // variabili
        byte[] decompressed_step = Utils.DecompressStepPayload(step);
        SimObject x = new SimObject();
        MyList<(string, float)> coords = new MyList<(string, float)>();



        List<string> agent_class_names = new Func<List<string>>(() => {
            List<string> array = new List<string>();
            foreach (JSONObject p in (JSONArray)sim_prototype["agent_prototypes"])
            {
                array.Add(p["name"]);
            }
            return array;
        })();                                     // nomi delle classi degli agenti
        List<string> generic_class_names = new Func<List<string>>(() => {
            List<string> array = new List<string>();
            foreach (JSONObject p in (JSONArray)sim_prototype["generic_prototypes"])
            {
                array.Add(p["name"]);
            }
            return array;
        })();                                   // nomi delle classi degli oggetti
        List<List<(string, string)>> agent_params_for_each_class =
            new Func<List<List<(string, string)>>>(() => {
                List<List<(string, string)>> array = new List<List<(string, string)>>();
                for (int i = 0; i < ((JSONArray)sim_prototype["agent_prototypes"]).Count; i++)
                {
                    JSONObject c = (JSONObject)((JSONArray)sim_prototype["agent_prototypes"])[i];
                    array.Add(new List<(string, string)>());
                    foreach (JSONObject p in c["params"] as JSONArray)
                    {
                        array[i].Add((p["name"], p["type"]));
                    }
                }
                return array;
            })();                                             // (nome_param, tipo_param) per ogni parametro per ogni classe di agente 
        List<List<(string, string)>> generic_params_for_each_class =
            new Func<List<List<(string, string)>>>(() =>
            {
                List<List<(string, string)>> array = new List<List<(string, string)>>();
                for (int i = 0; i < ((JSONArray)sim_prototype["generic_prototypes"]).Count; i++)
                {
                    JSONObject c = (JSONObject)((JSONArray)sim_prototype["generic_prototypes"])[i];
                    array.Add(new List<(string, string)>());
                    foreach (JSONObject p in c["params"] as JSONArray)
                    {
                        array[i].Add((p["name"], p["type"]));
                    }
                }
                return array;
            })();                                              // (nome_param, tipo_param) per ogni parametro per ogni classe di oggetto 
        int n_agents_classes = ((JSONArray)sim_prototype["agent_prototypes"]).Count;                              // numero di classi di agenti aggiornati nello step
        int[] n_agents_for_each_class = new int[n_agents_classes];                                                // numero di agenti di ogni classe presenti nello step
        int n_generic_classes = ((JSONArray)sim_prototype["generic_prototypes"]).Count;                           // numero di classi di oggetti aggiornati nello step
        int[] n_objects_for_each_class = new int[n_generic_classes];                                              // numero di oggetti di ogni classe presenti nello step
        List<char> dimensions = new Func<List<char>>(() => {
            List<char> array = new List<char>();
            foreach (JSONObject d in (JSONArray)sim_prototype["dimensions"])
            {
                UnityEngine.Debug.Log(d["name"].ToString());
                array.Add(d["name"].ToString()[1]);
            }
            return array;
        })();                                                // dimensioni della sim come una lista di char (es. ['x', 'y', 'z'])

        // creo stream e reader per leggere lo step
        MemoryStream deserialize_inputStream = new MemoryStream(decompressed_step);
        BinaryReader deserialize_binaryReader = new BinaryReader(deserialize_inputStream);

        // estraggo l'ID
        Id = deserialize_binaryReader.ReadInt32();
        // estraggo il numero di agenti per classe presenti nello step
        for (int i = 0; i < n_agents_classes; i++)
        {
            n_agents_for_each_class[i] = deserialize_binaryReader.ReadInt32();
        }
        // estraggo il numero di oggetti per classe presenti nello step
        for (int i = 0; i < n_generic_classes; i++)
        {
            n_objects_for_each_class[i] = deserialize_binaryReader.ReadInt32();
        }

        // AGENTI
        // estraggo ogni agente per classe
        for (int i = 0; i < n_agents_classes; i++)                              // i è la classe
        {
            int n_agents_of_a_class = n_agents_for_each_class[i];

            if (n_agents_of_a_class == 0) { continue; }                                                            //se non ci sono elementi di quella classe è inutile continuare

            for (int j = 0; j < n_agents_of_a_class; j++)                       // j è l'agente
            {
                Agents.TryGetValue((agent_class_names[i], deserialize_binaryReader.ReadInt32()), out x);

                // position
                foreach (char d in "xyz")
                {
                    if (dimensions.Contains(d))
                    {
                        coords.Add((d + "", deserialize_binaryReader.ReadSingle()));
                    }
                    else
                    {
                        coords.Add((d + "", 0f));
                    }
                }
                x.UpdateParameter("position", coords);

                // other params
                foreach ((string, string) p in agent_params_for_each_class[i])
                {
                    switch (p.Item2)
                    {
                        case "System.Single":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadSingle());
                            break;
                        case "System.Int32":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadInt32());
                            break;
                        case "System.Boolean":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadBoolean());
                            break;
                        case "System.String":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadString());
                            break;
                    }
                }
            }
        }

        // OGGETTI
        // estraggo ogni oggetto per classe
        for (int i = 0; i < n_objects_for_each_class.Length; i++)                // i è la classe
        {
            int n_objects_of_a_class = n_objects_for_each_class[i];

            if (n_objects_of_a_class == 0) { continue; }                                                            //se non ci sono elementi di quella classe è inutile continuare

            for (int j = 0; j < n_objects_of_a_class; j++)                       // j è l'oggetto
            {

                Agents.TryGetValue((generic_class_names[i], deserialize_binaryReader.ReadInt32()), out x);

                // position
                foreach (char d in "xyz")
                {
                    if (dimensions.Contains(d))
                    {
                        coords.Add((d + "", deserialize_binaryReader.ReadSingle()));
                    }
                    else
                    {
                        coords.Add((d + "", 0f));
                    }
                }
                x.UpdateParameter("position", coords);

                // other params
                foreach ((string, string) p in generic_params_for_each_class[i])
                {
                    switch (p.Item2)
                    {
                        case "System.Single":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadSingle());
                            break;
                        case "System.Int32":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadInt32());
                            break;
                        case "System.Boolean":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadBoolean());
                            break;
                        case "System.String":
                            x.UpdateParameter(p.Item1, deserialize_binaryReader.ReadString());
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Update Simulation from uncommited_update
    /// </summary>
    public void UpdateSimulationFromEdit(JSONObject sim_updateJSON, Dictionary<(string, (SimObject.SimObjectType, string, int)), SimObject> sim_update)
    {
        SimObject x = new SimObject();
        IEnumerable<KeyValuePair<(string class_name, int id), SimObject>> objs;
        MyList<(string, int)> keys_to_remove = new MyList<(string, int)>();
        var dict = new Dictionary<(string class_name, int id), SimObject>();


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
                        foreach (KeyValuePair<string, dynamic> p in entry.Value.Parameters)
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
                        dict.Remove(k);
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
                    dict.Remove((entry.Key.obj.class_name, entry.Key.obj.id));
                }
                else
                {
                    dict.Add((entry.Key.obj.class_name, entry.Key.obj.id), entry.Value);
                }
                    
            }
        }
    }

    public override string ToString()
    {
        return
            
            "ID: " + id + "\n" + "" +
            "Name: " + name + "\n" +
            "Description: " + description + "\n" +
            "Type: " + type + "\n" +
            "Dimensions: " + string.Join("  ", dimensions) + "\n" +
            "Current sim step: " + currentSimStep.ToString() + "\n" +
            "Agent Prototypes: " + string.Join("  ", Agent_prototypes) + "\n" +
            "Generic Prototypes: " + string.Join("  ", Generic_prototypes) + "\n" +
            "Agents: " + string.Join("  ", agents) + "\n" +
            "Generics: " + string.Join("  ", generics) + "\n" +
            "Obstacles: " + string.Join("  ", obstacles) + "\n" +
            "Sim params: " + string.Join("  ", parameters) + "\n" +
            "Editable in play: " + string.Join("  ", editableInPlay) + "\n" +
            "Editable in pause: " + string.Join("  ", editableInPause) + "\n";
    }
}
