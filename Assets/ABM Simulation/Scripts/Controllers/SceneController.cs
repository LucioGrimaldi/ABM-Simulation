using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SceneController : MonoBehaviour
{
    private static int widthlenght = 10; //Default 10x10
    private static int scaleFactor = widthlenght / 10;
    public GameObject antEnvironment, flockerEnvironment;
    private static bool showSimSpace, showEnvironment;
    private GameObject simulationSpace, visualEnvironment, UIManager;
    private UIController UIController;
    private Button editButton;
    private bool canInteractSimSpace = true; //sim = ant


    private void Awake()
    {
        //FLOCKER
        //GameObject flockEnv = Instantiate(flockerEnvironment, new Vector3(200, 0, 200), Quaternion.identity);
        //flockEnv.transform.Rotate(0, 90, 0, Space.Self);
        //editButton = GameObject.Find("ButtonEdit").GetComponent<Button>();
        //editButtonFlock = false;

        //ANT
        GameObject antEnv = Instantiate(antEnvironment, transform.position, Quaternion.identity);
        antEnv.transform.localScale = new Vector3(transform.localScale.x * scaleFactor, transform.localScale.y * scaleFactor, transform.localScale.z * scaleFactor);
        antEnv.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial.SetFloat("_GridSize", widthlenght); //setta material size griglia

        //gridXZ.GetComponent<Renderer>().sharedMaterial.SetFloat("_GridSize", widthlenght); //setta material size griglia
        //GameObject gridPlane = Instantiate(gridXZ, Vector3.zero, Quaternion.Euler(new Vector3(0, 180, 0)));
        //gridPlane.transform.localScale = new Vector3(widthlenght, transform.position.y, widthlenght); //ingrandisce
        //gridPlane.transform.position = new Vector3(5 * widthlenght, 0, 5 * widthlenght); //sposta

        simulationSpace = GameObject.FindWithTag("SimulationCube");
        visualEnvironment = GameObject.FindWithTag("Environment");

        UIManager = GameObject.Find("UIMananger");
        UIController = UIManager.GetComponent<UIController>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        CanInteractSimSpace(); //solo ant
        ShowHideSimEnvironment();

    }


    //OTHER METHODS
    void CanInteractSimSpace()
    {
        if (!canInteractSimSpace)
            editButton.interactable = false;
    }

    //metodo dei 2 toggle

    //

    //

    void ShowHideSimEnvironment()
    {
        showSimSpace = UIController.showSimSpace;
        showEnvironment = UIController.showEnvironment;

        if (showSimSpace)
            simulationSpace.gameObject.SetActive(true);
        else
            simulationSpace.gameObject.SetActive(false);

        if (showEnvironment)
            visualEnvironment.gameObject.SetActive(true);
        else
            visualEnvironment.gameObject.SetActive(false);
    }

}
