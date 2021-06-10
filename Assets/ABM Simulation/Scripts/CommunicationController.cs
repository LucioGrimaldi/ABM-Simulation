using System.Threading;
using Fixed;
using uPLibrary.Networking.M2Mqtt.Messages;
using SimpleJSON;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System;
using System.Linq;

public class CommunicationController
{
    /// Queues ///
    private ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue;
    private ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue;
    private SortedList<long, byte[]> secondaryQueue = new SortedList<long, byte[]>();

    /// MQTT Clients ///
    private MQTTControlClient controlClient = new MQTTControlClient();
    private MQTTSimClient simClient = new MQTTSimClient();
    private bool sim_client_ready, control_client_ready;

    /// Threads ///
    Thread controlClientThread;
    Thread simClientThread;
    Thread StepsHandlerThread;
    Thread MessagesHandlerThread;

    /// Decompression/Deserialize Streams/Readers
    private MemoryStream decompress_inputStream;
    private BinaryReader decompress_binaryReader;
    private MemoryStream deserialize_inputStream;
    private BinaryReader deserialize_binaryReader;
    private GZipStream gZipStream;

    /// Access Methods ///
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    public SortedList<long, byte[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }
    public bool SIM_CLIENT_READY { get => sim_client_ready; set => sim_client_ready = value; }
    public bool CONTROL_CLIENT_READY { get => control_client_ready; set => control_client_ready = value; }

    /// METHODS ///

    /// <summary>
    /// Start Simulation MQTT Client
    /// </summary>
    public void StartSimulationClient(ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue, bool SIM_CLIENT_READY)
    {
        simClientThread = new Thread(() => simClient.Connect(out simMessageQueue, out SIM_CLIENT_READY));
        simClientThread.Start();
    }
    
    /// <summary>
    /// Start Control MQTT Client
    /// </summary>
    public void StartControlClient(ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue, bool CONTROL_CLIENT_READY)
    {
        // Start MQTT Clients
        controlClientThread = new Thread(() => controlClient.Connect(out responseMessageQueue, out CONTROL_CLIENT_READY));
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
    /// Sim Message Enqueuer
    /// </summary>
    public void EnqueueSimMessage(MqttMsgPublishEventArgs msg)
    {
        simMessageQueue.Enqueue(msg);
    }

    /// <summary>
    /// Control Message Enqueuer
    /// </summary>
    public void EnqueueControlMessage(MqttMsgPublishEventArgs msg)
    {
        responseMessageQueue.Enqueue(msg);
    }

    /// <summary>
    /// SubscribeTopics Wrapper
    /// </summary>
    public void SubscribeTopics(int[] topics)
    {
        simClient.SubscribeTopics(topics);
    }

    /// <summary>
    /// UnsubscribeTopics Wrapper
    /// </summary>
    public void UnsubscribeTopics(int[] topics)
    {
        simClient.UnsubscribeTopics(topics);
    }

    /// <summary>
    /// Start steps message handler
    /// </summary>
    public void StartStepsHandlerThread(object simulation, object TARGET_FPS)
    {
        Debug.WriteLine("Step Management Thread started..");
        object parameters = new object[] { simulation, TARGET_FPS };
        StepsHandlerThread = new Thread(new ParameterizedThreadStart(SimStepHandler));
        StepsHandlerThread.Start(parameters);
    }

    /// <summary>
    /// Start messages handler
    /// </summary>
    public void StartMessageHandlerThread()
    {
        Debug.WriteLine("Message Handler Thread started..");
        MessagesHandlerThread = new Thread(MessagesHandler);
        MessagesHandlerThread.Start();
    }

    /// <summary>
    /// Stop steps message handler
    /// </summary>
    public void StopStepsHandlerThread()
    {
        Debug.WriteLine("Step Management Thread stopped.");
        StepsHandlerThread.Abort();
    }

    /// <summary>
    /// Stop messages handler
    /// </summary>
    public void StopMessageHandlerThread()
    {
        Debug.WriteLine("Message Handler Thread stopped.");
        MessagesHandlerThread.Abort();
    }

    /// <summary>
    /// Handles messages from MASON
    /// </summary>
    public void MessagesHandler()
    {
        JSONObject json_response;
        JSONArray sim_list;
        MqttMsgPublishEventArgs msg;
        string message_code;
        while (true)
        {
            if(responseMessageQueue.TryDequeue(out msg))
            {
                json_response = (JSONObject)JSON.Parse(msg.Message.ToString());
                message_code = json_response["op"];
                switch (message_code)
                {
                    case "000":

                                                            //check status

                        break;
                    case "007":
                        
                        // SIM_LIST

                        // CONNECTION true
                        // segnalare che la connessione è avvenuta
                        

                        //generic response




                        break;
                    case "998"://error
                               //disconnect
                        break;
                    default:
                        break;
                }
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
    public void SimStepHandler(object parameters)
    {
        MqttMsgPublishEventArgs message;
        Simulation simulation = (Simulation) ((object[]) parameters)[0];
        int TARGET_FPS = (int) ((object[]) parameters)[1];

        while (true)
        {
            if (SecondaryQueue.Count > TARGET_FPS && !simulation.State.Equals(Simulation.StateEnum.PAUSE))
            {
                if (SecondaryQueue.Values[0] != null)
                {
                    // trigger simulation update passing DecompressStepPayload(SecondaryQueue.Values[0])
                    SecondaryQueue.RemoveAt(0);
                }
                else
                {
                    Debug.WriteLine("Cannot get Step!");
                }
            }
            if (SimMessageQueue.Count > 0)
            {
                if (SimMessageQueue.TryDequeue(out message))
                {
                    // trigger latestsimsteparrived update passing GetStepId(message.Message)
                    SecondaryQueue.Add(GetStepId(message.Message), message.Message);
                }
                else { Debug.WriteLine("Cannot Dequeue!"); }
            }
        }
    }

    /// <summary>
    /// Decompresses Gzipped message payload and get payload byte[]
    /// </summary>
    private byte[] DecompressStepPayload(byte[] payload)
    {
        // Prepare Binary Stream of Gzipped payload
        decompress_inputStream = new MemoryStream(payload);
        gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress);

        List<byte> uncompressedPayload = new List<byte>();

        int bytesRead = gZipStream.ReadByte();
        while (bytesRead != -1)
        {
            uncompressedPayload.Add((byte)bytesRead);
            bytesRead = gZipStream.ReadByte();
        }
        gZipStream.Flush();
        decompress_inputStream.Flush();
        decompress_inputStream.Close();
        gZipStream.Close();

        return uncompressedPayload.ToArray();
    }

    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] bytes = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
        Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
        return bytes;
    }

    /// <summary>
    /// Send message to MASON
    /// </summary>
    public void SendMessage(string sender, string op, JSONObject payload)
    {
        JSONObject msg = new JSONObject();
        msg.Add("sender", sender);
        msg.Add("op", op);
        msg.Add("payload", payload);
        controlClient.SendMessage(msg);
    }

    /// <summary>
    /// Gets Step id from Step Message payload
    /// </summary>
    public long GetStepId(byte[] payload){

        // Prepare Binary Stream of Gzipped payload
        decompress_inputStream = new MemoryStream(payload);
        gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress);
        deserialize_binaryReader = new BinaryReader(gZipStream);

        gZipStream.Flush();
        decompress_inputStream.Flush();
        decompress_inputStream.Close();
        gZipStream.Close();

        return deserialize_binaryReader.ReadInt64();
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