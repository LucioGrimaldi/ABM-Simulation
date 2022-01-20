using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using static SceneController;
using System.Collections.Generic;
using SimpleJSON;

public class SpeedChangeEventArgs : EventArgs
{
    public int Speed { get; set; }
}

public class UIController : MonoBehaviour
{
    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;

    // UI Events
    public static event EventHandler<EventArgs> OnPlayEventHandler;
    public static event EventHandler<EventArgs> OnPauseEventHandler;
    public static event EventHandler<EventArgs> OnStopEventHandler;
    public static event EventHandler<SpeedChangeEventArgs> OnSpeedChangeEventHandler;
    public static event EventHandler<SimParamsUpdateEventArgs> OnSimParamsUpdateEventHandler;
    public static event EventHandler<SimObjectModifyEventArgs> OnSimObjectParamsUpdateEventHandler;
    public static event EventHandler<EventArgs> OnEditExitEventHandler;
    public static event EventHandler<EventArgs> OnExitEventHandler;

    // Controllers
    private SceneController SceneController;

    // Sprites Collection
    private List<NamedPrefab> AgentsData;
    private List<NamedPrefab> GenericsData;
    private List<NamedPrefab> ObstaclesData;

    // Variables
    Color32 cyan = new Color32(146, 212, 219, 0);
    Color32 black_gray = new Color32(56, 61, 63, 0);

    public Camera camera;
    public TMP_Text nickname, admin_nickname;
    public bool showEditPanel = false, showSettingsPanel = false, showInfoPanel = false, showQuitPanel = false, showInspectorPanel = false, admin;
    public GameObject panelSimButtons, panelEditMode, panelInspector, panelSimParams, panelBackToMenu, panelFPS, 
        InspectorParamPrefab, InspectorTogglePrefab, InspectorContent, SimParamPrefab, SimParamPrefab_Disabled, SimTogglePrefab, simParamsContent,
        simToggle, envToggle, contentAgents, contentGenerics, contentObstacles, editPanelSimObject_prefab;
    public Slider slider;
    public Image imgEditMode, imgSimState, imgContour;
    public Button buttonEdit, muteUnmuteButton;
    public AudioSource backgroundMusic;
    public Sprite[] commandSprites;
    public Sprite[] muteUnmuteSprites;
    public Text inspectorType, inspectorClass, inspectorId, emptyScrollTextInspector, emptyScrollTextSimParams;
    public static bool showSimSpace, showEnvironment;
    public Dictionary<string, object> tempSimParams = new Dictionary<string, object>();
    public Dictionary<string, object> tempSimObjectParams = new Dictionary<string, object>();

    
    private float musicVolume;

