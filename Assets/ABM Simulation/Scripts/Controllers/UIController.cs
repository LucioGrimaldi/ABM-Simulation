using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;

public class UIController : MonoBehaviour
{
    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;

    // UI Events
    public static event EventHandler<EventArgs> OnLoadMainSceneEventHandler;
    public static event EventHandler<EventArgs> OnPlayEventHandler;
    public static event EventHandler<EventArgs> OnPauseEventHandler;
    public static event EventHandler<EventArgs> OnStopEventHandler;

    // Controllers
    private SceneController SceneController;

    // Sprites Collection
    public SOCollection SimObjectsData;

    // Variables
    public TMP_Text nickname;
    private int counter1 = 0, counter2 = 1, counter3 = 1, counter4 = 1, counter5 = 1;
    public GameObject panelSimButtons, panelEditMode, panelInspector, panelSettings, panelBackToMenu, panelFPS, 
        scrollParamsPrefab, paramsScrollContent,scrollSettingsGamePrefab, settingsScrollContent,
        simToggle, envToggle, contentAgents, contentGenerics, contentObstacles, editPanelSimObject_prefab;
    public Slider slider;
    public Image imgEditMode, imgSimState, imgContour;
    public Button buttonEdit;
    public Sprite[] commandSprites;
    public static bool showSimSpace, showEnvironment;
    

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
        LoadParamsSettings(10);
        LoadGameSettings(10);
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
        if (slider.value == 0)
        {
            //SimulationController.ChangeSimulationSpeed(0);
            Debug.Log("Slider set to 0");
        }
        else if (slider.value == 1)
        {
            //SimulationController.ChangeSimulationSpeed(1);
            Debug.Log("Slider set to 1");
        }
        else if (slider.value == 2)
        {
            //SimulationController.ChangeSimulationSpeed(2);
            Debug.Log("Slider set to 2");
        }
        else if (slider.value == 3)
        {
            //SimulationController.ChangeSimulationSpeed(3);
            Debug.Log("Slider set to 3");
        }
        else if (slider.value == 4)
        {
            //SimulationController.ChangeSimulationSpeed(4);
            Debug.Log("Slider set to 4");
        }

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

    public void ApplySettings()
    {
        //INVIA SETTAGGI A MASON
    }
    public void ApplyParameters()
    {
        //INVIA PARAMETRI A MASON
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
    public void StoreDataPreferences(string nameString, bool toggleSimSpace, bool toggleEnvironment)
    {
        playerPreferencesSO.nickname = nameString;
        playerPreferencesSO.showSimSpace = toggleSimSpace;
        playerPreferencesSO.showEnvironment = toggleEnvironment;

    }

    void LoadEditPanel()
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
    void LoadParamsSettings(int parameters)
    {

        for (int i = 0; i < parameters; i++)
        {
            GameObject paramsSettings = Instantiate(scrollParamsPrefab);
            paramsSettings.transform.SetParent(paramsScrollContent.transform);
        }
    }
    void LoadGameSettings(int settings)
    {
        //int[] settingsArray = new int[3];

        for (int i = 0; i < settings; i++)
        {
            string text = "Settaggio";
            double value = 0.1;

            //foreach(int setting in settingsArray)
            //{
            //    //istanzia oggetto e settalo
            //}

            GameObject simSettingsGame = Instantiate(scrollSettingsGamePrefab);
            simSettingsGame.transform.SetParent(settingsScrollContent.transform);

            simSettingsGame.transform.GetComponentInChildren<Text>().text = text;
            simSettingsGame.transform.GetComponentInChildren<InputField>().text = value.ToString();
            //simSettingsGame.transform.GetChild(0).gameObject.GetComponent<Text>().text = text;
            //simSettingsGame.transform.GetChild(1).gameObject.GetComponent<InputField>().text = value.ToString();
            //Debug.Log("Testo: " + text + " Valore: " + value);

            //Debug.Log("Debug: " + simSettingsGame.GetComponentInChildren<Transform>().gameObject.GetComponent<Text>());
        }
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
            panelSettings.gameObject.SetActive(false);
        else
            panelSettings.gameObject.SetActive(true);

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
}
