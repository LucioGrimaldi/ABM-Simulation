using SimpleJSON;
using System.Diagnostics;
using System.Threading;

public class ConnectionManager
{
    /// Controller
    private CommunicationController comm_controller;

    /// Connection variables
    private long elapsed_time;
    private int CONN_TIMEOUT = 5000;    ///millis
    private bool mason_ready;
    private string nickname;
    private Stopwatch stopwatch;

    /// Access Methods
    public bool MASON_READY { get => mason_ready; set => mason_ready = value; }

    public ConnectionManager(CommunicationController comm_controller, string nickname)
    {
        this.comm_controller = comm_controller;
        this.nickname = nickname;
    }


    private void StartConnection()
    {
        comm_controller.SendMessage(nickname, "001", new JSONObject());
        stopwatch = new Stopwatch();
        stopwatch.Start();
    }

    private void CheckConnection()
    {



    }





}