    private void Awake()
    {

        nickname.text = playerPreferencesSO.nickname;
        showSimSpace = playerPreferencesSO.showSimSpace;
        showEnvironment = playerPreferencesSO.showEnvironment;
        musicVolume = playerPreferencesSO.musicVolume;
        admin_nickname.text = SimulationController.admin_name;

        if (musicVolume == 0f)
        {
            muteUnmuteButton.GetComponent<Image>().sprite = muteUnmuteSprites[1];
            backgroundMusic.mute = true;
        }

        // Bind Controllers
        SceneController = GameObject.Find("SceneController").GetComponent<SceneController>();

        Debug.Log("sim space: " + showSimSpace + " env: " + showEnvironment);

        simToggle.GetComponent<Toggle>().isOn = showSimSpace;
        envToggle.GetComponent<Toggle>().isOn = showEnvironment;
        if (envToggle.GetComponent<Toggle>().isOn)
            camera.backgroundColor = cyan;
        else camera.backgroundColor = black_gray;

    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    private void OnEnable()
    {
        slider.onValueChanged.AddListener(delegate { MoveSlider(); });
    }
    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    void Start()
    {
        AgentsData = SceneController.PO_Prefab_Collection[simId].PO_AgentPrefabs;
        GenericsData = SceneController.PO_Prefab_Collection[simId].PO_GenericPrefabs;
        ObstaclesData = SceneController.PO_Prefab_Collection[simId].PO_ObstaclePrefabs;

        PopulateEditPanel();
        emptyScrollTextSimParams.gameObject.SetActive(false);

        LoadSimDimensions(simParamsContent, (JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["dimensions"]);
        LoadAgentAmounts(simParamsContent, (JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["agent_prototypes"]);
        LoadSimParams(simParamsContent, (JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["sim_params"]);
                
    }
    /// <summary>
    /// Update routine (Unity Process)
    /// </summary>
    void Update()
    {
        if (simToggle.GetComponent<Toggle>().isOn)
            showSimSpace = true;
        else showSimSpace = false;

        if (envToggle.GetComponent<Toggle>().isOn)
        {
            showEnvironment = true;
            camera.backgroundColor = cyan;
        }
        else
        {
            showEnvironment = false;
            camera.backgroundColor = black_gray;

        }

        backgroundMusic.volume = musicVolume;

        admin_nickname.text = "Admin: " + SimulationController.admin_name;
        admin = SimulationController.admin;

        CheckIfAdmin(admin);
    }


    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    void OnApplicationQuit()
    {

    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {
        slider.onValueChanged.RemoveAllListeners();

    }
    /// <summary>
    /// onDestroy routine (Unity Process)
    /// </summary>
    private void OnDestroy()
    {
        
    }

    //ALTRI METODI

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

    public void MoveSlider()
    {
        SpeedChangeEventArgs e = new SpeedChangeEventArgs();
        e.Speed = (int)slider.value;
        OnSpeedChangeEventHandler?.BeginInvoke(this, e, null, null);
    }

    public void PlaySimulation()
    {
        imgSimState.GetComponent<Image>().color = Color.green;
        imgSimState.GetComponent<Image>().sprite = commandSprites[1];
        buttonEdit.interactable = false;
        OnPlayEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
    }
    public void PauseSimulation()
    {
        imgSimState.GetComponent<Image>().color = Color.yellow;
        imgSimState.GetComponent<Image>().sprite = commandSprites[0];
        buttonEdit.interactable = true;
        OnPauseEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
    }
    public void StopSimulation()
    {
        imgSimState.GetComponent<Image>().color = Color.red;
        imgSimState.GetComponent<Image>().sprite = commandSprites[2];
        buttonEdit.interactable = true;
        OnStopEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
    }

    public void CheckIfAdmin(bool admin)
    {
        if (admin)
        {
            if (!panelSimButtons.activeSelf && !panelEditMode.activeSelf)
            {
                panelSimButtons.gameObject.SetActive(true);
                buttonEdit.interactable = true;
            }
        }
        else
        {
            panelSimButtons.gameObject.SetActive(false);
            panelEditMode.gameObject.SetActive(false);
            buttonEdit.interactable = false;
            LoadSimParams(simParamsContent, (JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["sim_params"]);
        }
    }

    public void PopulateEditPanel()
    {
        foreach (NamedPrefab po in AgentsData)
        {
            Sprite s = po.prefab.GetComponent<PlaceableObject>().SimObjectRender.Sprites["default"];
            GameObject o = Instantiate(editPanelSimObject_prefab, contentAgents.transform);
            o.GetComponent<Image>().sprite = s;
            o.GetComponent<Button>().onClick.AddListener(() =>
            {
                SceneController.SelectGhost(0, o.transform.GetSiblingIndex());
            });
        }
        foreach (NamedPrefab po in GenericsData)
        {
            Sprite s = po.prefab.GetComponent<PlaceableObject>().SimObjectRender.Sprites["default"];
            GameObject o = Instantiate(editPanelSimObject_prefab, contentGenerics.transform);
            o.GetComponent<Image>().sprite = s;
            o.GetComponent<Button>().onClick.AddListener(() =>
            {
                SceneController.SelectGhost(1, o.transform.GetSiblingIndex());
            });
        }
        foreach (NamedPrefab po in ObstaclesData)
        {
            Sprite s = po.prefab.GetComponent<PlaceableObject>().SimObjectRender.Sprites["default"];
            GameObject o = Instantiate(editPanelSimObject_prefab, contentObstacles.transform);
            o.GetComponent<Image>().sprite = s;
            o.GetComponent<Button>().onClick.AddListener(() =>
            {
                SceneController.SelectGhost(2, o.transform.GetSiblingIndex());
            });
        }
    }
    public void PopulateInspector(PlaceableObject po)
    {
        EmptyInspectorParams();
        tempSimObjectParams.Clear();
        emptyScrollTextInspector.gameObject.SetActive(false);
        LoadInspectorInfo(po.SimObject.Type, po.SimObject.Class_name, po.SimObject.Id);
        LoadInspectorParams(SceneController.GetSimObjectParamsPrototype(po.SimObject.Type, po.SimObject.Class_name));
    }
    public void EmptyInspectorParams()
    {
        foreach (Transform child in InspectorContent.transform) GameObject.Destroy(child.gameObject);
        InspectorContent.transform.DetachChildren();
    }
    public void LoadInspectorInfo(SimObject.SimObjectType type, string class_name, int id)
    {
        inspectorType.text = type.ToString();
        inspectorClass.text = class_name;
        inspectorId.text = id.ToString();
    }
    public void LoadInspectorParams(JSONArray parameters)
    {
        GameObject param;

        foreach (JSONObject p in parameters)
        {
            if (p.HasKey("editable_in_play") && p["editable_in_play"].Equals(true) || !p.HasKey("editable_in_play"))
            {
                switch ((string)p["type"])
                {
                    case "System.Single":
                        param = Instantiate(InspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimObjectParamUpdate(p["name"], float.Parse(value)));
                        break;
                    case "System.Int32":
                        param = Instantiate(InspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimObjectParamUpdate(p["name"], int.Parse(value)));
                        break;
                    case "System.Boolean":
                        param = Instantiate(InspectorTogglePrefab);
                        param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => OnSimObjectParamUpdate(p["name"], value));
                        param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                        param.GetComponentInChildren<Toggle>().isOn = p["defalut"];
                        break;
                    case "System.String":
                        param = Instantiate(InspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimObjectParamUpdate(p["name"], value));
                        break;
                    case "System.Position":
                        param = Instantiate(InspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimObjectParamUpdate(p["name"], value));
                        break;
                    case "System.Cells":
                        // multiple elements prefab
                        continue;
                    default:
                        continue;
                }
                param.transform.SetParent(InspectorContent.transform);
                if (!((string)p["type"]).Equals("System.Boolean"))
                {
                    param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
                    param.GetComponentInChildren<InputField>().characterLimit = 20;
                    param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                    param.transform.Find("InputField").GetComponent<InputField>().text = p["default"];
                }
            }
        }
        if (InspectorContent.transform.childCount > 0)
        {
            param = Instantiate(InspectorTogglePrefab);
            //param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => 
            param.transform.Find("Param Name").GetComponent<Text>().text = "Follow";
            param.GetComponentInChildren<Toggle>().isOn = false;
            param.transform.SetParent(InspectorContent.transform);
        }
        else
        {
            emptyScrollTextInspector.text = "No inspector parameters available";
            emptyScrollTextInspector.gameObject.SetActive(true);
        }
    }
    public void LoadSimParams(GameObject scrollContent, JSONArray parameters)
    {
        foreach (JSONObject p in parameters)
        {
            if (p.HasKey("editable_in_play") && p["editable_in_play"].Equals(true) || !p.HasKey("editable_in_play"))
            {
                GameObject param;
                switch ((string)p["type"])
                {
                    case "System.Single":
                        param = Instantiate(SimParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], float.Parse(value.Replace('.', ','))));
                        break;
                    case "System.Int32":
                        param = Instantiate(SimParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], int.Parse(value)));
                        break;
                    case "System.Boolean":
                        param = Instantiate(SimTogglePrefab);
                        param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => OnSimParamUpdate(p["name"], value));
                        param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                        param.GetComponentInChildren<Toggle>().isOn = p["defalut"];
                        break;
                    case "System.String":
                        param = Instantiate(SimParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], value));
                        break;
                    case "System.Position":
                        param = Instantiate(SimParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimObjectParamUpdate(p["name"], value));
                        break;
                    case "System.Cells":
                        // multiple elements prefab
                        continue;
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
            emptyScrollTextSimParams.text = "No simulation parameters available";
            emptyScrollTextSimParams.gameObject.SetActive(true);
        }
    }
    public void LoadSimDimensions(GameObject scrollContent, JSONArray dimensions)
    {
        foreach (JSONObject d in dimensions)
        {
            GameObject param;
            switch ((string)d["type"])
            {
                case "System.Single":
                    param = Instantiate(SimParamPrefab_Disabled);
                    param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
                    param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(d["name"], float.Parse(value.Replace('.', ','))));  // TODO ----------------
                    break;
                case "System.Int32":
                    param = Instantiate(SimParamPrefab_Disabled);
                    param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                    param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(d["name"], int.Parse(value))); // TODO ----------------
                    break;
                default:
                    return;
            }
            param.transform.SetParent(scrollContent.transform);

