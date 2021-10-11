using System;
using System.Linq;
using System.Text;
using Fixed;
using SimpleJSON;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class Test : MonoBehaviour
{
    [Header("MQTT broker configuration")]
    [Tooltip("IP addres or URL of host running the broker")]
    private string brokerAddress = "193.205.161.52"; //isislab = 193.205.161.52  pietro = 87.11.194.30
    [Tooltip("Port where the broker accepts connections")]
    private int brokerPort = 1883;
    [Tooltip("Use encrypted connection")]
    private bool isEncrypted = false;
    [Tooltip("Topic where Unity receive messages")]
    private int[] topicArray;
    [Tooltip("Topic where Unity send control/settings messages")]
    private string controlTopic = "all_to_mason";
    //represent all topics available for simulation as strings
    private string[] stringTopicArray;
    [Header("Connection parameters")]
    [Tooltip("Connection to the broker is delayed by the the given milliseconds")]
    public int connectionDelay = 500;
    [Tooltip("Connection timeout in milliseconds")]

    private MqttClient client;

    /// Settings
    public int timeoutOnConnection = MqttSettings.MQTT_CONNECT_TIMEOUT;
    private bool mqttClientConnectionClosed = false;
    private bool mqttClientConnected = false;

    private long start_time;

    /// MQTT Queue
    private int message_count = 0;
    private JSONArray sim_prototypes_list = (JSONArray)JSON.Parse("[ { \"id\": 0, \"name\": \"Flockers\", \"description\": \"....\", \"type\": \"CONTINUOUS\", \"dimensions\": [ { \"name\": \"x\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"y\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"z\", \"type\": \"System.Int32\", \"default\": 500 } ], \"sim_params\": [ { \"name\": \"cohesion\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"avoidance\", \"type\": \"System.Single\", \"default\": \"0.5\" }, { \"name\": \"randomness\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"consistency\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"momentum\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"neighborhood\", \"type\": \"System.Int32\", \"default\": 10 }, { \"name\": \"jump\", \"type\": \"System.Single\", \"default\": 0.7 } ], \"agent_prototypes\": [ { \"class\": \"Flocker\", \"default\": 10, \"is_in_step\" : true, \"params\": [ { \"name\": \"position\", \"type\": \"System.Position\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": { \"x\": 1, \"y\": 1, \"z\": 1 } } ] } ], \"generic_prototypes\": [] }, { \"id\": 1, \"name\": \"AntsForage\", \"description\": \"....\", \"type\": \"DISCRETE\", \"dimensions\": [ { \"name\": \"x\", \"type\": \"System.Int32\", \"default\": 100 }, { \"name\": \"y\", \"type\": \"System.Int32\", \"default\": 100 } ], \"sim_params\": [ { \"name\": \"evaporationConstant\", \"type\": \"System.Single\", \"default\": 0.999 }, { \"name\": \"reward\", \"type\": \"System.Single\", \"default\": 1.0 }, { \"name\": \"updateCutDown\", \"type\": \"System.Single\", \"default\": 0.9 }, { \"name\": \"momentumProbability\", \"type\": \"System.Single\", \"default\": 0.8 }, { \"name\": \"randomActionProbability\", \"type\": \"System.Single\", \"default\": 0.1 } ], \"agent_prototypes\": [ { \"class\": \"Ant\", \"default\": 100, \"is_in_step\" : true, \"params\": [ { \"name\": \"reward\", \"type\": \"System.Single\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": 0.9 }, { \"name\": \"hasFoodItem\", \"type\": \"System.Boolean\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": false }, { \"name\": \"position\", \"type\": \"System.Position\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": {} } ] } ], \"generic_prototypes\": [ { \"class\": \"Home\", \"default\": 1, \"is_in_step\" : false, \"params\": [ { \"name\": \"position\", \"type\": \"System.Cell\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": [] } ] }, { \"class\": \"Food\", \"default\": 1, \"is_in_step\" : false, \"params\": [ { \"name\": \"position\", \"type\": \"System.Cell\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": [] } ] }, { \"class\": \"PheromoneToHome\", \"default\": 0, \"is_in_step\" : true, \"params\": [ { \"name\": \"position\", \"type\": \"System.Cell\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": [] }, { \"name\": \"intensity\", \"type\": \"System.Single\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": true, \"editable_in_pause\": true, \"default\": 0 } ] }, { \"class\": \"PheromoneToFood\", \"default\": 0, \"is_in_step\" : true, \"params\": [ { \"name\": \"position\", \"type\": \"System.Cell\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": false, \"editable_in_pause\": true, \"default\": [] }, { \"name\": \"intensity\", \"type\": \"System.Single\", \"is_in_step\" : true, \"editable_in_init\": false, \"editable_in_play\": true, \"editable_in_pause\": true, \"default\": 0 } ] } ] } ]");
    string nick = "io";
    int[] topics = new int[30] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };


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
        client.Publish(controlTopic, message);
        Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Message published to " + controlTopic + " topic, message: " + msg.ToString());
    }

    /// <summary>
    /// Connect to the broker and get Queue ref.
    /// </summary>
    public virtual void Connect()
    {
        if (client == null || !client.IsConnected)
        {
            DoConnect();
        }
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
        Debug.LogFormat(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Connecting to broker on {0}:{1}...\n", brokerAddress, brokerPort.ToString());
    }

    /// <summary>
    /// Override this method to take some actions if the connection succeeded.
    /// </summary>
    protected virtual void OnConnected()
    {
        Debug.LogFormat(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Connected to {0}:{1}...\n", brokerAddress, brokerPort.ToString());
        SubscribeAll();
        ConnectionSucceeded?.Invoke();
        Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Waiting for MASON...");
    }

    /// <summary>
    /// Override this method to take some actions if the connection failed.
    /// </summary>
    protected virtual void OnConnectionFailed(string errorMessage)
    {
        Debug.LogWarning(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Connection failed.");
        ConnectionFailed?.Invoke();
    }

    /// <summary>
    /// Subscribe to all MQTT topics.
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
            QosArray[i] = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
        }
        client.Subscribe(stringTopicArray, QosArray);
        OnSubscribe(stringTopicArray);
    }

    /// <summary>
    /// Subscribe to topics
    /// </summary>
    protected virtual void SubscribeTopics(int[] topics)
    {
        string[] topicsToSubscribe = new string[topics.Length];
        byte[] QosArray = new byte[topics.Length];
        for (int i = 0; i < topics.Length; i++)
        {
            topicsToSubscribe[i] = "Topic" + topics[i];
            QosArray[i] = MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE;
        }
        client.Subscribe(topicsToSubscribe, QosArray);
        OnSubscribe(topicsToSubscribe);
    }

    /// <summary>
    /// Unsubscribe to all MQTT topics. 
    /// </summary>
    protected virtual void UnsubscribeAll()
    {
        client.Unsubscribe(stringTopicArray);
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
        if (message_count == 0)
        {
            start_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
        ++message_count;

        long received_time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - start_time;

        if (received_time > 0)
        {
            Debug.Log("Step: " + message_count + " | " + message_count/(received_time/1000f) + " steps/s");
        }
    }

    /// <summary>
    /// Log OnSubscribe.
    /// </summary>
    protected virtual void OnSubscribe(string[] topics)
    {
        Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Subscribed to topic: " + String.Join(",", topics));
    }

    /// <summary>
    /// Override this method to take some actions when disconnected.
    /// </summary>
    protected virtual void OnDisconnected()
    {
        Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Disconnected.");
    }

    /// <summary>
    /// Override this method to take some actions when the connection is closed.
    /// </summary>
    protected virtual void OnConnectionLost()
    {
        Debug.LogWarning(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | CONNECTION LOST!");
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
                //Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | CONNECTING..");
                client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                //System.Security.Cryptography.X509Certificates.X509Certificate cert = new System.Security.Cryptography.X509Certificates.X509Certificate();
                //MQTTSimClient = new MqttClient(brokerAddress, brokerPort, isEncrypted, cert, null, MqttSslProtocols.TLSv1_0, MyRemoteCertificateValidationCallback);
                //Debug.Log(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | CONNECTED");


            }
            catch (Exception e)
            {
                client = null;
                Debug.LogErrorFormat(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | CONNECTION FAILED! {0}", e.ToString());
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
            Debug.LogErrorFormat(this.GetType().Name + " | " + System.Reflection.MethodBase.GetCurrentMethod().Name + " | Failed to connect to {0}:{1}:\n{2}", brokerAddress, brokerPort, e.ToString());
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

    // Start is called before the first frame update
    void Start()
    {
        Connect();
        SubscribeTopics(topics);

        JSONObject payload = new JSONObject();
        payload.Add("admin", "true");
        payload.Add("sys_info", "...");
        JSONObject msg = new JSONObject();
        msg.Add("sender", nick);
        msg.Add("op", "001");
        msg.Add("payload", payload);
        SendMessage(msg);

        payload = (JSONObject)sim_prototypes_list[1];
        msg.Add("op", "004");
        msg.Add("payload", payload);
        SendMessage(msg);

        payload = new JSONObject();
        payload.Add("command", 1);
        payload.Add("value", 0);

        msg.Add("op", "006");
        msg.Add("payload", payload);
        SendMessage(msg);

    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
