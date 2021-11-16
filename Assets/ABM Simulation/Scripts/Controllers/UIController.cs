using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
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
    public SOCollection SimObjectsData;

    // Variables
    public TMP_Text nickname;
    private int counter1 = 0, counter2 = 1, counter3 = 1, counter4 = 1, counter5 = 1;
    public GameObject panelSimButtons, panelEditMode, panelInspector, panelSimParams, panelBackToMenu, panelFPS, 
        InspectorParamPrefab, InspectorTogglePrefab, InspectorContent, SimParamPrefab, SimTogglePrefab, SimParamsContent,
        simToggle, envToggle, contentAgents, contentGenerics, contentObstacles, editPanelSimObject_prefab;
    public Slider slider;
    public Image imgEditMode, imgSimState, imgContour;
    public Button buttonEdit;
    public Sprite[] commandSprites;
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
        SimObjectsData = SceneController.SimObjectsData;

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
        foreach (SimObjectSO so in SimObjectsData.agents)
        {
            Sprite s = so.sprite;
            GameObject o = Instantiate(editPanelSimObject_prefab, contentAgents.transform);
            o.GetComponent<Image>().sprite = s;
            o.GetComponent<Button>().onClick.AddListener(() =>
            {
                SceneController.SelectPlaceableSimObject(0, o.transform.GetSiblingIndex());
            });
        }
        foreach (SimObjectSO so in SimObjectsData.generics)
        {
            Sprite s = so.sprite;
            GameObject o = Instantiate(editPanelSimObject_prefab, contentGenerics.transform);
            o.GetComponent<Image>().sprite = s;
            o.GetComponent<Button>().onClick.AddListener(() =>
            {
                SceneController.SelectPlaceableSimObject(1, o.transform.GetSiblingIndex());
            });
        }
        foreach (SimObjectSO so in SimObjectsData.obstacles)
        {
            Sprite s = so.sprite;
            GameObject o = Instantiate(editPanelSimObject_prefab, contentObstacles.transform);
            o.GetComponent<Image>().sprite = s;
            o.GetComponent<Button>().onClick.AddListener(() =>
            {
                SceneController.SelectPlaceableSimObject(2, o.transform.GetSiblingIndex());
            });
        }
    }
    public void LoadInspectorInfo()
    {
        // load type, class_name, id
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

    public void ShowHidePanelSim()
    {
        counter1++;
        if (counter1 % 2 == 1)
        {
            panelSimButtons.gameObject.SetActive(false);
            panelEditMode.gameObject.SetActive(true);
            imgEditMode.gameObject.SetActive(true);
            imgContour.gameObject.SetActive(true);
        }
        else
        {
            imgContour.gameObject.SetActive(false);
            imgEditMode.gameObject.SetActive(false);
            panelEditMode.gameObject.SetActive(false);
            panelSimButtons.gameObject.SetActive(true);
        }

    }
    public void ShowHidePanelSettings()
    {
        counter2++;
        if (counter2 % 2 == 1)
            panelSimParams.gameObject.SetActive(false);
        else
            panelSimParams.gameObject.SetActive(true);

    }
    public void ShowHideInfoPanel()
    {
        counter3++;
        if (counter3 % 2 == 1)
            panelFPS.gameObject.SetActive(false);
        else
            panelFPS.gameObject.SetActive(true);

    }
    public void ShowHidePanelQuit()
    {
        counter4++;
        if (counter4 % 2 == 1)
            panelBackToMenu.gameObject.SetActive(false);
        else
            panelBackToMenu.gameObject.SetActive(true);

    }
    public void ShowHidePanelInspector()
    {
        counter5++;
        if (counter5 % 2 == 1)
            panelInspector.gameObject.SetActive(false);
        else
            panelInspector.gameObject.SetActive(true);

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
