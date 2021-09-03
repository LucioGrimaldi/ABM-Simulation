using System.Threading;
using Fixed;
using uPLibrary.Networking.M2Mqtt.Messages;
using SimpleJSON;
using System.Collections.Generic;
using System;

public class CommunicationController
{
    /// Event Handlers ///
    public event EventHandler<ResponseMessageEventArgs> responseMessageEventHandler;
    public event EventHandler<StepMessageEventArgs> stepMessageEventHandler;



    /// Queues ///
    private ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private SortedList<long, byte[]> secondaryQueue = new SortedList<long, byte[]>();

    /// MQTT Clients ///
    private MQTTControlClient controlClient = new MQTTControlClient();
    private MQTTSimClient simClient = new MQTTSimClient();
    private bool sim_client_ready, control_client_ready;

    /// Threads ///
    Thread controlClientThread;
    Thread simClientThread;
    Thread StepQueueHandlerThread;
    Thread ResponseQueueHandlerThread;

    /// Utility
    private JSONObject sim_step = new JSONObject();

    /// Access Methods ///
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, byte[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }
    public bool SIM_CLIENT_READY { get => sim_client_ready; set => sim_client_ready = value; }
    public bool CONTROL_CLIENT_READY { get => control_client_ready; set => control_client_ready = value; }
    public JSONObject Sim_step { get => sim_step; set => sim_step = value; }
    
    /// Methods ///
    
    // Clients //
    
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


    // Queue Handlers //

    /// <summary>
    /// Start steps message handler
    /// </summary>
    public void StartStepQueueHandlerThread(Simulation.StateEnum sim_state, int TARGET_FPS)
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step Management Thread started..");
        StepQueueHandlerThread = new Thread(() => StepQueueHandler(sim_state, TARGET_FPS));
        StepQueueHandlerThread.Start();
    }

    /// <summary>
    /// Start messages handler
    /// </summary>
    public void StartResponseQueueHandlerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Message Handler Thread started..");
        ResponseQueueHandlerThread = new Thread(ResponseQueueHandler);
        ResponseQueueHandlerThread.Start();
    }

    /// <summary>
    /// Stop steps message handler
    /// </summary>
    public void StopStepQueueHandlerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step Management Thread stopped.");
        StepQueueHandlerThread.Abort();
    }

    /// <summary>
    /// Stop messages handler
    /// </summary>
    public void StopResponseQueueHandlerThread()
    {
        UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Message Handler Thread stopped.");
        ResponseQueueHandlerThread.Abort();
    }

    /// <summary>
    /// Handles messages from MASON
    /// </summary>
    public void ResponseQueueHandler()
    {
        JSONObject json_response, payload;
        MqttMsgPublishEventArgs msg;
        string sender, op;
        while(responseMessageQueue == null){}
        while (true)
        {
            if(responseMessageQueue.TryDequeue(out msg))
            {
                json_response = (JSONObject) JSON.Parse(System.Text.Encoding.Unicode.GetString(Utils.DecompressStepPayload(msg.Message)));
                sender = json_response["sender"];
                op = json_response["op"];
                payload = (JSONObject) json_response["payload"];

                ResponseMessageEventArgs e = new ResponseMessageEventArgs();
                e.Msg = msg;
                e.Sender = sender;
                e.Op = op;
                e.Payload = payload;

                UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Message received in " + msg.Topic + " topic.\n" + "Sender: " + sender + "\n" + "OP: " + op + "\n" + "Payload: " + payload.ToString(1));

                responseMessageEventHandler.Invoke(this, e);
            }
            else
            {
                //print error
            }
        }
    }

    /// <summary>
    /// Orders steps and updates last arrived one
    /// </summary>
    public void StepQueueHandler(Simulation.StateEnum sim_state, int TARGET_FPS)
    {
        JSONObject step = new JSONObject();
        MqttMsgPublishEventArgs message;

        while (true)
        {
            if (SecondaryQueue.Count > TARGET_FPS && !sim_state.Equals(Simulation.StateEnum.PAUSE))
            {
                if (SecondaryQueue.Values[0] != null)
                {
                    //
                    // Trigger StepMessageEventHandler
                    //
                    //sim_step = Utils.StepToJSON(SecondaryQueue.Values[0], (JSONObject)sim_prototypes_list[sim_id]);
                    //UnityEngine.Debug.Log("SIM_STEP: \n" + sim_step.ToString());
                    SecondaryQueue.RemoveAt(0);
                }
                else
                {
                    UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Cannot get Step!");
                }
            }
            if (SimMessageQueue.Count > 0)
            {
                if (SimMessageQueue.TryDequeue(out message))
                {
                    //
                    // Trigger latestsimsteparrived update passing GetStepId(message.Message)
                    // 
                    SecondaryQueue.Add(Utils.GetStepId(message.Message), message.Message);
                    UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Step " + Utils.GetStepId(message.Message) + " dequeued from SimMessageQueue \n | " + "Topic: " + message.Topic);

                }
                else { UnityEngine.Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Cannot Dequeue!"); }
            }
        }
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


/// <summary>
/// Event Args Definitions
/// </summary>
public class ResponseMessageEventArgs : EventArgs
{
    public MqttMsgPublishEventArgs Msg { get; set; }
    public string Sender { get; set; }
    public string Op { get; set; }
    public JSONObject Payload { get; set; }
}
public class StepMessageEventArgs : EventArgs
{

}