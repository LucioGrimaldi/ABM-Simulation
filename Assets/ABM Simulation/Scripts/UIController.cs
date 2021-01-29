using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    //Simulation Controller Variables
    private SimulationController SimulationController;
    private float framerate = 60f;


    //WORLD VARIABLES
    public Camera freeCam, cam1, cam2, cam3, cam4, cam5;
    public Slider slider;
    private string playernick, nameCamera = "Free Camera";
    private bool listenPlayGame, listenSettings, listenQuitGame, listenApplyM, listenDiscardM, listenInGameSettings, listenApplyG, listenDiscardG,
        listenButtonBackToMenu, listenPrevCam, listenNextCam, setFreeCam, setBindings, startGameCam, setNick = false; //settingsAnim, resetAnim = false; animSpeed = 600.0f
    private float speed = 18.0f, camSpeed = 20.0f, camRotation = 14.0f;
    private InputField cohesionIF, avoidanceIF, avoidDistanceIF, randomnessIF, consistencyIF, momentumIF, neighborhoodIF,
        deadAgentProbIF, jumpIF, widthIF, heightIF, lenghtIF, numAgentsIF, simStepRateIF, simStepDelayIF, framerateIF;
    

    //CANVAS MENU
    public Canvas canvasMenu;
    private Button buttonPlayGame;
    private Button buttonSettings;
    private Button buttonQuitGame;
    private Text textNick;
    private InputField inputFieldNick;


    //CANVAS SETTINGS
    public Canvas canvasSettingsMenu;
    private Button button_applyMenu;
    private Button button_xDiscardMenu;
    

    //CANVAS SETTINGS INGAME MENU
    public Canvas canvasSettingsInGameMenu;
    private Image backgroundSettingsInGame;
    private Button button_applyGame;
    private Button button_xDiscardGame;
    private Vector3 bgStartAnim = new Vector3(170, 275, 0); //new Vector3(1255, 275, 0);
    private Vector3 bgFinishAnim = new Vector3(1050, 275, 0); //new Vector3(958, 275, 0);


    //CANVAS GAME
    public Canvas canvasGame;
    public Button buttonInGameSettings;
    public Button playSimulation;
    public Button pauseSimulation;
    public Button stopSimulation;
    public Text text025x;
    public Text text05x;
    public Text text1x;
    public Text text2x;
    public Text textMax;
    private Button buttonPreviousCam;
    private Button buttonNextCam;
    private Button buttonBackToMenu;
    private Text textPlayer;
    private Text textCamera;
    private Vector3 endMarkerP = new Vector3(5, 25, -130);
    private Quaternion endMarkerR = Quaternion.Euler(0, 0, 0);


    void Awake()
    {
    }
    

    void Start()
    {
        SimulationController = GameObject.Find("Simulation Controller").GetComponent<SimulationController>();
        playSimulation.onClick.AddListener(SimulationController.Play);
        pauseSimulation.onClick.AddListener(SimulationController.Pause);
        stopSimulation.onClick.AddListener(SimulationController.Stop);

        framerate = 60f;

        QualitySettings.vSyncCount = 0;

        slider.value = 2.0f;
        slider.onValueChanged.AddListener(delegate { MoveSlider(); });

        //Invio settaggi iniziali di simulazione a Mason ---------TODO

        freeCam.transform.SetPositionAndRotation(new Vector3(17, 16, -68), Quaternion.Euler(new Vector3(25, -37, 0)));
        
        SetMainCamera();
        SetMainMenu();
        
    }


    void Update()
    {
        if(setFreeCam == true)
            SetCamStartPosition();

        if(setNick == true)
            CheckNickname();

        if(setBindings == true) //se sono su freecamera la uso, altrimenti no
            MoveFreeCamera();

        SetFramerate();

        //if (settingsAnim == true)
        //SettingsAnimation();

        //if (resetAnim == true)
        //ResetAnimation();

    }

    //SETTING MAIN MENU-------------------------

    public void SetMainMenu()
    {
        text025x.gameObject.SetActive(false);
        text05x.gameObject.SetActive(false);
        text1x.gameObject.SetActive(false);
        text2x.gameObject.SetActive(false);
        textMax.gameObject.SetActive(false);
        canvasSettingsMenu.gameObject.SetActive(false);
        canvasGame.gameObject.SetActive(false);
        canvasSettingsInGameMenu.gameObject.SetActive(false);
        canvasMenu.gameObject.SetActive(true);
        setBindings = false;
        startGameCam = false;
        setNick = true;

        textNick = GameObject.Find("TextNick").GetComponent<Text>();
        inputFieldNick = GameObject.Find("InputFieldNick").GetComponent<InputField>();
        buttonPlayGame = GameObject.Find("ButtonPlayGame").GetComponent<Button>();
        buttonSettings = GameObject.Find("ButtonSettings").GetComponent<Button>();
        buttonQuitGame = GameObject.Find("ButtonQuitGame").GetComponent<Button>();

        
        if(listenPlayGame == false)
        {
            buttonPlayGame.onClick.AddListener(StartGame);
            listenPlayGame = true;
        }
        if (listenSettings == false)
        {
            buttonSettings.onClick.AddListener(SetSettingsMenu);
            listenSettings = true;
        }
        if (listenQuitGame == false)
        {
            buttonQuitGame.onClick.AddListener(QuitGame);
            listenQuitGame = true;
        }

    }


    void StartGame()
    {
        canvasSettingsMenu.gameObject.SetActive(false);
        canvasMenu.gameObject.SetActive(false);
        canvasGame.gameObject.SetActive(true);

        text1x.gameObject.SetActive(true);

        textCamera = GameObject.Find("TextCamera").GetComponent<Text>();
        textPlayer = GameObject.Find("TextPlayer").GetComponent<Text>();

        buttonInGameSettings = GameObject.Find("Button_settings").GetComponent<Button>();
        buttonBackToMenu = GameObject.Find("Button_menu").GetComponent<Button>();
        buttonPreviousCam = GameObject.Find("Button_arrow_leftCam").GetComponent<Button>();
        buttonNextCam = GameObject.Find("Button_arrow_rightCam").GetComponent<Button>();

        slider.value = 2.0f;

        if (startGameCam == false)
            setFreeCam = true;

        setNick = false;
        setBindings = true;
        setNameCamera();
        textPlayer.text = playernick;

        if (listenPrevCam == false)
        {
            buttonPreviousCam.onClick.AddListener(PreviousCamera);
            listenPrevCam = true;
        }

        if (listenNextCam == false)
        {
            buttonNextCam.onClick.AddListener(NextCamera);
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
        canvasMenu.gameObject.SetActive(false);
        canvasSettingsMenu.gameObject.SetActive(true);

        GetOptions(); //get settings

        button_applyMenu = GameObject.Find("Button_applyMenu").GetComponent<Button>();
        button_xDiscardMenu = GameObject.Find("Button_xDiscardMenu").GetComponent<Button>();
        

        if (listenApplyM == false)
        {
            button_applyMenu.onClick.AddListener(ApplyMenuSettings);
            listenApplyM = true;
        }

        if (listenDiscardM == false)
        {
            button_xDiscardMenu.onClick.AddListener(DiscardMenuSettings);
            listenDiscardM = true;
        }

    }

    void InGameSettings()
    {
        //settingsAnim = true;
        canvasSettingsInGameMenu.gameObject.SetActive(true);

        backgroundSettingsInGame = GameObject.Find("bg").GetComponent<Image>();
        button_applyGame = GameObject.Find("Button_applyGame").GetComponent<Button>();
        button_xDiscardGame = GameObject.Find("Button_xDiscardGame").GetComponent<Button>();

        GetOptions();

        //backgroundSettingsInGame.transform.position = new Vector3(900, 250, 0); //quelli finali Vector3(958, 277, 0);
        //SettingsAnimation();

        if (listenApplyG == false)
        {
            button_applyGame.onClick.AddListener(ApplyInGameSettings);
            listenApplyG = true;
        }

        if (listenDiscardG == false)
        {
            button_xDiscardGame.onClick.AddListener(DiscardInGameSettings);
            listenDiscardG = true;
        }

        startGameCam = true;
    }

    //OTHER USEFUL METHODS

    void MoveSlider()
    {
        if (slider.value == 0)
        {
            text025x.gameObject.SetActive(true);
            text05x.gameObject.SetActive(false);
            text1x.gameObject.SetActive(false);
            text2x.gameObject.SetActive(false);
            textMax.gameObject.SetActive(false);
            SimulationController.ChangeSimulationSpeed(0);
        }
        else if (slider.value == 1)
        {
            text025x.gameObject.SetActive(false);
            text05x.gameObject.SetActive(true);
            text1x.gameObject.SetActive(false);
            text2x.gameObject.SetActive(false);
            textMax.gameObject.SetActive(false);
            SimulationController.ChangeSimulationSpeed(1);
        }
        else if (slider.value == 2)
        {
            text025x.gameObject.SetActive(false);
            text05x.gameObject.SetActive(false);
            text1x.gameObject.SetActive(true);
            text2x.gameObject.SetActive(false);
            textMax.gameObject.SetActive(false);
            SimulationController.ChangeSimulationSpeed(2);
        }
        else if (slider.value == 3)
        {
            text025x.gameObject.SetActive(false);
            text05x.gameObject.SetActive(false);
            text1x.gameObject.SetActive(false);
            text2x.gameObject.SetActive(true);
            textMax.gameObject.SetActive(false);
            SimulationController.ChangeSimulationSpeed(3);
        }
        else if (slider.value == 4)
        {
            text025x.gameObject.SetActive(false);
            text05x.gameObject.SetActive(false);
            text1x.gameObject.SetActive(false);
            text2x.gameObject.SetActive(false);
            textMax.gameObject.SetActive(true);
            SimulationController.ChangeSimulationSpeed(4);
        }

        //send sim speed to Mason -------TODO
    }

    //void SettingsAnimation()
    //{
    //    backgroundSettingsInGame.transform.position = Vector3.MoveTowards(backgroundSettingsInGame.transform.position, bgFinishAnim, animSpeed * Time.deltaTime);
    //    if (backgroundSettingsInGame.transform.position == new Vector3(958, 277, 0))
    //        settingsAnim = false;
    //}

    //void ResetAnimation()
    //{
    //    backgroundSettingsInGame.transform.position = Vector3.MoveTowards(backgroundSettingsInGame.transform.position, bgStartAnim, animSpeed * Time.deltaTime);
    //    if (backgroundSettingsInGame.transform.position == new Vector3(1255, 277, 0))
    //    {
    //        resetAnim = false;
    //    }
    //    StartCoroutine(Wait1Second());

    //}

    //IEnumerator Wait1Second()
    //{
    //    yield return new WaitForSeconds(1.0f);
    //    canvasSettingsInGameMenu.gameObject.SetActive(false);

    //}

    void GetOptions()
    {
        cohesionIF = GameObject.Find("InputFieldCohesion").GetComponent<InputField>();
        avoidanceIF = GameObject.Find("InputFieldAvoidance").GetComponent<InputField>();
        avoidDistanceIF = GameObject.Find("InputFieldAvoidDistance").GetComponent<InputField>();
        randomnessIF = GameObject.Find("InputFieldRandomness").GetComponent<InputField>();
        consistencyIF = GameObject.Find("InputFieldConsistency").GetComponent<InputField>();
        momentumIF = GameObject.Find("InputFieldMomentum").GetComponent<InputField>();
        neighborhoodIF = GameObject.Find("InputFieldNeighborhood").GetComponent<InputField>();
        deadAgentProbIF = GameObject.Find("InputFieldDeadAgentProb").GetComponent<InputField>();
        jumpIF = GameObject.Find("InputFieldJump").GetComponent<InputField>();
        widthIF = GameObject.Find("InputFieldWidth").GetComponent<InputField>();
        heightIF = GameObject.Find("InputFieldHeight").GetComponent<InputField>();
        lenghtIF = GameObject.Find("InputFieldLenght").GetComponent<InputField>();
        numAgentsIF = GameObject.Find("InputFieldNumAgents").GetComponent<InputField>();
        simStepRateIF = GameObject.Find("InputFieldSimStepRate").GetComponent<InputField>();
        simStepDelayIF = GameObject.Find("InputFieldSimStepDelay").GetComponent<InputField>();
        framerateIF = GameObject.Find("InputFieldFramerate").GetComponent<InputField>();

        cohesionIF.text = SimulationController.FlockSim.Cohesion.ToString();
        avoidanceIF.text = SimulationController.FlockSim.Avoidance.ToString();
        avoidDistanceIF.text = SimulationController.FlockSim.AvoidDistance.ToString();
        randomnessIF.text = SimulationController.FlockSim.Randomness.ToString();
        consistencyIF.text = SimulationController.FlockSim.Consistency.ToString();
        momentumIF.text = SimulationController.FlockSim.Momentum.ToString();
        neighborhoodIF.text = SimulationController.FlockSim.Neighborhood.ToString();
        deadAgentProbIF.text = SimulationController.FlockSim.DeadAgentProbability.ToString();
        jumpIF.text = SimulationController.FlockSim.Jump.ToString();
        widthIF.text = SimulationController.FlockSim.Width.ToString();
        heightIF.text = SimulationController.FlockSim.Height.ToString();
        lenghtIF.text = SimulationController.FlockSim.Lenght.ToString();
        numAgentsIF.text = SimulationController.FlockSim.NumAgents.ToString();
        simStepRateIF.text = SimulationController.FlockSim.SimStepRate.ToString();
        simStepDelayIF.text = SimulationController.FlockSim.SimStepDelay.ToString();
        framerateIF.text = framerate.ToString();
    }

    void SetOptions()
    {
        SimulationController.FlockSim.Cohesion = float.Parse(cohesionIF.text);
        SimulationController.FlockSim.Avoidance = float.Parse(avoidanceIF.text);
        SimulationController.FlockSim.AvoidDistance = float.Parse(avoidDistanceIF.text);
        SimulationController.FlockSim.Randomness = float.Parse(randomnessIF.text);
        SimulationController.FlockSim.Consistency = float.Parse(consistencyIF.text);
        SimulationController.FlockSim.Momentum = float.Parse(momentumIF.text);
        SimulationController.FlockSim.Neighborhood = float.Parse(neighborhoodIF.text);
        SimulationController.FlockSim.DeadAgentProbability = float.Parse(deadAgentProbIF.text);
        SimulationController.FlockSim.Jump = float.Parse(jumpIF.text);
        SimulationController.FlockSim.Width = float.Parse(widthIF.text);
        SimulationController.FlockSim.Height = float.Parse(heightIF.text);
        SimulationController.FlockSim.Lenght = float.Parse(lenghtIF.text);
        SimulationController.FlockSim.NumAgents = int.Parse(numAgentsIF.text);
        SimulationController.FlockSim.SimStepRate = int.Parse(simStepRateIF.text);
        SimulationController.FlockSim.SimStepDelay = float.Parse(simStepDelayIF.text);
        framerate = float.Parse(framerateIF.text);
    }

    void SetFramerate()
    {
        Application.targetFrameRate = (int) framerate;
    }


    void SetCamStartPosition()
    {
        freeCam.transform.position = Vector3.MoveTowards(freeCam.transform.position, endMarkerP, camSpeed * Time.deltaTime);
        freeCam.transform.rotation = Quaternion.RotateTowards(freeCam.transform.rotation, endMarkerR, camRotation * Time.deltaTime);
        if (freeCam.transform.position == endMarkerP && freeCam.transform.rotation == endMarkerR)
            setFreeCam = false;
    }

    void MoveFreeCamera()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.position += new Vector3(0, -speed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.position += new Vector3(0, speed * Time.deltaTime, 0);
        }
        if (Input.GetKey(KeyCode.R))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.Rotate(Vector3.forward, speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.F))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.Rotate(Vector3.back, speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.W))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.position = freeCam.transform.position + Camera.main.transform.forward * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.position = freeCam.transform.position - Camera.main.transform.right * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.position = freeCam.transform.position - Camera.main.transform.forward * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.position = freeCam.transform.position + Camera.main.transform.right * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.Rotate(Vector3.left, speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.Rotate(Vector3.down, speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.Rotate(Vector3.right, speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            if (freeCam.isActiveAndEnabled)
                freeCam.transform.Rotate(Vector3.up, speed * Time.deltaTime);
        }


    }

    void SetMainCamera()
    {
        cam1.gameObject.SetActive(false);
        cam2.gameObject.SetActive(false);
        cam3.gameObject.SetActive(false);
        cam4.gameObject.SetActive(false);
        cam5.gameObject.SetActive(false);
        freeCam.gameObject.SetActive(true);
    }

    void PreviousCamera()
    {
        if (freeCam.isActiveAndEnabled)
        {
            freeCam.gameObject.SetActive(false);
            cam5.gameObject.SetActive(true);
            nameCamera = "Camera 5";
            setNameCamera();

        } else if (cam5.isActiveAndEnabled)
        {
            cam5.gameObject.SetActive(false);
            cam4.gameObject.SetActive(true);
            nameCamera = "Camera 4";
            setNameCamera();

        } else if (cam4.isActiveAndEnabled)
        {
            cam4.gameObject.SetActive(false);
            cam3.gameObject.SetActive(true);
            nameCamera = "Camera 3";
            setNameCamera();

        } else if (cam3.isActiveAndEnabled)
        {
            cam3.gameObject.SetActive(false);
            cam2.gameObject.SetActive(true);
            nameCamera = "Camera 2";
            setNameCamera();

        } else if (cam2.isActiveAndEnabled)
        {
            cam2.gameObject.SetActive(false);
            cam1.gameObject.SetActive(true);
            nameCamera = "Camera 1";
            setNameCamera();

        } else if (cam1.isActiveAndEnabled)
        {
            cam1.gameObject.SetActive(false);
            freeCam.gameObject.SetActive(true);
            nameCamera = "Free Camera";
            setNameCamera();
        }
    }

    void NextCamera()
    {
        if (cam5.isActiveAndEnabled)
        {
            cam5.gameObject.SetActive(false);
            freeCam.gameObject.SetActive(true);
            nameCamera = "Free Camera";
            setNameCamera();

        }
        else if (cam4.isActiveAndEnabled)
        {
            cam4.gameObject.SetActive(false);
            cam5.gameObject.SetActive(true);
            nameCamera = "Camera 5";
            setNameCamera();

        }
        else if (cam3.isActiveAndEnabled)
        {
            cam3.gameObject.SetActive(false);
            cam4.gameObject.SetActive(true);
            nameCamera = "Camera 4";
            setNameCamera();

        }
        else if (cam2.isActiveAndEnabled)
        {
            cam2.gameObject.SetActive(false);
            cam3.gameObject.SetActive(true);
            nameCamera = "Camera 3";
            setNameCamera();

        }
        else if (cam1.isActiveAndEnabled)
        {
            cam1.gameObject.SetActive(false);
            cam2.gameObject.SetActive(true);
            nameCamera = "Camera 2";
            setNameCamera();

        }
        else if (freeCam.isActiveAndEnabled)
        {
            freeCam.gameObject.SetActive(false);
            cam1.gameObject.SetActive(true);
            nameCamera = "Camera 1";
            setNameCamera();

        }
    }

    void setNameCamera()
    {
        textCamera.text = nameCamera;
    }


    void ApplyInGameSettings()
    {
        SetOptions();
        //resetAnim = true;
        //ResetAnimation();
        if (SimulationController.State.Equals(SimulationController.simulationState.PLAY) 
            || SimulationController.State.Equals(SimulationController.simulationState.PAUSE))
        {
            SimulationController.SendSimulationSettings(true);
        }
        else
        {
            SimulationController.SendSimulationSettings(false);
        }

        canvasSettingsInGameMenu.gameObject.SetActive(false);
        StartGame();
    }


    void ApplyMenuSettings()
    {
        SetOptions();
        if (SimulationController.State.Equals(SimulationController.simulationState.PLAY)
            || SimulationController.State.Equals(SimulationController.simulationState.PAUSE))
        {
            SimulationController.SendSimulationSettings(true);
        }
        else
        {
            SimulationController.SendSimulationSettings(false);
        }
        SetMainMenu();
    }

    
    void DiscardInGameSettings()
    {
        cohesionIF.text = SimulationController.FlockSim.Cohesion.ToString();
        avoidanceIF.text = SimulationController.FlockSim.Avoidance.ToString();
        avoidDistanceIF.text = SimulationController.FlockSim.AvoidDistance.ToString();
        randomnessIF.text = SimulationController.FlockSim.Randomness.ToString();
        consistencyIF.text = SimulationController.FlockSim.Consistency.ToString();
        momentumIF.text = SimulationController.FlockSim.Momentum.ToString();
        neighborhoodIF.text = SimulationController.FlockSim.Neighborhood.ToString();
        deadAgentProbIF.text = SimulationController.FlockSim.DeadAgentProbability.ToString();
        jumpIF.text = SimulationController.FlockSim.Jump.ToString();
        widthIF.text = SimulationController.FlockSim.Width.ToString();
        heightIF.text = SimulationController.FlockSim.Height.ToString();
        lenghtIF.text = SimulationController.FlockSim.Lenght.ToString();
        numAgentsIF.text = SimulationController.FlockSim.NumAgents.ToString();
        simStepRateIF.text = SimulationController.FlockSim.SimStepRate.ToString();
        simStepDelayIF.text = SimulationController.FlockSim.SimStepDelay.ToString();

        //resetAnim = true;
        //ResetAnimation();

        canvasSettingsInGameMenu.gameObject.SetActive(false);


        StartGame();

    }

    void DiscardMenuSettings()
    {
        cohesionIF.text = SimulationController.FlockSim.Cohesion.ToString();
        avoidanceIF.text = SimulationController.FlockSim.Avoidance.ToString();
        avoidDistanceIF.text = SimulationController.FlockSim.AvoidDistance.ToString();
        randomnessIF.text = SimulationController.FlockSim.Randomness.ToString();
        consistencyIF.text = SimulationController.FlockSim.Consistency.ToString();
        momentumIF.text = SimulationController.FlockSim.Momentum.ToString();
        neighborhoodIF.text = SimulationController.FlockSim.Neighborhood.ToString();
        deadAgentProbIF.text = SimulationController.FlockSim.DeadAgentProbability.ToString();
        jumpIF.text = SimulationController.FlockSim.Jump.ToString();
        widthIF.text = SimulationController.FlockSim.Width.ToString();
        heightIF.text = SimulationController.FlockSim.Height.ToString();
        lenghtIF.text = SimulationController.FlockSim.Lenght.ToString();
        numAgentsIF.text = SimulationController.FlockSim.NumAgents.ToString();
        simStepRateIF.text = SimulationController.FlockSim.SimStepRate.ToString();
        simStepDelayIF.text = SimulationController.FlockSim.SimStepDelay.ToString();
        SetMainMenu();

    }

    void CheckNickname()
    {
        if (inputFieldNick.text.Length > 0 && inputFieldNick.text.Length < 11 && !inputFieldNick.text.Contains(" "))
        {
            buttonPlayGame.interactable = true;
            playernick = inputFieldNick.text;
        }
        else buttonPlayGame.interactable = false;

    }
    
    void QuitGame()
    {
        Application.Quit();
    }
    
}

