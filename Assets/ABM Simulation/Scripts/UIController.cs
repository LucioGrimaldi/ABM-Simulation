using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    //GENERAL VARIABLES
    string playernick;


    //CANVAS MENU
    public Canvas canvasMenu;
    Button buttonPlayGame;
    Button buttonSettings;
    Button buttonQuitGame;
    Text textField;
    InputField inputField;

    //CANVAS SETTINGS
    public Canvas canvasSettings;
    Button applyButton;
    Button undoChanges;


    //CANVAS GAME
    public Canvas canvasGame;
    Button buttonPreviousCam;
    Button buttonNextCam;
    Button buttonOptions;
    Text playerText;
    Text cameraText;
    Button playSimulation;
    Button pauseSimulation;
    Button stopSimulation;
    
    

    void Start()
    {

        //setMainMenu();
        
    }


    void Update()
    {
        //CheckNickname();
        
    }

    
    void setMainMenu()
    {
        textField = GameObject.Find("TextInsertNick").GetComponent<Text>();
        inputField = GameObject.Find("InputField").GetComponent<InputField>();
        buttonPlayGame = GameObject.Find("ButtonPlayGame").GetComponent<Button>();
        buttonSettings = GameObject.Find("ButtonSettings").GetComponent<Button>();
        buttonQuitGame = GameObject.Find("ButtonQuitGame").GetComponent<Button>();

        buttonPlayGame.onClick.AddListener(StartGame);
        buttonSettings.onClick.AddListener(MenuSettings);
        buttonQuitGame.onClick.AddListener(QuitGame);

        canvasGame.gameObject.SetActive(false);
        canvasSettings.gameObject.SetActive(false);
        
        buttonPlayGame.interactable = false;
        

    }



    void StartGame()
    {
        canvasSettings.gameObject.SetActive(false);
        canvasMenu.gameObject.SetActive(false);
        canvasGame.gameObject.SetActive(true);

        Debug.Log("Game Started!");

        playerText = GameObject.Find("PlayerText").GetComponent<Text>();
        cameraText = GameObject.Find("CamText").GetComponent<Text>();
        buttonOptions = GameObject.Find("ButtonOptions").GetComponent<Button>();
        buttonPreviousCam = GameObject.Find("previous_Cam").GetComponent<Button>();
        buttonNextCam = GameObject.Find("next_Cam").GetComponent<Button>();
        playSimulation = GameObject.Find("PlaySimulation").GetComponent<Button>();
        pauseSimulation = GameObject.Find("PauseSimulation").GetComponent<Button>();
        stopSimulation = GameObject.Find("StopSimulation").GetComponent<Button>();

        
        playerText.text = "Player: " + inputField.text;
        cameraText.text = "Main Camera";
        

        buttonOptions.onClick.AddListener(inGameSettings);
        

    }

    void inGameSettings()
    {
        
        canvasGame.gameObject.SetActive(false);
        canvasSettings.gameObject.SetActive(true);
        
        applyButton.onClick.AddListener(ApplySettings);
        undoChanges.onClick.AddListener(StartGame);
    }

    void ApplySettings()
    {
        Debug.Log("Settings Applied!");
        applyButton.gameObject.SetActive(false);
        canvasSettings.gameObject.SetActive(false);
        canvasMenu.gameObject.SetActive(true);
        canvasSettings.gameObject.SetActive(false);
        //save new settings into variables and send to simulation
    }


    void CheckNickname()
    {
        if (inputField.text.Length > 0 && !inputField.text.Contains(" "))
        {

            buttonPlayGame.interactable = true;
            playernick = inputField.text;
        }
        else buttonPlayGame.interactable = false;

    }

    void MenuSettings()
    {
        Debug.Log("Settings Menu!");
        canvasMenu.gameObject.SetActive(false);
        canvasSettings.gameObject.SetActive(true);

        undoChanges = GameObject.Find("UndoChanges").GetComponent<Button>();
        applyButton = GameObject.Find("ApplyChanges").GetComponent<Button>();
        //show all settings to modify

        undoChanges.onClick.AddListener(StartGame);
        applyButton.onClick.AddListener(ApplySettings);
    }


    void QuitGame()
    {
        Application.Quit();
        Debug.Log("Exiting Game!");
    }
}
