using SimpleJSON;
using System.Collections.Generic;

public class Simulation
{
    //Simulation Info
    private string name, description, type;
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
    private Dictionary<string, SimObject> agents = new Dictionary<string, SimObject>();
    private Dictionary<string, SimObject> generics = new Dictionary<string, SimObject>();
    private Dictionary<string, SimObject> obstacles = new Dictionary<string, SimObject>();

    public enum StateEnum
    {
        PLAY = 1,
        PAUSE = 2,
        STOP = 3
    }

    private StateEnum state;

    public string Name { get => name; set => name = value; }
    public string Description { get => description; set => description = value; }
    public string Type { get => type; set => type = value; }
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
    public Dictionary<string, SimObject> Agents { get => agents; set => agents = value; }
    public Dictionary<string, SimObject> Obstacles { get => obstacles; set => obstacles = value; }
    public Dictionary<string, SimObject> Generics { get => generics; set => generics = value; }

    public Simulation(){}

    // Add/Remove Agents/Generics Prototypes
    public void AddAgentPrototype(SimObject a)
    {
        agent_prototypes.Add(a.Class_name, a);
    }
    public void AddGenericPrototype(SimObject g)
    {
        generic_prototypes.Add(g.Class_name, g);
    }

    // Add/Remove Agents/Generics/Obstacles
    public void AddAgent(SimObject a)
    {
        Agents.Add(a.Class_name + "." + a.Id, a);
    }
    public void RemoveAgent(SimObject a)
    {
        Agents.Remove(a.Class_name + "." + a.Id);
    }
    public void RemoveAgent(string id, string class_name)
    {
        Agents.Remove(class_name + "." + id);
    }
    public void AddGeneric(SimObject g)
    {
        Generics.Add(g.Class_name + "." + g.Id, g);
    }
    public void RemoveGeneric(SimObject g)
    {
        Generics.Remove(g.Class_name + "." + g.Id);
    }
    public void RemoveGeneric(string id, string class_name)
    {
        Generics.Remove(class_name + "." + id);
    }
    public void AddObstacle(SimObject o)
    {
        Obstacles.Add(o.Class_name + "." + o.Id, o);
    }
    public void RemoveObstacle(SimObject o)
    {
        Obstacles.Remove(o.Class_name + "." + o.Id);
    }
    public void RemoveObstacle(string class_name, string id)
    {
        Obstacles.Remove(class_name + "." + id);
    }
    public void MoveObstacle(string class_name, int id, float x, float y, float z)
    {
        Obstacles.TryGetValue(class_name + "." + id, out SimObject o);
        o.X = x;
        o.Y = y;
        o.Z = z;
    }

    // Get/Add/Update Sim params

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


    // Utils

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
    public void InitSimulationFromJSONEditedPrototype(JSONObject sim_edited_prototype)
    {
        // Get Sim infos
        Id = sim_edited_prototype["id"];
        Name = sim_edited_prototype["name"];
        Description = sim_edited_prototype["description"];
        Type = sim_edited_prototype["type"];

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

            a.X = ((JSONObject)agent["position"])["x"];
            a.Y = ((JSONObject)agent["position"])["y"];
            a.Z = ((JSONObject)agent["position"])["z"];

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

            g.X = ((JSONObject)generic["position"])["x"];
            g.Y = ((JSONObject)generic["position"])["y"];
            g.Z = ((JSONObject)generic["position"])["z"];

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
                clone.Id = i;
                AddGeneric(clone);                                                                                          // gestire i parametri dell'agente se ci sono
            }
        }

    }

    /// <summary>
    /// Update Simulation from uncommited_update
    /// </summary>
    public void UpdateSimulationFromUpdate(JSONObject sim_updateJSON, Dictionary<string, SimObject> sim_update)
    {
        // SIM_PARAMS
        JSONObject parameters = (JSONObject)sim_updateJSON["sim_params"];
        foreach (KeyValuePair<string, JSONNode> p in parameters.Dict)
        {
            Parameters[p.Key] = p.Value;
        }

        // AGENTS
        


        // OBSTACLES
        


        // GENERICS
        


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
