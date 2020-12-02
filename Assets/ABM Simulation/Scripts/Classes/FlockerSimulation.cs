

public class FlockerSimulation : Simulation
{
    private float cohesion;
    private float avoidance;
    private float avoidDistance;
    private float randomness;
    private float consistency;
    private float momentum;
    private float neighborhood;
    private float jump;
    private float deadAgentProbability;

    public FlockerSimulation(float width, float height, float lenght, int numAgents, int simStepRate, float simStepDelay,
        float cohesion, float avoidance, float avoidDistance, float randomness, float consistency, float momentum, float neighborhood, float jump, float deadAgentProbability) 
        : base(width, height, lenght, numAgents, simStepRate, simStepDelay)
    {
        this.Cohesion = cohesion;
        this.Avoidance = avoidance;
        this.AvoidDistance = avoidDistance;
        this.Randomness = randomness;
        this.Consistency = consistency;
        this.Momentum = momentum;
        this.Neighborhood = neighborhood;
        this.Jump = jump;
        this.DeadAgentProbability = deadAgentProbability;
    }

    public float Cohesion { get => cohesion; set => cohesion = value; }
    public float Avoidance { get => avoidance; set => avoidance = value; }
    public float AvoidDistance { get => avoidDistance; set => avoidDistance = value; }
    public float Randomness { get => randomness; set => randomness = value; }
    public float Consistency { get => consistency; set => consistency = value; }
    public float Momentum { get => momentum; set => momentum = value; }
    public float Neighborhood { get => neighborhood; set => neighborhood = value; }
    public float Jump { get => jump; set => jump = value; }
    public float DeadAgentProbability { get => deadAgentProbability; set => deadAgentProbability = value; }
}
