using System.Threading;
using Fixed;
using uPLibrary.Networking.M2Mqtt.Messages;
using SimpleJSON;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.IO;
using System.IO.Compression;

public class CommunicationController
{
    private int TARGET_FPS = 60;
    private MemoryStream deserialize_inputStream;
    private MemoryStream decompress_inputStream;
    private BinaryReader deserialize_binaryReader;
    private BinaryReader decompress_binaryReader;
    private GZipStream gZipStream;

    private long latestSimStepArrived = 0;
    private long currentSimStep = -1;
    public long LatestSimStepArrived { get => latestSimStepArrived; set => latestSimStepArrived = value; }
    public long CurrentSimStep { get => currentSimStep; set => currentSimStep = value; }
    /// Queues
    private ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    private ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    public ConcurrentQueue<MqttMsgPublishEventArgs> ResponseMessageQueue { get => responseMessageQueue; set => responseMessageQueue = value; }
    public ConcurrentQueue<MqttMsgPublishEventArgs> SimMessageQueue { get => simMessageQueue; set => simMessageQueue = value; }
    private SortedList<long, byte[]> secondaryQueue = new SortedList<long, byte[]>();
    public SortedList<long, byte[]> SecondaryQueue { get => secondaryQueue; set => secondaryQueue = value; }

    /// MQTT Clients ///
    private MQTTControlClient controlClient = new MQTTControlClient();
    private MQTTSimClient simClient = new MQTTSimClient();
    /// Threads
    Thread controlClientThread, simClientThread, buildStepThread;

    public void StartSimulationClient(ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue, bool SIM_CLIENT_READY)
    {
        simClientThread = new Thread(() => simClient.Connect(out simMessageQueue, out SIM_CLIENT_READY));
        simClientThread.Start();
    }
    public void StartControlClient(ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue, bool CONTROL_CLIENT_READY)
    {
        // Start MQTT Clients
        controlClientThread = new Thread(() => controlClient.Connect(out responseMessageQueue, out CONTROL_CLIENT_READY));
        controlClientThread.Start();
    }

    public void DisconnectSimulationClient()
    {
        simClient.Disconnect();
        simClientThread.Abort();
    }

    public void DisconnectControlClient()
    {
        controlClient.Disconnect();
        controlClientThread.Abort();
    }

    public void InitializeSimulation()
    {

    }

    public void SendCommand()
    {

    }

    public void EnqueueSimMessage()
    {

    }

    public void EnqueueControlMessage(MqttMsgPublishEventArgs msg)
    {
        responseMessageQueue.Enqueue(msg);
    }

    public void SubscribeTopics(int[] topics)
    {
        simClient.SubscribeTopics(topics);
    }

    public void UnsubscribeTopics(int[] topics)
    {
        simClient.UnsubscribeTopics(topics);
    }

    public void StartUpdateSimStepThread()
    {
        Debug.WriteLine("Building Steps..");
        buildStepThread = new Thread(SortAndUpdateSimStep);
        buildStepThread.Start();
    }

    /// <summary>
    /// Batch Step Construction Coroutine
    /// </summary>
    public void SortAndUpdateSimStep()
    {
        MqttMsgPublishEventArgs step;

        while (true)
        {
            if (SecondaryQueue.Count > TARGET_FPS && !Simulation.State.Equals(Simulation.StateEnum.PAUSE))
            {
                if (SecondaryQueue.Values[0] != null)
                {
                    DeserializeStep(SecondaryQueue.Values[0]);
                    SecondaryQueue.RemoveAt(0);
                }
                else
                {
                    Debug.WriteLine("Cannot get Step!");
                }
            }
            if (SimMessageQueue.Count > 0)
            {
                if (SimMessageQueue.TryDequeue(out step))
                {
                    LatestSimStepArrived = GetStepId(DecompressData(step.Message));
                    SecondaryQueue.Add(LatestSimStepArrived, step.Message);
                }
                else { Debug.WriteLine("Cannot Dequeue!"); }
            }
        }
    }

    /// <summary>
    /// Convert received string data into Vector3[]
    /// </summary>
    private void DeserializeStep(byte[] data)
    {
        deserialize_inputStream = new MemoryStream(data);
        deserialize_binaryReader = new BinaryReader(deserialize_inputStream);
        batch_flockSimStep = deserialize_binaryReader.ReadInt64();

        for (int i = 0; i < Simulation.Parameters.TryGetValue("nome agente"); i++)
        {
            batch_flockId = deserialize_binaryReader.ReadInt32();
            position = new Vector3(deserialize_binaryReader.ReadSingle() / flockSim.Width - 0.5f, deserialize_binaryReader.ReadSingle() / flockSim.Height - 0.5f, deserialize_binaryReader.ReadSingle() / flockSim.Lenght - 0.5f);
            positions[batch_flockId] = position;
        }

        deserialize_inputStream.Close();
        deserialize_binaryReader.Close();
        //Aggiornare la simulazione con lo step deserializzato
    }
    private byte[] DecompressData(byte[] payload)
    {
        decompress_inputStream = new MemoryStream(payload);
        gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress);
        decompress_binaryReader = new BinaryReader(gZipStream);

        byte[] data = decompress_binaryReader.ReadBytes(8 //Bytes for sim step num (long)
                            + (12 * flockSim.NumAgents)   //Bytes for flock positions (3 floats)
                            + (4 * flockSim.NumAgents));  //Bytes for flock id (int)

        decompress_inputStream.Close();
        gZipStream.Close();
        decompress_binaryReader.Close();

        return data;
    }
    public long GetStepId(byte[] data){
        deserialize_inputStream = new MemoryStream(data);
        deserialize_binaryReader = new BinaryReader(deserialize_inputStream);
        return deserialize_binaryReader.ReadInt64();
    }
}