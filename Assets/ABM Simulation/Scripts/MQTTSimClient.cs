#define TRACE

using System;
using System.Collections;
using Fixed;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


public class MQTTSimClient
{
    [Header("MQTT broker configuration")]
    [Tooltip("IP addres or URL of host running the broker")]
    private string brokerAddress = "87.11.194.30"; //isislab = 193.205.161.52  pietro = 87.11.194.30
    [Tooltip("Port where the broker accepts connections")]
    private int brokerPort = 1883;
    [Tooltip("Use encrypted connection")]
    private bool isEncrypted = false;
    [Tooltip("Topic where Unity receive messages")]
    private int[] topicArray;
    //represent all topics available for simulation as strings
    private string[] stringTopicArray;
    [Header("Connection parameters")]
    [Tooltip("Connection to the broker is delayed by the the given milliseconds")]
    public int connectionDelay = 500;
    [Tooltip("Connection timeout in milliseconds")]

    /// MQTT-related variables ///
    /// Client
    private MqttClient client;
    /// Settings
    public int timeoutOnConnection = MqttSettings.MQTT_CONNECT_TIMEOUT;    
    private bool mqttClientConnectionClosed = false;
    private bool mqttClientConnected = false;
    /// MQTT Queues
    private ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();

    /// <summary>
    /// Event fired when a connection is successfully estabilished
    /// </summary>
    public event Action ConnectionSucceeded;
    /// <summary>
    /// Event fired when failing to connect
    /// </summary>
    public event Action ConnectionFailed;

    /// <summary>
    /// Connect to the broker.
    /// </summary>
    public virtual void Connect(out ConcurrentQueue<MqttMsgPublishEventArgs> simMessageQueue, out bool ready)
    {
        simMessageQueue = this.simMessageQueue;
        if (client == null || !client.IsConnected)
        {
            DoConnect();
        }
        ready = client.IsConnected;
    }

    /// <summary>
    /// Disconnect from the broker, if connected.
    /// </summary>
    public virtual void Disconnect()
    {
        if (client != null)
        {
            DoDisconnect();
        }
    }

    /// <summary>
    /// Ovverride this method to take some actions before connection (e.g. display a message)
    /// </summary>
    protected virtual void OnConnecting()
    {
        Debug.LogFormat("Connecting to broker on {0}:{1}...\n", brokerAddress, brokerPort.ToString());
    }

    /// <summary>
    /// Override this method to take some actions if the connection succeeded.
    /// </summary>
    protected virtual void OnConnected()
    {
        Debug.LogFormat("Connected to {0}:{1}...\n", brokerAddress, brokerPort.ToString());
        SubscribeAll();
        ConnectionSucceeded?.Invoke();
        Debug.Log("Waiting for MASON...");
    }

    /// <summary>
    /// Override this method to take some actions if the connection failed.
    /// </summary>
    protected virtual void OnConnectionFailed(string errorMessage)
    {
        Debug.LogWarning("Connection failed.");
        ConnectionFailed?.Invoke();
    }

    /// <summary>
    /// Ovverride this method to subscribe to MQTT topics.
    /// </summary>
    protected virtual void SubscribeAll()
    {
        topicArray = new int[60];
        stringTopicArray = new string[60];
        byte[] QosArray = new byte[60];
        for (int i = 0; i < 60; i++)
        {
            topicArray[i] = i;
            stringTopicArray[i] = "Topic" + i;
            QosArray[i] = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE;
        }
        client.Subscribe(stringTopicArray, QosArray);
        OnSubscribe(stringTopicArray);
    }

    public virtual void SubscribeTopics(int[] topics)
    {
        string[] topicsToSubscribe = new string[topics.Length];
        byte[] QosArray = new byte[topics.Length];
        for (int i = 0; i < topics.Length; i++)
        {
            topicsToSubscribe[i] = "Topic" + topics[i];
            QosArray[i] = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE;
        }
        client.Subscribe(topicsToSubscribe, QosArray);
        OnSubscribe(topicsToSubscribe);
    }

