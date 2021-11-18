using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using static SceneController;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

public class SpeedChangeEventArgs : EventArgs
{
    public int Speed { get; set; }
}

public class UIController : MonoBehaviour
{
    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;

    // UI Events
    public static event EventHandler<EventArgs> OnLoadMainSceneEventHandler;
    public static event EventHandler<EventArgs> OnPlayEventHandler;
    public static event EventHandler<EventArgs> OnPauseEventHandler;
    public static event EventHandler<EventArgs> OnStopEventHandler;
    public static event EventHandler<SpeedChangeEventArgs> OnSpeedChangeEventHandler;
    public static event EventHandler<SimParamsUpdateEventArgs> OnSimParamsUpdateEventHandler; 

    // Controllers
    private SceneController SceneController;

    // Sprites Collection
    public List<NamedPrefab> AgentsData;
    public List<NamedPrefab> GenericsData;
    public List<NamedPrefab> ObstaclesData;

    // Variables
    public TMP_Text nickname;
    public bool showEditPanel = false, showSettingsPanel = false, showInfoPanel = false, showQuitPanel = false, showInspectorPanel = false;
    public GameObject panelSimButtons, panelEditMode, panelInspector, panelSimParams, panelBackToMenu, panelFPS, 
        InspectorParamPrefab, InspectorTogglePrefab, InspectorContent, SimParamPrefab, SimTogglePrefab, SimParamsContent,
        simToggle, envToggle, contentAgents, contentGenerics, contentObstacles, editPanelSimObject_prefab;
    public Slider slider;
    public Image imgEditMode, imgSimState, imgContour;
    public Button buttonEdit;
    public Sprite[] commandSprites;
    public Text inspectorType, inspectorClass, inspectorId;
    public static bool showSimSpace, showEnvironment;
    public Dictionary<string, object> tempSimParams = new Dictionary<string, object>();
    public Dictionary<string, object> tempSimObjectParams = new Dictionary<string, object>();

    private void Awake()
    {
        nickname.text = playerPreferencesSO.nickname;
        showSimSpace = playerPreferencesSO.showSimSpace;
        showEnvironment = playerPreferencesSO.showEnvironment;

        // Bind Controllers
        SceneController = GameObject.Find("SceneController").GetComponent<SceneController>();

        Debug.Log("sim space: " + showSimSpace + " env: " + showEnvironment);

        simToggle.GetComponent<Toggle>().isOn = showSimSpace;
        envToggle.GetComponent<Toggle>().isOn = showEnvironment;
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
        OnLoadMainSceneEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
        AgentsData = SceneController.PO_Prefab_Collection[simId].PO_AgentPrefabs;
        GenericsData = SceneController.PO_Prefab_Collection[simId].PO_GenericPrefabs;
        ObstaclesData = SceneController.PO_Prefab_Collection[simId].PO_ObstaclePrefabs;

        LoadEditPanel();
        LoadSimParams(SimParamsContent, (JSONArray)SimulationController.sim_list_editable[SimulationController.sim_id]["sim_params"]);
    }
    /// <summary>
    /// Update routine (Unity Process)
    /// </summary>
    void Update()
    {
        if (simToggle.GetComponent<Toggle>().isOn == true)
            showSimSpace = true;
        else showSimSpace = false;

        if (envToggle.GetComponent<Toggle>().isOn == true)
            showEnvironment = true;
        else showEnvironment = false;

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

    public void LoadEditPanel()
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
        LoadInspectorInfo(po.type, po.class_name, po.id);
        LoadInspectorParams(InspectorContent, SceneController.GetSimObjectParamsPrototype(po.type, po.class_name));
    }
    public void LoadInspectorInfo(SimObject.SimObjectType type, string class_name, int id)
    {
        inspectorType.text = type.ToString();
        inspectorClass.text = class_name;
        inspectorId.text = id.ToString();
    }
    public void LoadInspectorParams(GameObject scrollContent, JSONArray parameters)
    {
        foreach (JSONObject p in parameters)
        {
            if (p.HasKey("editable_in_play") && p["editable_in_play"].Equals(true) || !p.HasKey("editable_in_play"))
            {
                GameObject param;
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
                        break;
                    case "System.String":
                        param = Instantiate(InspectorParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimObjectParamUpdate(p["name"], value));
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
            //if no params
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
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], float.Parse(value)));
                        break;
                    case "System.Int32":
                        param = Instantiate(SimParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.IntegerNumber;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], int.Parse(value)));
                        break;
                    case "System.Boolean":
                        param = Instantiate(SimTogglePrefab);
                        param.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) => OnSimParamUpdate(p["name"], value));
                        break;
                    case "System.String":
                        param = Instantiate(SimParamPrefab);
                        param.GetComponentInChildren<InputField>().contentType = InputField.ContentType.Alphanumeric;
                        param.GetComponentInChildren<InputField>().onEndEdit.AddListener((value) => OnSimParamUpdate(p["name"], value));
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
            //if not params
        }
    }
    
    public void OnSimParamUpdate(string param_name, dynamic value)
    {
        tempSimParams.Add(param_name, value);
    }
    public void OnSimObjectParamUpdate(string param_name, dynamic value)
    {
        tempSimObjectParams.Add(param_name, value);
    }

    public void OnSimParamsApply()
    {
        SimParamsUpdateEventArgs e = new SimParamsUpdateEventArgs();
        e.parameters = tempSimParams;
        OnSimParamsUpdateEventHandler?.BeginInvoke(this, e, null, null); 
    }
    public void OnSimObjectParamsApply()
    {
        SimObjectModifyEventArgs e = new SimObjectModifyEventArgs();
        e.type = SceneController.selectedSimObject.type;
        e.class_name = SceneController.selectedSimObject.class_name;
        e.id = SceneController.selectedSimObject.id;
        e.parameters = tempSimObjectParams;
    }
    public void OnSimParamsDiscard()
    {
        tempSimParams.Clear();
    }
    public void OnSimObjectParamsDiscard()
    {
        tempSimObjectParams.Clear();
    }

    public void ShowHideEditPanel()
    {
            panelSimButtons.gameObject.SetActive(showEditPanel);
            panelEditMode.gameObject.SetActive(!showEditPanel);
            imgEditMode.gameObject.SetActive(!showEditPanel);
            imgContour.gameObject.SetActive(!showEditPanel);
            showEditPanel = !showEditPanel;
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

    public void StoreDataPreferences(string nameString, bool toggleSimSpace, bool toggleEnvironment)
    {
        playerPreferencesSO.nickname = nameString;
        playerPreferencesSO.showSimSpace = toggleSimSpace;
        playerPreferencesSO.showEnvironment = toggleEnvironment;

    }
    public void BackToMenu()    //distruggi settaggi prima di uscire
    {
        StoreDataPreferences(nickname.text, showSimSpace, showEnvironment);
        //foreach (Transform child in settingsScrollContent.transform)
        //    GameObject.Destroy(child.gameObject);
        //foreach (Transform child in paramsScrollContent.transform)
        //    GameObject.Destroy(child.gameObject);

        SceneManager.LoadScene("MenuScene");
    }
}
