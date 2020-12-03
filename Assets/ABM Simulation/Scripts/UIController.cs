using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    //Simulation Controller
    private SimulationController SimulationController;

    //GENERAL VARIABLES
    //WORLD VARIABLES
    string playernick, nameCamera = "Main Camera";
    bool listenPlayGame, listenSettings, listenQuitGame, listenApplyM, listenDiscardM, listenInGameSettings,
        listenApplyG, listenDiscardG, listenButtonBackToMenu, listenPrevCam, listenNextCam = false;

    //CANVAS MENU
    public Canvas canvasMenu;
    private Button buttonPlayGame;
    private Button buttonSettings;
    private Button buttonQuitGame;
    private Text textField;
    private InputField inputField;

    //CANVAS SETTINGS
    public Canvas canvasSettings;
    private Button applyButton;
    private Button undoChanges;

    //CANVAS GAME
    public Canvas canvasGame;
    private Button buttonPreviousCam;
    private Button buttonNextCam;
    private Button buttonOptions;
    private Text playerText;
    private Text cameraText;
    public Button playSimulation;
    public Button pauseSimulation;
    public Button stopSimulation;
    
    void Awake()
    {
    }

    //CANVAS SETTINGS INGAME MENU
    public Canvas canvasSettingsInGameMenu;
    Button button_applyGame;
    Button button_xDiscardGame;


    void Start()
    {
        SimulationController = GameObject.Find("Simulation Controller").GetComponent<SimulationController>();
        playSimulation.onClick.AddListener(SimulationController.Play);
        pauseSimulation.onClick.AddListener(SimulationController.Pause);
        stopSimulation.onClick.AddListener(SimulationController.Stop);
        //setMainMenu();

        SetMainMenu();

        
    }


    void Update()
    {
        CheckNickname();
        
    }
    
    //SETTING MENUS-------------------------

    public void SetMainMenu()
    {
        canvasSettingsMenu.gameObject.SetActive(false);
        canvasGame.gameObject.SetActive(false);
        canvasSettingsInGameMenu.gameObject.SetActive(false);
        canvasMenu.gameObject.SetActive(true);

        textNick = GameObject.Find("TextNick").GetComponent<Text>();
        inputFieldNick = GameObject.Find("InputFieldNick").GetComponent<InputField>();
        buttonPlayGame = GameObject.Find("ButtonPlayGame").GetComponent<Button>();
        buttonSettings = GameObject.Find("ButtonSettings").GetComponent<Button>();
        buttonQuitGame = GameObject.Find("ButtonQuitGame").GetComponent<Button>();

        if(listenPlayGame == false)
        {
            buttonPlayGame.onClick.AddListener(StartGame);
            Debug.Log("PLAY game added listener");
            listenPlayGame = true;
        }
        if (listenSettings == false)
        {
            buttonSettings.onClick.AddListener(SetSettingsMenu);
            Debug.Log("SETTINGS game added listener");
            listenSettings = true;
        }
        if (listenQuitGame == false)
        {
            buttonQuitGame.onClick.AddListener(QuitGame);
            Debug.Log("QUIT game added listener");
            listenQuitGame = true;
        }


    }


    void StartGame()
    {
        canvasSettingsMenu.gameObject.SetActive(false);
        canvasSettingsInGameMenu.gameObject.SetActive(false);
        canvasMenu.gameObject.SetActive(false);
        canvasGame.gameObject.SetActive(true);

        Debug.Log("Game Started!");

        textCamera = GameObject.Find("TextCamera").GetComponent<Text>();
        textPlayer = GameObject.Find("TextPlayer").GetComponent<Text>();

        buttonBackToMenu = GameObject.Find("Button_menu").GetComponent<Button>();
        buttonInGameSettings = GameObject.Find("Button_settings").GetComponent<Button>();
        buttonPreviousCam = GameObject.Find("Button_arrow_leftCam").GetComponent<Button>();
        buttonNextCam = GameObject.Find("Button_arrow_rightCam").GetComponent<Button>();
        //playSimulation = GameObject.Find("Button_play").GetComponent<Button>();
        //pauseSimulation = GameObject.Find("Button_pause").GetComponent<Button>();
        //stopSimulation = GameObject.Find("Button_xStop").GetComponent<Button>();

        
        textCamera.text = nameCamera;
        textPlayer.text = playernick;

        if (listenPrevCam == false)
        {
            buttonPreviousCam.onClick.AddListener(ChangeCameraLeft);
            listenPrevCam = true;
        }

        if (listenNextCam == false)
        {
            buttonNextCam.onClick.AddListener(ChangeCameraRight);
            listenNextCam = true;
        }

        if (listenButtonBackToMenu == false)
        {
            buttonBackToMenu.onClick.AddListener(SetMainMenu);
            listenButtonBackToMenu = true;
        }

        if (listenInGameSettings == false)
        {
            buttonInGameSettings.onClick.AddListener(InGameSettings);
            listenInGameSettings = true;
        }
        
    }

    void SetSettingsMenu()
    {
        Debug.Log("Settings Menu!");
        canvasMenu.gameObject.SetActive(false);
        canvasSettingsMenu.gameObject.SetActive(true);

        button_applyMenu = GameObject.Find("Button_applyMenu").GetComponent<Button>();
        button_xDiscardMenu = GameObject.Find("Button_xDiscardMenu").GetComponent<Button>();

        //get and show all settings to modify TODO--------------------------


        if (listenApplyM == false)
        {
            button_applyMenu.onClick.AddListener(ApplyMenuSettings);
            listenApplyM = true;

        }

        if (listenDiscardM == false)
        {
            button_xDiscardMenu.onClick.AddListener(SetMainMenu);
            listenDiscardM = true;

        }

    }

    void InGameSettings()
    {
        canvasGame.gameObject.SetActive(false);
        canvasSettingsInGameMenu.gameObject.SetActive(true);
        
        button_applyGame = GameObject.Find("Button_applyGame").GetComponent<Button>();
        button_xDiscardGame = GameObject.Find("Button_xDiscardGame").GetComponent<Button>();

        if (listenApplyG == false)
        {
            button_applyGame.onClick.AddListener(ApplyInGameSettings);
            listenApplyG = true;
        }

        if (listenDiscardG == false)
        {
            button_xDiscardGame.onClick.AddListener(StartGame);
            listenDiscardG = true;
        }
    }

    //OTHER USEFUL METHODS

    void ChangeCameraLeft()
    {

    }

    void ChangeCameraRight()
    {

    }


    void ApplyInGameSettings()
    {

        //save new settings into variables and send to simulation TODO------


        Debug.Log("Settings InGame Applied!");
        StartGame();
    }


    void ApplyMenuSettings()
    {

        //save new settings into variables and send to simulation TODO------


        Debug.Log("Settings Applied!");
        SetMainMenu();
    }


    void CheckNickname()
    {
        if (inputFieldNick.text.Length > 0 && !inputFieldNick.text.Contains(" "))
        {
            buttonPlayGame.interactable = true;
            playernick = inputFieldNick.text;
        }
        else buttonPlayGame.interactable = false;

    }
    

    void QuitGame()
    {
        Application.Quit();
        Debug.Log("Exiting Game!");
    }
    
}

