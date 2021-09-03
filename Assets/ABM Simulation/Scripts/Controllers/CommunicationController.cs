using System.Threading;
using Fixed;
using uPLibrary.Networking.M2Mqtt.Messages;
using SimpleJSON;
using System.Collections.Generic;
using System;

public class CommunicationController
{
    /// QUEUES ///
    public ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    public ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private SortedList<long, byte[]> secondaryQueue = new SortedList<long, byte[]>();

    /// MQTT CLIENTS ///
    private MQTTControlClient controlClient = new MQTTControlClient();
    private MQTTSimClient simClient = new MQTTSimClient();
    private bool sim_client_ready, control_client_ready;

    /// THREADS ///
    private Thread controlClientThread;
    private Thread simClientThread;

    /// ACCESS METHODS ///
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, byte[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }
    public bool SIM_CLIENT_READY { get => sim_client_ready; set => sim_client_ready = value; }
    public bool CONTROL_CLIENT_READY { get => control_client_ready; set => control_client_ready = value; }
    
    /// METHODS ///
    
    /// MQTT Clients
    
    /// <summary>
    /// Start Simulation MQTT Client
    /// </summary>
    public void StartSimulationClient()
    {
        simClientThread = new Thread(() => simClient.Connect(ref simMessageQueue, out sim_client_ready));
        simClientThread.Start();
    }
    
    /// <summary>
    /// Start Control MQTT Client
    /// </summary>
    public void StartControlClient()
    {
        // Start MQTT Clients
        controlClientThread = new Thread(() => controlClient.Connect(ref responseMessageQueue, out control_client_ready));
        controlClientThread.Start();
    }

    /// <summary>
    /// Disconnect and aborts Simulation MQTT Client
    /// </summary>
    public void DisconnectSimulationClient()
    {
        simClient.Disconnect();
        simClientThread.Abort();
    }

    /// <summary>
    /// Disconnect and aborts Control MQTT Client
    /// </summary>
    public void DisconnectControlClient()
    {
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
    /// UnsubscribeTopics Wrapper
    /// </summary>
    public void UnsubscribeTopics(int[] topics)
    {
        simClient.UnsubscribeTopics(topics);
    }


    // Utilities //
    
    /// <summary>
    /// Send message to MASON
    /// </summary>
    public void SendMessage(string sender, string op, JSONObject payload)
    {
        JSONObject msg = new JSONObject();
        msg.Add("sender", sender);
        msg.Add("op", op);
        msg.Add("payload", payload);
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Trying sending message...");
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

}
