using System.Threading;
using Fixed;
using uPLibrary.Networking.M2Mqtt.Messages;
using SimpleJSON;
using System.Collections.Generic;
using System;

public class CommunicationController
{
    /// Communication Events ///
    public static event EventHandler<EventArgs> OnControlClientConnectedHandler;
    public static event EventHandler<EventArgs> OnSimClientConnectedHandler;
    public static event EventHandler<EventArgs> OnControlClientDisconnectedHandler;
    public static event EventHandler<EventArgs> OnSimClientDisconnectedHandler;

    /// QUEUES ///
    public ConcurrentQueue<MqttMsgPublishEventArgs> messageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    public ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private SortedList<long, byte[]> secondaryQueue = new SortedList<long, byte[]>();

    /// MQTT CLIENTS ///
    private MQTTControlClient controlClient = new MQTTControlClient();
    private MQTTSimClient simClient = new MQTTSimClient();
    private static Boolean sim_client_ready = false, control_client_ready = false;

    /// THREADS ///
    private Thread controlClientThread;
    private Thread simClientThread;

    /// ACCESS METHODS ///
    public ConcurrentQueue<MqttMsgPublishEventArgs> MessageQueue { get => messageQueue; set => messageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, byte[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }
    public static Boolean SIM_CLIENT_READY { get => sim_client_ready; set => sim_client_ready = value; }
    public static Boolean CONTROL_CLIENT_READY { get => control_client_ready; set => control_client_ready = value; }
    
    /// METHODS ///
    
    /// MQTT Clients
    
    /// <summary>
    /// Start Simulation MQTT Client
    /// </summary>
    public void StartSimulationClient()
    {
        simClient.ConnectionSucceeded += OnSimClientConnected;
        //simClient.ConnectionFailed +=
        simClientThread = new Thread(() => simClient.Connect(ref simMessageQueue));
        simClientThread.Start();
    }
    
    /// <summary>
    /// Start Control MQTT Client
    /// </summary>
    public void StartControlClient()
    {
        controlClient.ConnectionSucceeded += OnControlClientConnected;
        //controlClient.ConnectionFailed +=
        controlClientThread = new Thread(() => controlClient.Connect(ref messageQueue));
        controlClientThread.Start();
    }

    /// <summary>
    /// Disconnect and aborts Simulation MQTT Client
    /// </summary>
    public void DisconnectSimulationClient()
    {
        simClient.DisconnectionSucceeded += OnSimClientDisconnected;
        simClient.Disconnect();
        simClientThread.Abort();
    }

    /// <summary>
    /// Disconnect and aborts Control MQTT Client
    /// </summary>
    public void DisconnectControlClient()
    {
        controlClient.DisconnectionSucceeded += OnControlClientDisconnected;
        controlClient.Disconnect();
        controlClientThread.Abort();
    }

    /// <summary>
    /// SubscribeTopics Wrapper
    /// </summary>
    public void SubscribeTopics(int[] topics)
    {
        simClient.SubscribeTopics(topics);
    }

    /// <summary>
    /// SubscribeTopic Wrapper
    /// </summary>
    public void SubscribeTopic(string nickname)
    {
        controlClient.SubscribeTopic(nickname);
    }

    /// <summary>
    /// UnsubscribeTopic Wrapper
    /// </summary>
    public void UnsubscribeTopic(string nickname)
    {
        controlClient.UnsubscribeTopic(nickname);
    }

    /// <summary>
    /// UnsubscribeTopics Wrapper
    /// </summary>
    public void UnsubscribeTopics(int[] topics)
    {
        simClient.UnsubscribeTopics(topics);
    }

    /// OnEvent Methods ///
    private void OnSimClientConnected()
    {
        SIM_CLIENT_READY = true;
        OnSimClientConnectedHandler?.BeginInvoke(null, EventArgs.Empty, null, null);
    }
    private void OnControlClientConnected()
    {
        CONTROL_CLIENT_READY = true;
        OnControlClientConnectedHandler?.BeginInvoke(null, EventArgs.Empty, null, null);
    }
    private void OnSimClientDisconnected()
    {
        SIM_CLIENT_READY = false;
        OnSimClientDisconnectedHandler?.BeginInvoke(null, EventArgs.Empty, null, null);
    }
    private void OnControlClientDisconnected()
    {
        CONTROL_CLIENT_READY = false;
        OnControlClientDisconnectedHandler?.BeginInvoke(null, EventArgs.Empty, null, null);
    }

    /// Utilities ///

    /// <summary>
    /// Send message to MASON
    /// </summary>
    public void SendMessage(string sender, string op, JSONObject payload)
    {
        JSONObject msg = new JSONObject();
        msg.Add("sender", sender);
        msg.Add("op", op);
        msg.Add("payload", payload);
        //UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Trying sending message...");
        controlClient.SendMessage(msg);
    }

    /// <summary>
    /// Empty queues
    /// </summary>
    public void EmptyQueues()
    {
        while (simMessageQueue.TryDequeue(out _)) ;
        SecondaryQueue.Clear();
    }

    public void Quit()
    {
        simClient.ConnectionSucceeded -= OnSimClientConnected;
        controlClient.ConnectionSucceeded -= OnControlClientConnected;
        simClient.DisconnectionSucceeded -= OnSimClientDisconnected;
        controlClient.DisconnectionSucceeded -= OnControlClientDisconnected;
    }
}