            param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
            param.GetComponentInChildren<InputField>().characterLimit = 20;
            param.transform.Find("Param Name").GetComponent<Text>().text = (d["name"] == "x") ? "Width" : (d["name"] == "z") ? "Lenght" : "Height";
            param.transform.Find("InputField").GetComponent<InputField>().text = d["default"];
        }
    }
    public void LoadAgentAmounts(GameObject scrollContent, JSONArray agentPrototypes)
    {
        foreach (JSONObject p in agentPrototypes)
        {
            GameObject param;
            param = Instantiate(SimParamPrefab_Disabled);
            param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
            param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], int.Parse(value))); //TODO ---------------

            param.transform.SetParent(scrollContent.transform);

            param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
            param.GetComponentInChildren<InputField>().characterLimit = 20;
            param.transform.Find("Param Name").GetComponent<Text>().text = p["class"] + "s amount";
            param.transform.Find("InputField").GetComponent<InputField>().text = p["default"];
        }
    }

    public void OnSimParamUpdate(string param_name, dynamic value)
    {
        if (!tempSimParams.ContainsKey(param_name)) tempSimParams.Add(param_name, value);
        else tempSimParams[param_name] = value;
    }
    public void OnSimParamsApply()
    {
        SimParamsUpdateEventArgs e = new SimParamsUpdateEventArgs();
        e.parameters = tempSimParams;
        OnSimParamsUpdateEventHandler?.BeginInvoke(this, e, null, null); 
    }
    public void OnSimParamsDiscard()
    {
        tempSimParams.Clear();
    }
    public void OnSimObjectParamUpdate(string param_name, dynamic value)
    {
        if (!tempSimObjectParams.ContainsKey(param_name)) tempSimObjectParams.Add(param_name, value);
        else tempSimObjectParams[param_name] = value;
    }
    public void OnSimObjectParamsApply()
    {
        SimObjectModifyEventArgs e = new SimObjectModifyEventArgs();
        e.type = SceneController.selectedPlaced.SimObject.Type;
        e.class_name = SceneController.selectedPlaced.SimObject.Class_name;
        e.id = SceneController.selectedPlaced.SimObject.Id;
        e.parameters = tempSimObjectParams;
        OnSimObjectParamsUpdateEventHandler?.BeginInvoke(this, e, null, null);
    }
    public void OnSimObjectParamsDiscard()
    {
        tempSimObjectParams.Clear();
    }
    

    //public void ConfirmEdit()
    //{
    //    EventArgs e = new EventArgs();
    //    OnEditExitEventHandler?.BeginInvoke(this, e, null, null);
    //}

    public void ShowHideEditPanel()
    {
        panelSimButtons.gameObject.SetActive(showEditPanel);
        panelEditMode.gameObject.SetActive(!showEditPanel);
        imgEditMode.gameObject.SetActive(!showEditPanel);
        imgContour.gameObject.SetActive(!showEditPanel);
        showEditPanel = !showEditPanel;
        //if (showEditPanel == false) ConfirmEdit();
    }
    public void ShowHidePanelSettings()
    {
        panelSimParams.gameObject.SetActive(!showSettingsPanel);
        showSettingsPanel = !showSettingsPanel;
    }
    public void ShowHideInfoPanel()
    {
        panelFPS.gameObject.SetActive(!showInfoPanel);
        showInfoPanel = !showInfoPanel;
    }
    public void ShowHidePanelQuit()
    {
        panelBackToMenu.gameObject.SetActive(!showQuitPanel);
        showQuitPanel = !showQuitPanel;
    }
    public void ShowHidePanelInspector()
    {
        panelInspector.gameObject.SetActive(!showInspectorPanel);
        showInspectorPanel = !showInspectorPanel;
    }
    public void MuteUnmuteAudio()
    {
        if (backgroundMusic.mute)
        {
            muteUnmuteButton.GetComponent<Image>().sprite = muteUnmuteSprites[0];
            backgroundMusic.mute = false;
        }
        else
        {
            muteUnmuteButton.GetComponent<Image>().sprite = muteUnmuteSprites[1];
            backgroundMusic.volume = 0.3f;
            backgroundMusic.mute = true;
        }
    }
    public void StoreDataPreferences(string nameString, bool toggleSimSpace, bool toggleEnvironment)
    {
        playerPreferencesSO.nickname = nameString;
        playerPreferencesSO.showSimSpace = toggleSimSpace;
        playerPreferencesSO.showEnvironment = toggleEnvironment;
    }
    public void BackToMenu()    //distruggi settaggi prima di uscire
    {
        StoreDataPreferences(nickname.text, showSimSpace, showEnvironment);
        OnExitEventHandler.BeginInvoke(this, new EventArgs(), null, null);
        SceneManager.LoadScene("MenuScene");
    }
}
