public class Simulation
{
    //Simulation Variables
    //Simulation space dimentions
    private float width, height, lenght;
    private int numAgents;

    //State
    private int simStepRate;
    private float simStepDelay;
    private long lastStep;
    private bool running, paused, stopped;


    public Simulation(float width, float height, float lenght, int numAgents, int simStepRate, float simStepDelay)
    {
        this.Width = width;
        this.Height = height;
        this.Lenght = lenght;
        this.SimStepRate = simStepRate;
        this.SimStepDelay = SimStepDelay;
        this.numAgents = numAgents;
    }

    public float Width { get => width; set => width = value; }
    public float Height { get => height; set => height = value; }
    public float Lenght { get => lenght; set => lenght = value; }
    public int NumAgents { get => numAgents; set => numAgents = value; }
    public int SimStepRate { get => simStepRate; set => simStepRate = value; }
    public float SimStepDelay { get => simStepDelay; set => simStepDelay = value; }
    public long LastStep { get => lastStep; set => lastStep = value; }
    public bool Running { get => running; set => running = value; }
    public bool Paused { get => paused; set => paused = value; }
    public bool Stopped { get => stopped; set => stopped = value; }
}
