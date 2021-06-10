using System.Collections.Generic;

public class Simulation
{
    //Simulation Info
    private string name, description, type;
    private int id, dimensions;
    private List<Agent> agent_prototypes;
    private List<Obstacle> obstacle_prototypes;
    private List<Generic> generic_prototypes;
    private Dictionary<string, object> parameters = new Dictionary<string, object>();
    private Dictionary<string, Agent> agents = new Dictionary<string, Agent>();
    private Dictionary<string, Obstacle> obstacles = new Dictionary<string, Obstacle>();
    private Dictionary<string, Generic> generics = new Dictionary<string, Generic>();
    private List<string> editableInPlay;
    private List<string> editableInPause;
    private long currentSimStep;

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
    public int Dimensions { get => dimensions; set => dimensions = value; }
    public List<Agent> Agent_prototypes { get => agent_prototypes; set => agent_prototypes = value; }
    public List<Obstacle> Obstacle_prototypes { get => obstacle_prototypes; set => obstacle_prototypes = value; }
    public List<Generic> Generic_prototypes { get => generic_prototypes; set => generic_prototypes = value; }
    public Dictionary<string, object> Parameters { get => parameters; set => parameters = value; }
    public StateEnum State { get => state; set => state = value; }
    public int Id { get => id; set => id = value; }
    public List<string> EditableInPlay { get => editableInPlay; set => editableInPlay = value; }
    public List<string> EditableInPause { get => editableInPause; set => editableInPause = value; }
    public Dictionary<string, Agent> Agents { get => agents; set => agents = value; }
    public Dictionary<string, Obstacle> Obstacles { get => obstacles; set => obstacles = value; }
    public Dictionary<string, Generic> Generics { get => generics; set => generics = value; }
    public long CurrentSimStep { get => currentSimStep; set => currentSimStep = value; }

    public Simulation(){}
    




    public object GetParameter(string param_name)
    {
        object parameter;
        Parameters.TryGetValue(param_name, out parameter);
        return (!parameter.Equals(null)) ? parameter : false;
    }
    public bool AddParameter(string param_name, object value)
    {
        if (!Parameters.ContainsKey(param_name))
        {
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }
    public bool UpdateParameter(string param_name, object value)
    {
        if (Parameters.ContainsKey(param_name))
        {
            Parameters.Remove(param_name);
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }
    
}
