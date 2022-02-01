using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using static SceneController;
using System.Collections.Generic;
using SimpleJSON;
using System.Collections.Concurrent;
using System.Linq;

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

    // UI Action Queue
    public static readonly ConcurrentQueue<Action> UIControllerThreadQueue = new ConcurrentQueue<Action>();

    // Controllers
    private SimulationController SimController;
    private SceneController SceneController;
    private Simulation.StateEnum state;

    // Sprites Collection
    private List<NamedPrefab> AgentsData;
    private List<NamedPrefab> GenericsData;
    private List<NamedPrefab> ObstaclesData;

    // Variables
    //Scene Background Colors
    Color32 cyan = new Color32(146, 212, 219, 0);
    Color32 black_gray = new Color32(56, 61, 63, 0);

    //Sim Params prefab Colors
    Color32 black = new Color32(0, 0, 0, 130);
    Color32 white = new Color32(255, 255, 255, 33);

    public Camera camera;
    public TMP_Text nickname, admin_nickname, step_id;
    public bool showEditPanel = false, showSettingsPanel = false, showInfoPanel = false, showQuitPanel = false, showInspectorPanel = false, admin_UI = true;
    public GameObject panelSimButtons, panelEditMode, panelInspector, panelSimParams, panelBackToMenu, panelFPS,
        inspectorParamPrefab, inspectorTogglePrefab, inspectorContent, simParamPrefab, SimParamPrefab_Disabled, SimTogglePrefab, simParamsContent,
        simToggle, envToggle, contentAgents, contentGenerics, contentObstacles, editPanelSimObject_prefab, followToggle;
    public Slider slider;
    public Image imgEditMode, imgSimState, imgContour;
    public Button buttonEdit, muteUnmuteButton, discardParamButton, applyParamButton, discardInspectorButton, applyInspectorButton;
    public AudioSource backgroundMusic, effectsAudio;
    public Sprite[] commandSprites;
    public Sprite[] muteUnmuteSprites;
    public Text inspectorType, inspectorClass, inspectorId, emptyScrollTextInspector, emptyScrollTextSimParams;
    public static bool showSimSpace, showEnvironment;
    public Dictionary<string, object> tempSimParams = new Dictionary<string, object>();
    public Dictionary<string, object> tempSimObjectParams = new Dictionary<string, object>();
    private float musicVolume, effectsVolume;
    public PlaceableObject selected = null;
    private ReceivedMessageEventArgs last_simUpdate = null;
    private StepAppliedEventArgs last_StepApplied = null;

    private void Awake()
    {
        nickname.text = playerPreferencesSO.nickname;
        showSimSpace = playerPreferencesSO.showSimSpace;
        showEnvironment = playerPreferencesSO.showEnvironment;
        musicVolume = playerPreferencesSO.musicVolume;
        effectsVolume = playerPreferencesSO.effectsVolume;
        admin_nickname.text = SimulationController.admin_name;

        backgroundMusic.volume = musicVolume;
        effectsAudio.volume = effectsVolume;

        if (musicVolume == 0f && effectsVolume == 0f)
        {
            muteUnmuteButton.GetComponent<Image>().sprite = muteUnmuteSprites[0];
            backgroundMusic.mute = true;
            effectsAudio.mute = true;
        }

        // Bind Controllers
        SimController = GameObject.Find("SimulationController").GetComponent<SimulationController>();
        SceneController = GameObject.Find("SceneController").GetComponent<SceneController>();

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
        Simulation.OnStepAppliedEventHandler += onStepApplied;
        SimulationController.OnNewAdminEventHandler += onNewAdmin;
        SimulationController.OnCheckStatusSuccessEventHandler += onCheckStatusSuccess;
        SimulationController.OnSimUpdateSuccessEventHandler += onSimUpdateSuccess;
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
        if (!UIControllerThreadQueue.IsEmpty)
        {
            while (UIControllerThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        state = Simulation.state;
        CheckSimState(state);
        ShowHideEnv_SimSpace();
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
        Simulation.OnStepAppliedEventHandler -= onStepApplied;
        SimulationController.OnNewAdminEventHandler -= onNewAdmin;
        SimulationController.OnCheckStatusSuccessEventHandler -= onCheckStatusSuccess;
        SimulationController.OnSimUpdateSuccessEventHandler -= onSimUpdateSuccess;
    }
    /// <summary>
    /// onDestroy routine (Unity Process)
    /// </summary>
    private void OnDestroy()
    {

    }

    //ALTRI METODI
    
    private void onNewAdmin(object sender, ReceivedMessageEventArgs e)
    {
        UIControllerThreadQueue.Enqueue(() => {

            admin_nickname.text = "Admin: " + SimulationController.admin_name;
            if (SimulationController.admin) UnlockUI();
            else LockUI();
        });
    }
    private void onCheckStatusSuccess(object sender, ReceivedMessageEventArgs e)
    {
        onNewAdmin(sender, e);
    }
    private void onSimUpdateSuccess(object sender, ReceivedMessageEventArgs e)
    {
        last_simUpdate = e;
        UIControllerThreadQueue.Enqueue(() => {
            UpdateSimParams(e);
        });
    }
    private void onStepApplied(object sender, StepAppliedEventArgs e)
    {
        last_StepApplied = e;
        UIControllerThreadQueue.Enqueue(() =>
        {
            UpdateAgentAmounts(e);
            UpdateInspectorParams();
            step_id.text = "" + e.step_id;
        });
    }

    public void UpdateSimParams(ReceivedMessageEventArgs e)
    {
        if (showSettingsPanel)
        {
            foreach (Transform child in simParamsContent.transform)
            {
                if (child.Find("Param Name").GetComponent<Text>().text.Equals("Width"))
                {
                    child.GetComponentInChildren<InputField>().text = "" + ((JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["dimensions"])[0]["default"];
                }
                else if (child.Find("Param Name").GetComponent<Text>().text.Equals("Height"))
                {
                    child.GetComponentInChildren<InputField>().text = "" + ((JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["dimensions"])[1]["default"];
                }
                else if (child.Find("Param Name").GetComponent<Text>().text.Equals("Length"))
                {
                    child.GetComponentInChildren<InputField>().text = "" + ((JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["dimensions"])[2]["default"];
                }
                else if (!child.Find("Param Name").GetComponent<Text>().text.Contains("s amount"))
                {
                    if (child.GetComponentInChildren<InputField>() != null && !child.GetComponentInChildren<InputField>().isFocused)
                    {
                        if (e != null && e.Payload["payload_data"]["payload"].HasKey("sim_params") && e.Payload["payload_data"]["payload"]["sim_params"].HasKey(child.Find("Param Name").GetComponent<Text>().text))
                        {
                            child.GetComponentInChildren<InputField>().text = "" + e.Payload["payload_data"]["payload"]["sim_params"][child.Find("Param Name").GetComponent<Text>().text];
                        }
                        else
                        {
                            child.GetComponentInChildren<InputField>().text = "" + ((JSONObject)((JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["sim_params"]).Linq.Where((param) => param.Value["name"].Equals(child.Find("Param Name").GetComponent<Text>().text)).ToArray()[0])["default"];
                        }
                    }
                    else if (child.GetComponentInChildren<Toggle>() != null)
                    {
                        if (e != null && e.Payload["payload_data"]["payload"].HasKey("sim_params") && e.Payload["payload_data"]["payload"]["sim_params"].HasKey(child.Find("Param Name").GetComponent<Text>().text))
                        {
                            child.GetComponentInChildren<Toggle>().isOn = e.Payload["payload_data"]["payload"]["sim_params"][child.Find("Param Name").GetComponent<Text>().text];
                        }
                        else
                        {
                            child.GetComponentInChildren<Toggle>().isOn = ((JSONObject)((JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["sim_params"]).Linq.Where((param) => param.Value["name"].Equals(child.Find("Param Name").GetComponent<Text>().text)).ToArray()[0])["default"];
                        }
                    }
                }
            }
        }
    }
    public void UpdateAgentAmounts(StepAppliedEventArgs e)
    {
        if (showSettingsPanel)
        {
            foreach (Transform child in simParamsContent.transform)
            {
                if (child.Find("Param Name").GetComponent<Text>().text.Contains("s amount"))
                {
                    string agent_class = child.Find("Param Name").GetComponent<Text>().text.Substring(0, child.Find("Param Name").GetComponent<Text>().text.Length - 8);
                    child.GetComponentInChildren<InputField>().text = "" + e.n_agents_for_each_class[e.agent_class_names.IndexOf(agent_class)];
                }
            }
        }
    }
    public void UpdateInspectorParams()
    {
        if (showInspectorPanel)
        {
            if(selected != null)
            {
                foreach (Transform child in inspectorContent.transform)
                {
                    if (child.GetComponentInChildren<InputField>() != null && !child.GetComponentInChildren<InputField>().isFocused) child.GetComponentInChildren<InputField>().text = "" + selected.SimObject.Parameters[child.Find("Param Name").GetComponent<Text>().text];
                    if (child.GetComponentInChildren<Toggle>() != null) child.GetComponentInChildren<Toggle>().isOn = (bool)selected.SimObject.Parameters[child.Find("Param Name").GetComponent<Text>().text];
                }
            }
            else
            {
                camera.GetComponent<CameraTarget>().follow = false;
                followToggle.GetComponent<Toggle>().isOn = false;
            }
        }
    }

    public void LockUI()
    {
        if (admin_UI)
        {
            admin_UI = false;
            panelSimButtons.gameObject.SetActive(false);
            panelEditMode.gameObject.SetActive(false);
            buttonEdit.interactable = false;
            discardParamButton.interactable = false;
            applyParamButton.interactable = false;
            discardInspectorButton.interactable = false;
            applyInspectorButton.interactable = false;

            foreach (Transform child in simParamsContent.transform)
            {
                if (child.GetComponentInChildren<InputField>() != null) child.GetComponentInChildren<InputField>().interactable = false;
                if (child.GetComponentInChildren<Toggle>() != null) child.GetComponentInChildren<Toggle>().interactable = false;
                child.GetComponent<Image>().color = black;
            }
        }
    }
    public void UnlockUI()
    {
        if (!admin_UI)
        {
            admin_UI = true;
            panelSimButtons.gameObject.SetActive(true);
            buttonEdit.interactable = true;
            discardParamButton.interactable = true;
            applyParamButton.interactable = true;
            discardInspectorButton.interactable = true;
            applyInspectorButton.interactable = true;

            foreach (Transform child in simParamsContent.transform)
            {
                if (child.GetComponentInChildren<Text>().text.Contains("Width") || child.GetComponentInChildren<Text>().text.Contains("Height") ||
                    child.GetComponentInChildren<Text>().text.Contains("Length") || child.GetComponentInChildren<Text>().text.Contains("amount"))
                {
                    if (!Simulation.state.Equals(Simulation.StateEnum.NOT_READY))
                    {
                        if (child.GetComponentInChildren<InputField>() != null) child.GetComponentInChildren<InputField>().interactable = false;
                        if (child.GetComponentInChildren<Toggle>() != null) child.GetComponentInChildren<Toggle>().interactable = false;
                        child.GetComponent<Image>().color = black;
                    }
                }
                else
                {
                    if (child.GetComponentInChildren<InputField>() != null) child.GetComponentInChildren<InputField>().interactable = true;
                    if (child.GetComponentInChildren<Toggle>() != null) child.GetComponentInChildren<Toggle>().interactable = true;
                    child.GetComponent<Image>().color = white;
                }
            }
            foreach (Transform child in inspectorContent.transform)
            {
                if (child.GetComponentInChildren<InputField>() != null) child.GetComponentInChildren<InputField>().interactable = true;
                if (child.GetComponentInChildren<Toggle>() != null) child.GetComponentInChildren<Toggle>().interactable = true;
                child.GetComponent<Image>().color = white;
            }
        }
    }

    private void CheckSimState(Simulation.StateEnum state)
    {
        switch (state)
        {
            case Simulation.StateEnum.NOT_READY:
                imgSimState.GetComponent<Image>().color = Color.red;
                imgSimState.GetComponent<Image>().sprite = commandSprites[2];
                buttonEdit.interactable = true;
                break;
            case Simulation.StateEnum.PLAY:
                imgSimState.GetComponent<Image>().color = Color.green;
                imgSimState.GetComponent<Image>().sprite = commandSprites[1];
                buttonEdit.interactable = false;
                break;
            case Simulation.StateEnum.PAUSE:
            case Simulation.StateEnum.READY:
                imgSimState.GetComponent<Image>().color = Color.yellow;
                imgSimState.GetComponent<Image>().sprite = commandSprites[0];
                buttonEdit.interactable = true;
                break;
            case Simulation.StateEnum.STEP:
                imgSimState.GetComponent<Image>().color = Color.yellow;
                imgSimState.GetComponent<Image>().sprite = commandSprites[3];
                break;
        }
    }

    public void OnFollowToggleClicked()
    {
        if (followToggle.GetComponent<Toggle>().isOn && selected != null)
        {
            camera.GetComponent<CameraTarget>().SetNewCameraTarget(selected.transform.GetChild(1));
            camera.GetComponent<CameraRotation>().enabled = false;
        }
        else
        {
            camera.GetComponent<CameraRotation>().enabled = true;
            camera.GetComponent<CameraTarget>().follow = false;
            camera.transform.position = new Vector3(50, 100, -100);
            camera.transform.rotation = Quaternion.Euler(new Vector3(20, 0, 0));
        }
    }

    public void OnChangeSelectedFollow()
    {
        selected = selectedPlaced;
        
        if (followToggle.GetComponent<Toggle>().isOn)
        {
            camera.GetComponent<CameraTarget>().SetNewCameraTarget(selected.transform.GetChild(1));
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

    public void MoveSlider()
    {
        SpeedChangeEventArgs e = new SpeedChangeEventArgs();
        e.Speed = (int)slider.value;
        OnSpeedChangeEventHandler?.BeginInvoke(this, e, null, null);
    }

    public void PlaySimulation()
    {
        foreach (Transform child in simParamsContent.transform)
        {
            if (child.GetComponentInChildren<Text>().text.Contains("Width") || child.GetComponentInChildren<Text>().text.Contains("Height") ||
                child.GetComponentInChildren<Text>().text.Contains("Length") || child.GetComponentInChildren<Text>().text.Contains("amount"))
            {
                child.GetComponentInChildren<InputField>().interactable = false;
                child.GetComponent<Image>().color = black;

            }
        }
        OnPlayEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
    }
    public void PauseSimulation()
    {
        foreach (Transform child in simParamsContent.transform)
        {
            if (child.GetComponentInChildren<Text>().text.Contains("Width") || child.GetComponentInChildren<Text>().text.Contains("Height") ||
                child.GetComponentInChildren<Text>().text.Contains("Length") || child.GetComponentInChildren<Text>().text.Contains("amount"))
            {
                child.GetComponentInChildren<InputField>().interactable = false;
                child.GetComponent<Image>().color = black;

            }
        }
        OnPauseEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
    }
    public void StopSimulation()
    {
        foreach (Transform child in simParamsContent.transform)
        {
            child.GetComponentInChildren<InputField>().interactable = true;
            child.GetComponent<Image>().color = white;
        }
        OnStopEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
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
        if (po != null)
        {
            EmptyInspectorParams();
            tempSimObjectParams.Clear();
            emptyScrollTextInspector.gameObject.SetActive(false);
            LoadInspectorInfo(po.SimObject.Type, po.SimObject.Class_name, po.SimObject.Id);
            LoadInspectorParams(SceneController.GetSimObjectParamsPrototype(po.SimObject.Type, po.SimObject.Class_name), po, SimulationController.admin);
        }
    }
    public void EmptyInspectorParams()
    {
        foreach (Transform child in inspectorContent.transform) GameObject.Destroy(child.gameObject);
        inspectorContent.transform.DetachChildren();
    }

    public void LoadInspectorInfo(SimObject.SimObjectType type, string class_name, int id)
    {
        inspectorType.text = type.ToString();
        inspectorClass.text = class_name;
        inspectorId.text = id.ToString();
    }
    public void LoadInspectorParams(JSONArray parameters, PlaceableObject po, bool admin)
    {
        GameObject param;

        foreach (JSONObject p in parameters)
        {
            if (p.HasKey("editable_in_play") && p["editable_in_play"].Equals(true) || !p.HasKey("editable_in_play"))
            {
                switch ((string)p["type"])
                {
                    case "System.Single":
                        param = Instantiate(inspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnInspectorParamUpdate(p["name"], float.Parse(value)));
                        if (!admin)
                        {
                            param.GetComponentInChildren<InputField>().interactable = false;
                            param.GetComponent<Image>().color = black;
                        }
                        break;
                    case "System.Int32":
                        param = Instantiate(inspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnInspectorParamUpdate(p["name"], int.Parse(value)));
                        if (!admin)
                        {
                            param.GetComponentInChildren<InputField>().interactable = false;
                            param.GetComponent<Image>().color = black;
                        }
                        break;
                    case "System.Boolean":
                        param = Instantiate(inspectorTogglePrefab);
                        param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => OnInspectorParamUpdate(p["name"], value));
                        param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                        param.GetComponentInChildren<Toggle>().isOn = (bool) po.SimObject.Parameters[p["name"]];
                        if (!admin)
                        {
                            param.GetComponentInChildren<Toggle>().interactable = false;
                            param.GetComponent<Image>().color = black;
                        }
                        break;
                    case "System.String":
                        param = Instantiate(inspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnInspectorParamUpdate(p["name"], value));
                        if (!admin) param.GetComponentInChildren<InputField>().interactable = false;
                        if (!admin)
                        {
                            param.GetComponentInChildren<InputField>().interactable = false;
                            param.GetComponent<Image>().color = black;
                        }
                        break;
                    case "System.Position":
                        param = Instantiate(inspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnInspectorParamUpdate(p["name"], value));
                        if (!admin) param.GetComponentInChildren<InputField>().interactable = false;
                        if (!admin)
                        {
                            param.GetComponentInChildren<InputField>().interactable = false;
                            param.GetComponent<Image>().color = black;
                        }
                        break;
                    case "System.Cells":
                        // multiple elements prefab
                        continue;
                    default:
                        continue;
                }
                param.transform.SetParent(inspectorContent.transform);

                if (!((string)p["type"]).Equals("System.Boolean"))
                {
                    param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
                    param.GetComponentInChildren<InputField>().characterLimit = 20;
                    param.transform.Find("Param Name").GetComponent<Text>().text = p["name"];
                    param.transform.Find("InputField").GetComponent<InputField>().text = "" + po.SimObject.Parameters[p["name"]];
                }
            }
        }
        if (inspectorContent.transform.childCount == 0)
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
                        param = Instantiate(simParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.DecimalNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], float.Parse(value.Replace('.', ','))));
                        break;
                    case "System.Int32":
                        param = Instantiate(simParamPrefab);
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
                        param = Instantiate(simParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], value));
                        break;
                    default:
                        return;
                }
                param.transform.SetParent(scrollContent.transform);
                if (!((string)p["type"]).Equals("System.Boolean"))
                {
                    //param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
                    //param.GetComponentInChildren<InputField>().characterLimit = 20;
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
                case "System.Int32":
                    param = Instantiate(simParamPrefab);
                    param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                    param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(d["name"], int.Parse(value)));
                    param.transform.GetComponentInChildren<InputField>().interactable = false;
                    param.GetComponent<Image>().color = black;
                    break;
                default:
                    return;
            }
            param.transform.SetParent(scrollContent.transform);

            param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
            param.GetComponentInChildren<InputField>().characterLimit = 20;
            param.transform.Find("Param Name").GetComponent<Text>().text = (d["name"] == "x") ? "Width" : (d["name"] == "z") ? "Length" : "Height";
            param.transform.Find("InputField").GetComponent<InputField>().text = d["default"];
        }
    }
    public void LoadAgentAmounts(GameObject scrollContent, JSONArray agentPrototypes)
    {
        foreach (JSONObject p in agentPrototypes)
        {
            GameObject param;
            param = Instantiate(simParamPrefab);
            param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
            param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["class"] + "s amount", int.Parse(value)));

            param.transform.SetParent(scrollContent.transform);

            param.GetComponentInChildren<InputField>().lineType = InputField.LineType.SingleLine;
            param.GetComponentInChildren<InputField>().characterLimit = 20;
            param.transform.Find("Param Name").GetComponent<Text>().text = p["class"] + "s amount";
            param.transform.Find("InputField").GetComponent<InputField>().text = p["default"];
            param.transform.GetComponentInChildren<InputField>().interactable = false;
            param.GetComponent<Image>().color = black;
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
        UpdateSimParams(last_simUpdate);
        UpdateAgentAmounts(last_StepApplied);
        tempSimParams.Clear();
    }
    public void OnInspectorParamUpdate(string param_name, dynamic value)
    {
        if (!tempSimObjectParams.ContainsKey(param_name)) tempSimObjectParams.Add(param_name, value);
        else tempSimObjectParams[param_name] = value;
    }
    public void OnInspectorParamsApply()/// SISTEMARE
    {
        SimObjectModifyEventArgs e = new SimObjectModifyEventArgs();
        e.type = SceneController.selectedPlaced.SimObject.Type;
        e.class_name = SceneController.selectedPlaced.SimObject.Class_name;
        e.id = SceneController.selectedPlaced.SimObject.Id;
        e.parameters = tempSimObjectParams;
        OnSimObjectParamsUpdateEventHandler?.BeginInvoke(this, e, null, null);
    }
    public void OnInspectorParamsDiscard()
    {
        UpdateInspectorParams();
        tempSimObjectParams.Clear();
    }

    public void ShowHideEnv_SimSpace()
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

    }
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
        camera.GetComponent<DisplayStats>().enabled = !showInfoPanel;
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
        if(selected != null)
            followToggle.GetComponent<Toggle>().interactable = !showInspectorPanel;
        showInspectorPanel = !showInspectorPanel;
    }
    public void MuteUnmuteAudio()
    {
        if (!backgroundMusic.mute || !effectsAudio.mute)
        {
            muteUnmuteButton.GetComponent<Image>().sprite = muteUnmuteSprites[0];
            backgroundMusic.mute = true;
            effectsAudio.mute = true;
        }
        else if (backgroundMusic.mute && effectsAudio.mute)
        {
            muteUnmuteButton.GetComponent<Image>().sprite = muteUnmuteSprites[1];
            backgroundMusic.volume = 0.3f;
            backgroundMusic.mute = false;
            effectsAudio.volume = 0.2f;
            effectsAudio.mute = false;
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
        OnExitEventHandler.Invoke(this, new EventArgs());
        SceneManager.LoadScene("MenuScene");
    }
}
