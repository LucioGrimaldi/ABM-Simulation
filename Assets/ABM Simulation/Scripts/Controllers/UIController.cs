using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using GerardoUtils;
using System;

public class UIController : MonoBehaviour
{

    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;


    // UI Events
    public static event EventHandler<EventArgs> OnLoadMainSceneEventHandler;

    // Variables
    public TMP_Text nickname;
    private int counter1 = 0, counter2 = 1, counter3 = 1, counter4 = 1, counter5 = 1;
    public GameObject panelSimButtons, panelEditMode, panelInspector, panelSettings, panelBackToMenu, panelFPS, 
                scrollParamsPrefab, paramsScrollContent,scrollSettingsGamePrefab, settingsScrollContent, simToggle, envToggle;
    public Slider slider;
    public Image imgEditMode, imgSimState, imgContour;
    public Button buttonEdit;
    public Sprite[] spriteArray; //0 pause, 1 play, 2 stop
    public static bool showSimSpace, showEnvironment;
    

    private void Awake()
    {
        nickname.text = playerPreferencesSO.nickname;
        showSimSpace = playerPreferencesSO.showSimSpace;
        showEnvironment = playerPreferencesSO.showEnvironment;

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
    void Start()
    {
        OnLoadMainSceneEventHandler?.BeginInvoke(this, EventArgs.Empty, null, null);
        LoadParamsSettings(10);
        LoadGameSettings(10);
    }
    void Update()
    {
        if (simToggle.GetComponent<Toggle>().isOn == true)
            showSimSpace = true;
        else showSimSpace = false;

        if (envToggle.GetComponent<Toggle>().isOn == true)
            showEnvironment = true;
        else showEnvironment = false;

        //Debug.Log("Grid toggle: " + gridToggle.GetComponent<Toggle>().isOn);

        //if (Input.GetMouseButtonDown(0))
        //{
        //    grid.SetValue(UtilsClass.GetMouseWorldPosition(), 50);
        //}
        //if (Input.GetMouseButtonDown(1))
        //{
        //    Debug.Log(grid.GetValue(UtilsClass.GetMouseWorldPosition()));
        //}
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
        imgSimState.GetComponent<Image>().sprite = spriteArray[1];
        buttonEdit.interactable = false;
        Debug.Log("PLAY SIM");
    }
    public void PauseSimulation()
    {
        imgSimState.GetComponent<Image>().color = Color.yellow;
        imgSimState.GetComponent<Image>().sprite = spriteArray[0];
        buttonEdit.interactable = true;
        Debug.Log("PAUSE SIM");
    }
    public void StopSimulation()
    {
        imgSimState.GetComponent<Image>().color = Color.red;
        imgSimState.GetComponent<Image>().sprite = spriteArray[2];
        buttonEdit.interactable = true;
        Debug.Log("STOP SIM");
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
