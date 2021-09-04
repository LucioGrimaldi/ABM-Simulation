using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;


public class MenuController : MonoBehaviour
{
    List<string> simulations = new List<string>() { "Flocker", "Ant Foraging", "Coming Soon" };
    List<string> models = new List<string>() { "Bird", "Sheep", "Wolf", "Ant" };

    public event EventHandler<NicknameEnterEventArgs> OnNicknameEnterEventHandler;

    public TMP_Dropdown dropdownSimType, dropdownAgentModel;
    public TMP_Text infoText1, infoText2, simTypeText;
    public Image simImage1, simImage2, agentImage;
    public Sprite spriteSimFlocker, spriteSimAnt, spriteSimMuseum, spriteBirdModel, spriteSheepModel, spriteWolfModel, spriteAntModel;
    public GameObject settings_Prefab, settingsPanel, agentsPanel;
    public InputField nicknameField;
    public Button newSimButton;
    private GameObject simSettings, agentSettings;
    private int sim = 16, agent = 5; //simulationType = 0; //0 = Flocker, 1 = Ant, 2 = Museum

    void Start()
    {
        PopulateList();
        LoadSimSettings(sim); //spawna primi settaggi per la simulazione flocker
        LoadAgentSettings(agent); //spawna primi settaggi per gli agenti flocker
    }

    void Update()
    {
        CheckNickname();
    }

    //ALTRI METODI

    

    void PopulateList()
    {
        dropdownSimType.AddOptions(simulations);
        dropdownAgentModel.AddOptions(models);
    }


    void LoadSimSettings(int sim)
    {

        for (int i = 0; i < sim; i++)
        {
            simSettings = Instantiate(settings_Prefab);
            simSettings.transform.SetParent(settingsPanel.transform);
        }
    }


    void LoadAgentSettings(int agent)
    {
        for (int i = 0; i < agent; i++)
        {
            agentSettings = Instantiate(settings_Prefab);
            agentSettings.transform.SetParent(agentsPanel.transform);
        }
    }


    public void Dropdown_IndexChanged(int index)
    {
        if (index == 0) //FLOCKER
        {
            foreach (Transform child in settingsPanel.transform) //distruggi i settaggi se cambi simulazione e ricreali
                GameObject.Destroy(child.gameObject);

            LoadSimSettings(16);
            infoText1.text = "Agent Based Simulation with Flocker";
            simImage1.sprite = spriteSimFlocker;
            simTypeText.text = "Flocker";
            simImage2.sprite = simImage1.sprite;

            //simulationType = 0;
        }

        if (index == 1) //ANT
        {
            foreach (Transform child in settingsPanel.transform)
                GameObject.Destroy(child.gameObject);

            LoadSimSettings(6);
            infoText1.text = "Ant Foraging Simulation bla bla bla";
            simImage1.sprite = spriteSimAnt;
            simTypeText.text = "Ant Foraging";
            simImage2.sprite = simImage1.sprite;

            //simulationType = 1;
        }

        if (index == 2) //MUSEUM
        {
            foreach (Transform child in settingsPanel.transform)
                GameObject.Destroy(child.gameObject);

            LoadSimSettings(1);
            infoText1.text = "To be defined...";
            simImage1.sprite = spriteSimMuseum;
            simTypeText.text = "Null";
            simImage2.sprite = simImage1.sprite;

            //simulationType = 2;
        }

        infoText2.text = infoText1.text;
    }


    public void Dropdown_ModelChanged(int index)
    {
        if (index == 0)
        {
            foreach (Transform child in agentsPanel.transform) //distruggi i settaggi degli agenti se cambi modello di agente e ricreali
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(4);
            agentImage.sprite = spriteBirdModel;
            //SCARICA SETTAGGI AGENTE DA MASON E VISUALIZZALI
        }
        if (index == 1)
        {
            foreach (Transform child in agentsPanel.transform) 
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(3);
            agentImage.sprite = spriteSheepModel;
            //

        }
        if (index == 2)
        {
            foreach (Transform child in agentsPanel.transform) 
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(2);
            agentImage.sprite = spriteWolfModel;
            //
        }
        if (index == 3)
        {
            foreach (Transform child in agentsPanel.transform) 
                GameObject.Destroy(child.gameObject);

            LoadAgentSettings(8);
            agentImage.sprite = spriteAntModel;
            //
        }
    }


    public void SendPlayerNick()
    {
        NicknameEnterEventArgs e = new NicknameEnterEventArgs();
        //e.nickname = nicknameField.text.Equals("")? "Player" : nicknameField.text;
        e.nickname = nicknameField.text;
        OnNicknameEnterEventHandler?.Invoke(this, e);
    }

    void CheckNickname()
    {
        if (nicknameField.text.Length > 0 && nicknameField.text.Length < 16 && !nicknameField.text.Contains(" "))
            newSimButton.interactable = true;
        else newSimButton.interactable = false;

    }

    public void StartSimulationSelected()
    {
        SceneManager.LoadScene("MainScene");
    }


    public void QuitGame()
    {
        Application.Quit();
    }

}