    /// <summary>
    /// Ovverride this method to unsubscribe to MQTT topics (they should be the same you subscribed to with SubscribeTopics() ).
    /// </summary>
    protected virtual void UnsubscribeAll()
    {
        client.Unsubscribe(stringTopicArray);
    }

    public virtual void UnsubscribeTopics(int[] topics)
    {
        string[] topicsToUnsubscribe = new string[topics.Length];
        for (int i = 0; i < topics.Length; i++)
        {
            topicsToUnsubscribe[i] = "Topic" + topics[i];
        }
        client.Unsubscribe(topicsToUnsubscribe);
    }

    /// <summary>
    /// Disconnect before the application quits.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        UnsubscribeAll();
        CloseConnection();
    }
    
    /// <summary>
    /// Routine for incoming messages
    /// </summary>
    private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs msg)
    {
        simMessageQueue.Enqueue(msg);
    }

    protected virtual void OnSubscribe(string[] topics)
    {
        Debug.Log("Subscribed to topic: " + topics);
    }

    /// <summary>
    /// Override this method to take some actions when disconnected.
    /// </summary>
    protected virtual void OnDisconnected()
    {
        Debug.Log("Disconnected.");
    }

    /// <summary>
    /// Override this method to take some actions when the connection is closed.
    /// </summary>
    protected virtual void OnConnectionLost()
    {
        Debug.LogWarning("CONNECTION LOST!");
    }

    private void OnMqttConnectionClosed(object sender, EventArgs e)
    {
        // Set unexpected connection closed only if connected (avoid event handling in case of controlled disconnection)
        mqttClientConnectionClosed = mqttClientConnected;
        mqttClientConnected = false;
    }

    /// <summary>
    /// Connects to the broker using the current settings.
    /// </summary>
    private void DoConnect()
    {
        // create MQTTSimClient instance 
        if (client == null)
        {
            try
            {
                Debug.Log("CONNECTING..");
                client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                //System.Security.Cryptography.X509Certificates.X509Certificate cert = new System.Security.Cryptography.X509Certificates.X509Certificate();
                //MQTTSimClient = new MqttClient(brokerAddress, brokerPort, isEncrypted, cert, null, MqttSslProtocols.TLSv1_0, MyRemoteCertificateValidationCallback);
                Debug.Log("CONNECTED");


            }
            catch (Exception e)
            {
                client = null;
                Debug.LogErrorFormat("CONNECTION FAILED! {0}", e.ToString());
                OnConnectionFailed(e.Message);
                return;
            }
        }
        else if (client.IsConnected)
        {
            return;
        }
        OnConnecting();

        client.Settings.TimeoutOnConnection = timeoutOnConnection;
        string clientId = Guid.NewGuid().ToString();
        try
        {
            client.Connect(clientId);
        }
        catch (Exception e)
        {
            client = null;
            Debug.LogErrorFormat("Failed to connect to {0}:{1}:\n{2}", brokerAddress, brokerPort, e.ToString());
            OnConnectionFailed(e.Message);
            return;
        }
        if (client.IsConnected)
        {
            client.ConnectionClosed += OnMqttConnectionClosed;
            // register to message received 
            client.MqttMsgPublishReceived += OnMqttMessageReceived;
            mqttClientConnected = true;
            OnConnected();
        }
        else
        {
            OnConnectionFailed("CONNECTION FAILED!");
        }
    }

    private void DoDisconnect()
    {
        CloseConnection();
        OnDisconnected();
    }

    private void CloseConnection()
    {
        mqttClientConnected = false;
        if (client != null)
        {
            if (client.IsConnected)
            {
                UnsubscribeAll();
                client.Disconnect();
            }
            client.ConnectionClosed -= OnMqttConnectionClosed;
            client = null;
        }
    }

#if ((!UNITY_EDITOR && UNITY_WSA_10_0))
        private void OnApplicationFocus(bool focus)
        {
            // On UWP 10 (HoloLens) we cannot tell whether the application actually got closed or just minimized.
            // (https://forum.unity.com/threads/onapplicationquit-and-ondestroy-are-not-called-on-uwp-10.462597/)
            if (focus)
            {
                Connect();
            }
            else
            {
                CloseConnection();
            }
        }
#endif
}