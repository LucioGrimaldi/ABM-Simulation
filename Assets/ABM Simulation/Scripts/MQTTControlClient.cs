using System;
using SimpleJSON;
using Fixed;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

public class MQTTControlClient
{
    [Header("MQTT broker configuration")]
    [Tooltip("IP addres or URL of host running the broker")]
    private string brokerAddress = "193.205.161.52"; //isislab = 193.205.161.52  pietro = 87.11.194.30
    [Tooltip("Port where the broker accepts connections")]
    private int brokerPort = 1883;
    [Tooltip("Use encrypted connection")]
    private bool isEncrypted = false;
    [Tooltip("Topic where Unity send control/settings messages")]
    private readonly string controlTopic = "all_to_mason";
    [Tooltip("Topic where Unity receives response messages")]
    private readonly string responseTopic = "Response";
    [Header("Connection parameters")]
    [Tooltip("Connection to the broker is delayed by the the given milliseconds")]
    public int connectionDelay = 500;
    [Tooltip("Connection timeout in milliseconds")]

    // Controllers References
    private CommunicationController CommController;

    /// MQTT-related variables ///
    /// Client
    private MqttClient client;

    /// Settings
    public int timeoutOnConnection = MqttSettings.MQTT_CONNECT_TIMEOUT;
    private bool mqttClientConnectionClosed = false;
    private bool mqttClientConnected = false;
    /// MQTT Queue
    public ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue = new ConcurrentQueue<MqttMsgPublishEventArgs>();
    
    /// Sim-related variables///

    /// <summary>
    /// Event fired when a connection is successfully estabilished
    /// </summary>
    public event Action ConnectionSucceeded;

    /// <summary>
    /// Event fired when failing to connect
    /// </summary>
    public event Action ConnectionFailed;

    /// <summary>
    /// Send a command to MASON
    /// </summary>
    public void SendMessage(JSONObject msg)
    {
        byte[] message = Encoding.ASCII.GetBytes(msg.ToString());
        Debug.Log("message " + message);
        client.Publish(controlTopic, message);
                
    }

    /// <summary>
    /// Connect to the broker and get Queue ref.
    /// </summary>
    public virtual void Connect(out ConcurrentQueue<MqttMsgPublishEventArgs> responseMessageQueue, out bool ready)
    {
        responseMessageQueue = this.responseMessageQueue;
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
        SubscribeTopics();
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
    /// Subscribe to "responseTopic".
    /// </summary>
    protected virtual void SubscribeTopics()
    {
        client.Subscribe(new string[] { responseTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        OnSubscribe();
    }

    /// <summary>
    /// Unsubscribe from "responseTopic".
    /// </summary>
    protected virtual void UnsubscribeTopics()
    {
        client.Unsubscribe(new string[] { responseTopic });
    }

    /// <summary>
    /// Disconnect before the application quits.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        UnsubscribeTopics();
        CloseConnection();
    }

    /// <summary>
    /// Routine for incoming messages
    /// </summary>
    private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs msg)
    {
        if (msg.Topic.Equals("Response"))
        {
            CommController.EnqueueControlMessage(msg);
        }
    }

    protected virtual void OnSubscribe()
    {
        Debug.Log("Subscribed to topic: " + responseTopic);
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
    /// <returns>The execution is done in a coroutine.</returns>
    private void DoConnect()
    {
        // create MQTTSimClient instance 
        if (client == null)
        {
            try
            {
                //Debug.Log("CONNECTING..");
                client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                //System.Security.Cryptography.X509Certificates.X509Certificate cert = new System.Security.Cryptography.X509Certificates.X509Certificate();
                //MQTTSimClient = new MqttClient(brokerAddress, brokerPort, isEncrypted, cert, null, MqttSslProtocols.TLSv1_0, MyRemoteCertificateValidationCallback);
                //Debug.Log("CONNECTED");


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
                UnsubscribeTopics();
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