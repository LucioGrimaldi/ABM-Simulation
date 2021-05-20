using System.Collections.Generic;

public class Simulation
{
    //Simulation Info
    private string name, description, type;
    private int dimensions, id;
    private List<Agent> agent_prototypes;
    private List<Obstacle> obstacle_prototypes;
    private List<Generic> generic_prototypes;
    private Dictionary<string, string> parameters = new Dictionary<string, string>();
    private List<Agent> agents = new List<Agent>();
    private List<Obstacle> obstacles = new List<Obstacle>();
    private List<Generic> generics = new List<Generic>();
    private List<string> editableInPlay;
    private List<string> editableInPause;

    public enum StateEnum
    {
        PLAY = 1,                   // Simulation is in PLAY
        PAUSE = 2,                  // Simulation is in PAUSE
        STOP = 3                    // Simulation is in STOP (settings not yet send)
    }

    private StateEnum state = StateEnum.STOP;

    public string Name { get => name; set => name = value; }
    public string Description { get => description; set => description = value; }
    public string Type { get => type; set => type = value; }
    public int Dimensions { get => dimensions; set => dimensions = value; }
    public List<Agent> Agent_prototypes { get => agent_prototypes; set => agent_prototypes = value; }
    public List<Obstacle> Obstacle_prototypes { get => obstacle_prototypes; set => obstacle_prototypes = value; }
    public List<Generic> Generic_prototypes { get => generic_prototypes; set => generic_prototypes = value; }
    public Dictionary<string, string> Parameters { get => parameters; set => parameters = value; }
    public StateEnum State { get => state; set => state = value; }
    public int Id { get => id; set => id = value; }
    public List<string> EditableInPlay { get => editableInPlay; set => editableInPlay = value; }
    public List<string> EditableInPause { get => editableInPause; set => editableInPause = value; }

    public Simulation(int id, string name, string description, string type, int dimensions, List<Agent> agent_prototypes,
        List<Obstacle> obstacle_prototypes, List<Generic> generic_prototypes, Dictionary<string, object> parameters)
    {
        Id = id;
        Name = name;
        Description = description;
        Type = type;
        Dimensions = dimensions;
        Agent_prototypes = agent_prototypes;
        Obstacle_prototypes = obstacle_prototypes;
        Generic_prototypes = generic_prototypes;
        Parameters = parameters;
    }
    
    public void UpdateSimParameter(string param_name, System.Object value)
    {
        if (Parameters.ContainsKey(param_name))
        {
            Parameters.Remove(param_name);
        }
        Parameters.Add(param_name, value);
    }
}
