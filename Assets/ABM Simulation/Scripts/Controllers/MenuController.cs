using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using SimpleJSON;

public class MenuController : MonoBehaviour
{

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


    public static event EventHandler<NicknameEnterEventArgs> OnNicknameEnterEventHandler;

    public TMP_Dropdown dropdownSimTypes, dropdownAgents, dropdownObjects;
    public TMP_Text simDescription, simDescription2, simTypeText;
    public Image simImage1, simImage2, agentImage;
    public GameObject settingsAgentsMenuPrefab, settingsScrollContent, agentsScrollContent;
    public InputField nicknameField;
    public Button newSimButton, joinSimButton;
    private int sim = 20, agent = 10; //simulationType = 0; //0 = Flocker, 1 = Ant, 2 = Museum
    private string showSimSpace = "true", showEnv = "true";


    void Awake()
    {
        SimulationController.OnSimListSuccessEventHandler += onSimListSuccess;
        SimulationController.OnSimListUnsuccessEventHandler += onSimListUnsuccess;
        SimulationController.OnSimInitSuccessEventHandler += onSimListInitSuccess;
        SimulationController.OnSimInitUnsuccessEventHandler += onSimListInitUnsuccess;

    }

    void Start()
    {
        LoadSimSettings(sim); //spawna primi settaggi per la simulazione flocker
        LoadAgentSettings(agent); //spawna primi settaggi per gli agenti flocker
    }

    void Update()
    {
        CheckNickname();
    }



    //ALTRI METODI

    private void LoadSimNames(JSONArray simList)
    {
        foreach (JSONObject s in simList)
        {
            IDs.Add(s["id"]);
            simNames.Add(s["name"]);
            descriptions.Add(s["description"]);
            types.Add(s["type"]);
            dimensions.Add(((JSONArray)s["dimensions"]).Count);

            List<string> list = new List<string>();
            foreach (JSONObject a in (JSONArray)s["agent_prototypes"])
                list.Add(a["class"]);
            agents.Add(list);

            foreach (JSONObject a in (JSONArray)s["generic_prototypes"])
                list.Add(a["class"]);
            generics.Add(list);
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

    public void PopulateSimListDropdown()
    {
        dropdownSimTypes.AddOptions(simNames);
    }

    public void PopulateAgentsListDropdown()
    {
        dropdownAgents.AddOptions(agents[dropdownSimTypes.value]);
    }

    public void PopulateObjectsListDropdown()
    {
        dropdownObjects.AddOptions(generics[dropdownSimTypes.value]);
    }

    void LoadSimSettings(int sim)
    {
        for (int i = 0; i < sim; i++)
        {
            GameObject simSettings = Instantiate(settingsAgentsMenuPrefab);
            simSettings.transform.SetParent(settingsScrollContent.transform);
        }
    }

    void LoadAgentSettings(int agent)
    {
        for (int i = 0; i < agent; i++)
        {
            GameObject agentSettings = Instantiate(settingsAgentsMenuPrefab);
            agentSettings.transform.SetParent(agentsScrollContent.transform);
        }
    }

    public void Dropdown_SimChanged(int index)
    {
        foreach (Transform child in settingsScrollContent.transform) //distruggi i settaggi se cambi simulazione e ricreali
            GameObject.Destroy(child.gameObject);

        //LoadSimInfos();
        //simImage1.sprite = spriteSimFlocker;
        simTypeText.text = simNames[index];
        simImage2.sprite = simImage1.sprite;
    }

    public void Dropdown_AgentsChanged(int index)
    {
        if (index == 0)
        {
            foreach (Transform child in agentsScrollContent.transform) //distruggi i settaggi degli agenti se cambi modello di agente e ricreali
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(4);
            //agentImage.sprite = spriteBirdModel;
            //SCARICA SETTAGGI AGENTE DA MASON E VISUALIZZALI
        }
        if (index == 1)
        {
            foreach (Transform child in agentsScrollContent.transform) 
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(3);
            //agentImage.sprite = spriteSheepModel;
            //

        }
        if (index == 2)
        {
            foreach (Transform child in agentsScrollContent.transform) 
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(2);
            //agentImage.sprite = spriteWolfModel;
            //
        }
        if (index == 3)
        {
            foreach (Transform child in agentsScrollContent.transform) 
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(8);
            //agentImage.sprite = spriteAntModel;
            //
        }
    }


    public void CheckPlayerName()
    {
        NicknameEnterEventArgs e = new NicknameEnterEventArgs();
        //e.nickname = nicknameField.text.Equals("")? "Player" : nicknameField.text;
        e.nickname = nicknameField.text;
        OnNicknameEnterEventHandler?.Invoke(this, e);
    }

    void CheckNickname()
    {
        bool nickAvail = true;
        if (nicknameField.text.Length > 0 && nicknameField.text.Length < 16 && !nicknameField.text.Contains(" ") && nickAvail)
        {
            if (nickAvail)//Room already exists
            {
                //newSimButton.GetComponentInChildren<TextMeshProUGUI>().text = "Join Simulation";

                newSimButton.gameObject.SetActive(false); 
                joinSimButton.gameObject.SetActive(true);
                joinSimButton.interactable = true;
                
            }
            else
            {
                //newSimButton.GetComponentInChildren<TextMeshProUGUI>().text = "New Simulation";
                joinSimButton.gameObject.SetActive(false);
                newSimButton.gameObject.SetActive(true);
                newSimButton.interactable = true;

            }
        }
        else
        {
            newSimButton.interactable = false;
            joinSimButton.interactable = false;
        }

    }

    public void StartSimulation()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }




    //BUTTONS CALLBACKS

    public void OnNewSimCallback()
    {
        PopulateSimListDropdown();

    }

    public void OnJoinSimCallback()
    {

    }




    //EVENT METHODS


    private void onSimListSuccess(object sender, ReceivedMessageEventArgs e)
    {
        LoadSimNames((JSONArray) e.Payload["list"]);
        LoadSprites();
    }

    private void onSimListUnsuccess(object sender, ReceivedMessageEventArgs e)
    {

    }

    private void onSimListInitSuccess(object sender, ReceivedMessageEventArgs e)
    {

    }

    private void onSimListInitUnsuccess(object sender, ReceivedMessageEventArgs e)
    {

    }


}
