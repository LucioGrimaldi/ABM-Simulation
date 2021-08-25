using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GerardoUtils;

public class UIController : MonoBehaviour
{
    private int counter1 = 1, counter2 = 1, counter3 = 1, counter4 = 1;
    public GameObject panelSim, panelSettings, panelInfo, panelBackToMenu;
    public Slider slider;

    //private Grid2DGeneric<bool> grid;

    void Start()
    {
        slider.onValueChanged.AddListener(delegate { MoveSlider(); });

        //grid = new Grid2DGeneric<bool>(5,4, 10f, new Vector3(10,0,0), (Grid2DGeneric<bool> g, int x, int z) => new bool());


    }


    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    grid.SetValue(UtilsClass.GetMouseWorldPosition(), 50);
        //}
        //if (Input.GetMouseButtonDown(1))
        //{
        //    Debug.Log(grid.GetValue(UtilsClass.GetMouseWorldPosition()));
        //}
    }


    //ALTRI METODI
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
        Debug.Log("PLAY SIM");
    }


    public void PauseSimulation()
    {
        Debug.Log("PAUSE SIM");
    }


    public void StopSimulation()
    {
        Debug.Log("STOP SIM");
    }


    public void ApplySettings()
    {
        //INVIA SETTAGGI A MASON
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }


    public void ShowHidePanelSim()
    {
        counter1++;
        if (counter1 % 2 == 1)
            panelSim.gameObject.SetActive(false);
        else
            panelSim.gameObject.SetActive(true);

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
            panelInfo.gameObject.SetActive(false);
        else
            panelInfo.gameObject.SetActive(true);

    }


    public void ShowHidePanelQuit()
    {
        counter4++;
        if (counter4 % 2 == 1)
            panelBackToMenu.gameObject.SetActive(false);
        else
            panelBackToMenu.gameObject.SetActive(true);

    }
}
