using Fixed;
using SimpleJSON;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class MenuController : MonoBehaviour
{
    // Player Preferences
    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;

    // Menu State
    public enum MenuState
    {
        MAIN,
        SETTINGS,
        NEWSIM,
        SIM_SETTINGS,
        AGENTS_SETTINGS,
        OBJECTS_SETTINGS
    }
    public MenuState menuState = MenuState.MAIN;

    // Sim Infos
    List<int> IDs = new List<int>();
    List<string> simNames = new List<string>();
    List<string> descriptions = new List<string>();
    List<string> types = new List<string>();
    List<int> dimensions = new List<int>();
    List<List<string>> agents = new List<List<string>>();
    List<List<string>> generics = new List<List<string>>();
    List<Sprite> simSprites = new List<Sprite>();
    List<List<Sprite>> agentsSprites = new List<List<Sprite>>();
    List<List<Sprite>> objectsSprites = new List<List<Sprite>>();

    // UI Events
    public static event EventHandler<NicknameEnterEventArgs> OnNicknameEnterEventHandler;
    public static event EventHandler<SimPrototypeConfirmedEventArgs> OnSimPrototypeConfirmedEventHandler;
    public static event EventHandler<EventArgs> OnJoinSimulationEventHandler;
    public static event EventHandler<EventArgs> OnLoadMainMenuHandler;

    // UI Action Queue
    public static readonly ConcurrentQueue<Action> MenuMainThreadQueue = new ConcurrentQueue<Action>();

    // Variables
    public TMP_Dropdown dropdownSimTypes, dropdownAgents, dropdownObjects;
    public TMP_Text simDescription, simDescription2, simName, emptyScrollText;
    public Image simImage1, simImage2, agentImage;
    public GameObject nickCheckSign, inMenuParamPrefab, inMenuTogglePrefab, settingsScrollContent, agentsScrollContent, objectsScrollContent, simToggle, envToggle;
    public InputField nicknameField;
    public Button newSimButton, joinSimButton, simSettingsButton, agentsSettingsButton, objectsSettingsButton;
    public AudioSource backgroundMusic, testAudioEffect;
    public Slider musicSlider, effectsSlider;
    public GameObject mainMenu, newSimScreen, panelBusy;
    private float musicVolume, effectsVolume;
    private bool showSimSpace, showEnvironment, showPanelBusy = false;
    
    
    // UNITY LOOP METHODS
    private void Awake()
    {
        nicknameField.text = playerPreferencesSO.nickname;
        showSimSpace = playerPreferencesSO.showSimSpace;
        showEnvironment = playerPreferencesSO.showEnvironment;
        musicVolume = playerPreferencesSO.musicVolume;
        effectsVolume = playerPreferencesSO.effectsVolume;

        musicSlider.value = musicVolume;
        effectsSlider.value = effectsVolume;

        if (nicknameField.text.Length > 0) SavePlayerName();

        simToggle.GetComponent<Toggle>().isOn = showSimSpace;
        envToggle.GetComponent<Toggle>().isOn = showEnvironment;
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    private void OnEnable()
    {
        // Register to EventHandlers
        CommunicationController.OnControlClientConnectedHandler += onControlClientConnected;
        SimulationController.OnConnectionSuccessEventHandler += onConnectionSuccess;
        SimulationController.OnConnectionUnsuccessEventHandler += onConnectionUnsuccess;
        SimulationController.OnCheckStatusSuccessEventHandler += onCheckStatusSuccess;
        SimulationController.OnCheckStatusUnsuccessEventHandler += onCheckStatusUnsuccess;
        SimulationController.OnSimListSuccessEventHandler += onSimListSuccess;
        SimulationController.OnSimListUnsuccessEventHandler += onSimListUnsuccess;
        SimulationController.OnSimInitSuccessEventHandler += onSimInitSuccess;
        SimulationController.OnSimInitUnsuccessEventHandler += onSimInitUnsuccess;
    }
    private void Start()
    {
    }
    private void Update()
    {
        if (!MenuMainThreadQueue.IsEmpty)
        {
            while (MenuMainThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        backgroundMusic.volume = musicVolume;
        testAudioEffect.volume = effectsVolume;
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    void OnApplicationQuit()
    {
        while(MenuMainThreadQueue.TryDequeue(out _));
    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {
        // Unregister to EventHandlers
        CommunicationController.OnControlClientConnectedHandler -= onControlClientConnected;
        SimulationController.OnConnectionSuccessEventHandler -= onConnectionSuccess;
        SimulationController.OnConnectionUnsuccessEventHandler -= onConnectionUnsuccess;
        SimulationController.OnCheckStatusSuccessEventHandler -= onCheckStatusSuccess;
        SimulationController.OnCheckStatusUnsuccessEventHandler -= onCheckStatusUnsuccess;
        SimulationController.OnSimListSuccessEventHandler -= onSimListSuccess;
        SimulationController.OnSimListUnsuccessEventHandler -= onSimListUnsuccess;
        SimulationController.OnSimInitSuccessEventHandler -= onSimInitSuccess;
        SimulationController.OnSimInitUnsuccessEventHandler -= onSimInitUnsuccess;
    }

 
    // Button Callbacks


    // Event Callbacks (NOT ON MAIN THREAD!)

    private void onControlClientConnected(object sender, EventArgs e)
    {
        OnLoadMainMenuHandler?.BeginInvoke(sender, e, EndInvoke, null);
    }
    private void onConnectionSuccess(object sender, ReceivedMessageEventArgs e)
    {
        if (SimulationController.admin)
        {
            MenuMainThreadQueue.Enqueue(() => {
                newSimScreen.SetActive(true);
                mainMenu.SetActive(false);
            });
        }
        else
        {
            MenuMainThreadQueue.Enqueue(() =>
            {
                ShowHidePanelBusy();
                mainMenu.SetActive(false);
            });
        }
    }
    private void onConnectionUnsuccess(object sender, ReceivedMessageEventArgs e)
    {
        // someone already connected with that nick
        if (!SimulationController.admin)
        {
            MenuMainThreadQueue.Enqueue(() =>
            {
                ShowHidePanelBusy();
                mainMenu.SetActive(false);
            });
        }
    }
    private void onCheckStatusSuccess(object sender, ReceivedMessageEventArgs e)
    {
        if (e.Payload["payload_data"]["state"].Equals(-1))
        {
            if (menuState.Equals(MenuState.MAIN))
                {
                    MenuMainThreadQueue.Enqueue(() => { SetSimButton(true);});
                }
        }
        else
        {
            if (menuState.Equals(MenuState.MAIN))
            {
                MenuMainThreadQueue.Enqueue(() => { SetSimButton(false); });
            }
        }
    }
    private void onCheckStatusUnsuccess(object sender, ReceivedMessageEventArgs e)
    {
        // TODO
    }
    private void onSimListSuccess(object sender, ReceivedMessageEventArgs e)
    {
        SimulationController.sim_list_editable = (JSONArray)e.Payload["payload_data"]["list"].Clone();
        
        if (SimulationController.admin)
        {
            MenuMainThreadQueue.Enqueue(() => { ChangeMenuState(); });
            MenuMainThreadQueue.Enqueue(() => { LoadSimNames(SimulationController.sim_list_editable); });
            MenuMainThreadQueue.Enqueue(() => { PopulateSimListDropdown(); });
            MenuMainThreadQueue.Enqueue(() => { LoadSprites(); });
        }
    }
    private void onSimListUnsuccess(object sender, ReceivedMessageEventArgs e)
    {
        // TODO
    }
    private void onSimInitSuccess(object sender, ReceivedMessageEventArgs e)
    {
        MenuMainThreadQueue.Enqueue(() => { SceneManager.LoadScene("MainScene"); });
    }
    private void onSimInitUnsuccess(object sender, ReceivedMessageEventArgs e)
    {
        // TODO
    }
    private void EndInvoke(IAsyncResult result)
    {
        OnLoadMainMenuHandler?.EndInvoke(result);
    }

    //ALTRI METODI

    public void CheckNickname()
    {
        bool nickAvail = true;
        if (nicknameField.text.Length > 0 && nicknameField.text.Length < 16 && !nicknameField.text.Contains(" ") && nickAvail)
        {
            nickCheckSign.SetActive(true);
            newSimButton.interactable = true;
            joinSimButton.interactable = true;
        }
        else
        {
            nickCheckSign.SetActive(false);
            newSimButton.interactable = false;
            joinSimButton.interactable = false;
        }
    }
    public void SavePlayerName()
    {
        NicknameEnterEventArgs e = new NicknameEnterEventArgs();
        e.nickname = nicknameField.text;
        OnNicknameEnterEventHandler?.BeginInvoke(this, e, null, null);
    }
    public void ShowHidePanelBusy()
    {
        panelBusy.gameObject.SetActive(!showPanelBusy);
        showPanelBusy = !showPanelBusy;
    }
    private void onSimParamModified(string param_name, dynamic value)
    {
        int x = 0;
        ((JSONArray)((JSONObject)SimulationController.sim_list_editable[SimulationController.sim_id])["sim_params"]).Linq.Where((p, i) => { if (p.Key.Equals(param_name)) { x = i; return true;} return false; });
        ((JSONObject)((JSONArray)((JSONObject)SimulationController.sim_list_editable[SimulationController.sim_id])["sim_params"])[x])["default"] = value;
    }
    private void onAgentParamModified(int id, string param_name, dynamic value)
    {
        foreach ((int i, KeyValuePair<string, JSONNode>) param in SimulationController.sim_list_editable[SimulationController.sim_id]["agent_prototypes"][id]["params"].Linq.Select((v, i) => (i,v)))
        {
            if (param.Item2.Key.Equals(param_name))
            {
                SimulationController.sim_list_editable[SimulationController.sim_id]["agent_prototypes"][id]["params"][param.Item1]["default"] = value;
            }
        }
    }
    private void onGenericParamModified(int id, string param_name, dynamic value)
    {
        foreach ((int i, KeyValuePair<string, JSONNode>) param in SimulationController.sim_list_editable[SimulationController.sim_id]["generic_prototypes"][id]["params"].Linq.Select((v, i) => (i, v)))
        {
            if (param.Item2.Key.Equals(param_name))
            {
                SimulationController.sim_list_editable[SimulationController.sim_id]["generic_prototypes"][id]["params"][param.Item1]["default"] = value;
            }
        }
    }
    public void ConfirmEditedPrototype()
    {
        SimPrototypeConfirmedEventArgs e = new SimPrototypeConfirmedEventArgs();
        e.sim_prototype = (JSONObject)SimulationController.sim_list_editable[SimulationController.sim_id];
        OnSimPrototypeConfirmedEventHandler?.BeginInvoke(this, e, null, null);
    }
    public void StoreDataPreferences(string nameString, bool toggleSimSpace, bool toggleEnvironment, float musicVolume, float effectsVolume)
    {
        playerPreferencesSO.nickname = nameString;
        playerPreferencesSO.showSimSpace = toggleSimSpace;
        playerPreferencesSO.showEnvironment = toggleEnvironment;
        playerPreferencesSO.musicVolume = musicVolume;
        playerPreferencesSO.effectsVolume = effectsVolume;

    }
    public void ChangeMenuState()
    {
        foreach (GameObject c in GameObject.FindGameObjectsWithTag("Menu"))
        {
            if (c.activeSelf)
            {
                switch (c.name)
                {
                    case "MainMenu":
                        menuState = MenuState.MAIN;
                        break;
                    case "SettingsMenu":
                        menuState = MenuState.SETTINGS;
                        break;
                    case "NewSimScreen":
                        menuState = MenuState.NEWSIM;
                        break;
                    case "SimSettingsScreen":
                        menuState = MenuState.SIM_SETTINGS;
                        break;
                    case "AgentsSettingsScreen":
                        menuState = MenuState.AGENTS_SETTINGS;
                        break;
                    case "ObjectsSettingsScreen":
                        menuState = MenuState.OBJECTS_SETTINGS;
                        break;
                }
                return;
            } 
        } 
    }
    public void JoinSimulation()
    {
        OnJoinSimulationEventHandler.BeginInvoke(this, new EventArgs(), null, null);
        OnLoadSimulationScene();
        MenuMainThreadQueue.Enqueue(() => { SceneManager.LoadScene("MainScene"); });
    }

    private void LoadSimNames(JSONArray simList)
    {
        simNames.Clear();
        agents.Clear();
        generics.Clear();

        foreach (JSONObject s in simList)
        {
            IDs.Add(s["id"]);
            simNames.Add(s["name"]);
            descriptions.Add(s["description"]);
            types.Add(s["type"]);
            dimensions.Add(((JSONArray)s["dimensions"]).Count);

            List<string> a_list = new List<string>();
            List<string> g_list = new List<string>();
            foreach (JSONObject a in (JSONArray)s["agent_prototypes"])
                a_list.Add(a["class"]);
            agents.Add(a_list);

            foreach (JSONObject a in (JSONArray)s["generic_prototypes"])
                g_list.Add(a["class"]);
            generics.Add(g_list);
        }
    }
    private void LoadSimInfos(JSONObject sim)
    {
        simDescription.SetText(sim["description"] + "\n" + sim["type"]);
        simDescription2.SetText(simDescription.text);
    }
    private void LoadSprites()
    {
        List<Sprite> aSprites = new List<Sprite>();
        List<Sprite> oSprites = new List<Sprite>();

        foreach (String s in simNames)
        {
            this.simSprites.Add(Resources.Load<Sprite>("Sprites/sprite" + s));

            foreach (String a in agents[simNames.IndexOf(s)])
            {
                aSprites.Add(Resources.Load<Sprite>("Sprites/" + s + "Agents" + "/sprite" + a + "_w"));
            }
            agentsSprites.Add(aSprites);

            foreach (String o in generics[simNames.IndexOf(s)])
            {
                oSprites.Add(Resources.Load<Sprite>("Sprites/" + s + "Objects" + "/sprite" + o + "_w"));
            }
            objectsSprites.Add(oSprites);
        }
    }
    public void LoadParams(GameObject scrollContent, int id, JSONArray parameters)
    {
        emptyScrollText.gameObject.SetActive(false);
        foreach (JSONObject p in parameters)
        {
            if (p.HasKey("editable_in_init") && p["editable_in_init"].Equals(true) || !p.HasKey("editable_in_init"))
            {
                GameObject param;
                switch ((string)p["type"])
                {
                    case "System.Single":
                        param = Instantiate(inMenuParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
                        if (scrollContent.name.Contains("agent")) param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onAgentParamModified(id, p["name"], float.Parse(value)));
                        else param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onGenericParamModified(id, p["name"], float.Parse(value)));
                        break;
                    case "System.Int32":
                        param = Instantiate(inMenuParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                        if (scrollContent.name.Contains("agent")) param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onAgentParamModified(id, p["name"], int.Parse(value)));
                        else param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onGenericParamModified(id, p["name"], int.Parse(value)));
                        break;
                    case "System.Boolean":
                        param = Instantiate(inMenuTogglePrefab);
                        if (scrollContent.name.Contains("agent")) param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => onAgentParamModified(id, p["name"], value));
                        else param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => onGenericParamModified(id, p["name"], value));
                        param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                        param.GetComponentInChildren<Toggle>().isOn = p["defalut"];
                        break;
                    case "System.String":
                        param = Instantiate(inMenuParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        if (scrollContent.name.Contains("agent")) param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onAgentParamModified(id, p["name"], value));
                        else param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onGenericParamModified(id, p["name"], value));
                        break;
                    default:
                        return;
                }
                param.transform.SetParent(scrollContent.transform);
                if (!((string)p["type"]).Equals("System.Boolean"))
                {
                    param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
                    param.GetComponentInChildren<InputField>().characterLimit = 20;
                    param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                    param.transform.Find("InputField").GetComponent<InputField>().text = p["default"];
                }
            }
        }
        if (scrollContent.transform.childCount == 0)
        {
            emptyScrollText.text = "No params available for this " + (scrollContent.name.Equals("ContentSettings") ? "Simulation" : (scrollContent.name.Equals("ContentAgents") ? "Agent" : "Object"));
            emptyScrollText.gameObject.SetActive(true);
        }
    }
    public void LoadSimParams(GameObject scrollContent, JSONArray parameters)
    {
        emptyScrollText.gameObject.SetActive(false);
        foreach (JSONObject p in parameters)
        {
            if(p.HasKey("editable_in_init") && p["editable_in_init"].Equals(true) || !p.HasKey("editable_in_init"))
            {
                GameObject param;
                switch ((string)p["type"])
                {
                    case "System.Single":
                        param = Instantiate(inMenuParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onSimParamModified(p["name"], float.Parse(value)));
                        break;
                    case "System.Int32":
                        param = Instantiate(inMenuParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onSimParamModified(p["name"], int.Parse(value)));
                        break;
                    case "System.Boolean":
                        param = Instantiate(inMenuTogglePrefab);
                        param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => onSimParamModified(p["name"], value));
                        param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                        param.GetComponentInChildren<Toggle>().isOn = p["defalut"];
                        break;
                    case "System.String":
                        param = Instantiate(inMenuParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => onSimParamModified(p["name"], value));
                        break;
                    default:
                        return;
                }
                param.transform.SetParent(scrollContent.transform);
                if (!((string)p["type"]).Equals("System.Boolean"))
                {
                    param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
                    param.GetComponentInChildren<InputField>().characterLimit = 20;
                    param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                    param.transform.Find("InputField").GetComponent<InputField>().text = p["default"];
                }    
            }
        }
        if(scrollContent.transform.childCount == 0)
        {
            emptyScrollText.text = "No params available for this " + (scrollContent.name.Equals("ContentSettings") ? "Simulation" : (scrollContent.name.Equals("ContentAgents") ? "Agent" : "Object"));
            emptyScrollText.gameObject.SetActive(true);
        }
    }
    public void OnToggleSimSpaceChanged(bool value)
    {
        if (simToggle.GetComponent<Toggle>().isOn)
            showSimSpace = true;
        else showSimSpace = false;

    }
    public void OnToggleEnvironmentChanged(bool value)
    {
        if (envToggle.GetComponent<Toggle>().isOn)
            showEnvironment = true;
        else showEnvironment = false;
    }

    public void SetSimButton(Boolean is_new)
    {
        newSimButton.gameObject.SetActive(is_new);
        joinSimButton.gameObject.SetActive(!is_new);
    }
    public void SetButtonsInteractivity()
    {
        if (((JSONArray)((JSONObject)SimulationController.sim_prototypes_list[SimulationController.sim_id])["sim_params"]).Count > 0)
        {
            simSettingsButton.interactable = true;
        }
        else { simSettingsButton.interactable = false; }
        if (((JSONArray)((JSONObject)SimulationController.sim_prototypes_list[SimulationController.sim_id])["agent_prototypes"]).Count > 0)
        {
            agentsSettingsButton.interactable = true;
        }
        else { agentsSettingsButton.interactable = false; }
        if (((JSONArray)((JSONObject)SimulationController.sim_prototypes_list[SimulationController.sim_id])["generic_prototypes"]).Count > 0)
        {
            objectsSettingsButton.interactable = true;
        }
        else { objectsSettingsButton.interactable = false; }
    }

    public void PopulateSimListDropdown()
    {
        dropdownSimTypes.ClearOptions();
        dropdownSimTypes.AddOptions(simNames);
        dropdownSimTypes.onValueChanged.Invoke(0);
    }
    public void PopulateAgentsListDropdown()
    {
        dropdownAgents.ClearOptions();
        dropdownAgents.AddOptions(agents[dropdownSimTypes.value]);
        dropdownAgents.onValueChanged.Invoke(0);
    }
    public void PopulateObjectsListDropdown()
    {
        dropdownObjects.ClearOptions();
        dropdownObjects.AddOptions(generics[dropdownSimTypes.value]);
        dropdownObjects.onValueChanged.Invoke(0);
    }

    public void Dropdown_SimChanged(int index)
    {
        foreach (Transform child in settingsScrollContent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        settingsScrollContent.transform.DetachChildren();
        foreach (Transform child in agentsScrollContent.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        agentsScrollContent.transform.DetachChildren();
        
        SimulationController.sim_id = index;
        LoadSimInfos((JSONObject)SimulationController.sim_list_editable[index]);
        LoadSimParams(settingsScrollContent, (JSONArray)SimulationController.sim_list_editable[index]["sim_params"]);

        // set Buttons interactable
        SetButtonsInteractivity();

        //simImage1.sprite = spriteSimFlocker;

        simName.text = simNames[index];

        simImage2.sprite = simImage1.sprite;
    }
    public void Dropdown_AgentChanged(int index)
    {
        // save params in sim_editable

        foreach (Transform child in agentsScrollContent.transform)
        {
            GameObject.Destroy(child.gameObject);
            agentsScrollContent.transform.DetachChildren();
        }

        LoadParams(agentsScrollContent, index, (JSONArray)((JSONObject)((JSONArray)((JSONObject)SimulationController.sim_list_editable[SimulationController.sim_id])["agent_prototypes"])[index])["params"]);
        //agentImage.sprite = spriteBirdModel;

    }
    public void Dropdown_ObjectChanged(int index)
    {
        // save params in sim_editable

        foreach (Transform child in objectsScrollContent.transform)
        {
            GameObject.Destroy(child.gameObject);
            objectsScrollContent.transform.DetachChildren();
        }

        LoadParams(objectsScrollContent, index, (JSONArray)((JSONObject)((JSONArray)((JSONObject)SimulationController.sim_list_editable[SimulationController.sim_id])["generic_prototypes"])[index])["params"]);

        //agentImage.sprite = spriteBirdModel;

    }
    public void OnLoadSimulationScene()
    {
        ConfirmEditedPrototype();
        StoreDataPreferences(nicknameField.text, showSimSpace, showEnvironment, musicVolume, effectsVolume);
    }
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
    }
    public void SetEffectsVolume(float volume)
    {
        effectsVolume = volume;
    }
    public void TestAudioEffect()
    {
        testAudioEffect.Play();
    }
    public void Quit()
    {
        Application.Quit();
    }
}
