using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GridSystem;

public class SelectChangedEventArgs : EventArgs
{
    public SimObject.SimObjectType type;
    public string class_name;
    public SimObjectSO so;
}

public class SceneController : MonoBehaviour
{
    // Player Preferences
    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;

    // Events
    public static event EventHandler<SimObjectCreateEventArgs> OnSimObjectCreateEventHandler;    
    public static event EventHandler<SimObjectModifyEventArgs> OnSimObjectModifyEventHandler;    
    public static event EventHandler<SimObjectDeleteEventArgs> OnSimObjectDeleteEventHandler;    
    public static event EventHandler<SelectChangedEventArgs> OnSelectChangedEventHandler;    

    private static bool showSimSpace, showEnvironment;
    private GameObject simulationSpace, visualEnvironment;

    // Controllers
    private SimulationController SimulationController;
    private UIController UIController;
    private SimSpaceSystem SimSpaceSystem;

    // Prefabs
    public GameObject[] SimSpace_prefabs_2D;
    public GameObject[] SimSpace_prefabs_3D;
    public GameObject[] Environment_prefabs;
    public Shader[] Shaders2D;
    public Shader[] Shaders3D;

    // Placeable SOs
    public List<SOCollection> SOCollections;
    public SOCollection SimObjectsData;

    // PlacedObjects
    private static ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), PlaceableObject> simObjectRenders = new ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), PlaceableObject>();

    // Variables
    public static int simId;
    private static ConcurrentDictionary<string, object> simDimensions;
    private object width = 1, height = 1, lenght = 1;
    private static Simulation.SimTypeEnum simType;
    private static bool isDiscrete;
    private PlaceableObject selectedSimObject = null;
    public GameObject cellDebug;

    /// UNITY LOOP METHODS ///

    /// <summary>
    /// We use Awake to bootstrap App
    /// </summary>
    private void Awake()
    {
        // ignore collisions
        Physics.IgnoreLayerCollision(9, 9, true);
        // bind GridSystem and Controllers
        SimulationController = GameObject.Find("SimulationController").GetComponent<SimulationController>();
        UIController = GameObject.Find("UIController").GetComponent<UIController>();
        simId = SimulationController.GetSimId();
        simDimensions = SimulationController.GetSimDimensions();
        simType = SimulationController.GetSimType();
        isDiscrete = simType.Equals(Simulation.SimTypeEnum.DISCRETE);
        InitSimSpaceSystem();
        InitScene();
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    private void OnEnable()
    {
        // Register to EventHandlers

       
    }
    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    private void Start()
    {
        
    }
    /// <summary>
    /// Update routine (Unity Process)
    /// </summary>
    private void Update()
    {
        if (SimulationController.GetSimState().Equals(Simulation.StateEnum.PLAY)) StepUp();
        if (SimulationController.GetSimState().Equals(Simulation.StateEnum.STEP)) {StepUp(); if(SimulationController.steps_to_consume==0) SimulationController.GetSimulation().state = Simulation.StateEnum.PAUSE; }
        ShowHideSimEnvironment();       // può essere sostituito con event
        CheckForUserInput();
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    private void OnApplicationQuit()
    {
        simObjectRenders.Clear();
    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {
        // Unregister to EventHandlers
        
    }


    /// Methods ///
    // Initialization
    public void InitSimSpaceSystem()
    {
        if (isDiscrete)
        {
            SimSpaceSystem = new GameObject("SimSpaceSystem").AddComponent<GridSystem>();
            ((GridSystem)SimSpaceSystem).DEBUG_cell = cellDebug;
        }
        //else SimSpaceSystem = GameObject.Find("ContinuousSystem").GetComponent<ContinuousSystem>();
    }
    public void InitScene()
    {
        float scaleFactor;

        // init SO Collection
        SimObjectsData = SOCollections[simId];

        // get sim dimensions (and add z in case)
        if (simDimensions.ContainsKey("x")) simDimensions.TryGetValue("x", out width);
        if (simDimensions.ContainsKey("y")) simDimensions.TryGetValue("y", out lenght);      // swap y-z
        if (simDimensions.ContainsKey("z")) simDimensions.TryGetValue("z", out height);
        if (isDiscrete) { scaleFactor = Mathf.Max((int)width, (int)height, (int)lenght) / 10f; }
        else { scaleFactor = Mathf.Max((int)width, (int)height, (int)lenght) / 10f; }

        // init env
        InitEnvironment();

        // init simSpace
        InitSimSpace(scaleFactor);

    }
    public void InitEnvironment()
    {
        GameObject choosenEnvironment = Environment_prefabs[1];
        visualEnvironment = Instantiate(choosenEnvironment, choosenEnvironment.transform.position, Quaternion.identity);
    }
    public void InitSimSpace(float scaleFactor)
    {
        GameObject choosenSimSpace;
        if (isDiscrete)
        {
            if (simDimensions.Count == 2)
            {
                choosenSimSpace = SimSpace_prefabs_2D[0];
                simulationSpace = Instantiate(choosenSimSpace, choosenSimSpace.transform.position, Quaternion.AngleAxis(180, new Vector3(0, 1, 0)));
                InitShader();
            }
            else
            {
                choosenSimSpace = SimSpace_prefabs_3D[0];
                simulationSpace = Instantiate(choosenSimSpace, choosenSimSpace.transform.position, Quaternion.AngleAxis(180, new Vector3(0, 1, 0)));
            }

            // init GridSystem
            ((GridSystem)SimSpaceSystem).grid = new Grid3D<GridObject>((int)width, (int)height, (int)lenght, 10f / scaleFactor, choosenSimSpace.transform.position - new Vector3(50, 0, 50), ((g, x, y, z) => new GridObject(g, x, y, z)));
        }
        else
        {
            if (simDimensions.Count == 2)
            {
                choosenSimSpace = SimSpace_prefabs_2D[1];
                simulationSpace = Instantiate(choosenSimSpace, choosenSimSpace.transform.position, Quaternion.AngleAxis(180, new Vector3(0, 1, 0)));
            }
            else
            {
                choosenSimSpace = SimSpace_prefabs_3D[1];
                simulationSpace = Instantiate(choosenSimSpace, choosenSimSpace.transform.position, Quaternion.AngleAxis(180, new Vector3(0, 1, 0)));
            }

            // init CountinuosSystem

        }
    }
    public void InitShader()
    {
        simulationSpace.GetComponent<Renderer>().material.shader = Shaders2D[simId];
        simulationSpace.AddComponent<ShaderManager>();
        simulationSpace.GetComponent<Renderer>().sharedMaterial.SetFloat("_GridSize", Mathf.Max((int)width, (int)height, (int)lenght));
        if(simId == 1)
        {
            simulationSpace.GetComponent<ShaderManager>().computeBuffers = new ComputeBuffer[2];
            simulationSpace.GetComponent<ShaderManager>().computeBuffers[0] = new ComputeBuffer((int)width * (int)width, sizeof(float), ComputeBufferType.Append);          // food
            simulationSpace.GetComponent<ShaderManager>().computeBuffers[1] = new ComputeBuffer((int)width * (int)width, sizeof(float), ComputeBufferType.Append);          // home
            simulationSpace.GetComponent<Renderer>().sharedMaterial.SetBuffer("_FoodGrid", simulationSpace.GetComponent<ShaderManager>().computeBuffers[0]);
            simulationSpace.GetComponent<Renderer>().sharedMaterial.SetBuffer("_HomeGrid", simulationSpace.GetComponent<ShaderManager>().computeBuffers[1]);
        }
    }


    // Interaction
    public void ShowHideSimEnvironment()                                // FARE CON EVENTI
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
    public void CheckForUserInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitPoint;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hitPoint, Mathf.Infinity))            // Check if UI is not hit
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())  {} else
                if (SimSpaceSystem.IsGhostSelected()) CreateSimObject(SimSpaceSystem.CreateSimObjectRender(SimSpaceSystem.GetSelectedSimObject())); else
                if (SelectSimObject(hitPoint)) Debug.Log("SELECTED:" + selectedSimObject.type + " " + selectedSimObject.class_name + " " + selectedSimObject.id);
            }
        }
        // Destroy
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (selectedSimObject != null)
            {
                SimSpaceSystem.DeleteSimObjectRender(selectedSimObject);
                DeleteSimObject();
            }
        }
        // Modify
        //if (Input.GetKeyDown(KeyCode.M))
        //{
        //    if(selectedSimObject != null)
        //    {
                
        //        selectedSimObject.SetMovable(true);
        //    }
        //    String buildName;
        //    Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
        //    if (GridSystem.grid.GetGridObject(mousePosition) != null)
        //    {
        //        // Valid Grid Position
        //        PlaceableObject placedObject = GridSystem.grid.GetGridObject(mousePosition).GetPlacedObject();
        //        if (placedObject != null)
        //        {
        //            buildName = placedObject.name;
        //            //Debug.Log("Nome prefab: " + buildName);
        //            // Distruggi
        //            placedObject.Destroy();

        //            List<Vector3Int> gridPositionList = placedObject.GetGridPositionList();
        //            foreach (Vector3Int gridPosition in gridPositionList)
        //            {
        //                GridSystem.grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).ClearPlacedObject();
        //            }

        //            foreach (SimObjectDiscreteSO scriptableObj in GridSystem.placeableAgentSos)
        //            {
        //                if (buildName.Contains(scriptableObj.name))
        //                {
        //                    GridSystem.selectedPlaceableObject = scriptableObj;
        //                    UtilsClass.CreateWorldTextPopup("Moving Building", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);
        //                    GridSystem.RefreshSelectedSimObject();
        //                    //DeselectObjectType();
        //                    break;
        //                }
        //            }
        //        }
        //        else
        //            UtilsClass.CreateWorldTextPopup("Nothing to MOVE!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);

        //    }
        //    else
        //        //do nothing
        //        UtilsClass.CreateWorldTextPopup("Nothing to MOVE!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);

        //}
        // Rotate
        if (Input.GetKeyDown(KeyCode.R)) { SimSpaceSystem.RotateSelectedSimObject(); }
        // Deselect
        if (Input.GetKeyDown(KeyCode.Escape)) { SimSpaceSystem.RemoveGhost(); }


    }                                  // DA SISTEMARE (raycast)
    public bool SelectSimObject(RaycastHit hitPoint)
    {
        if(hitPoint.transform.gameObject.layer.Equals(LayerMask.NameToLayer("Sim Objects")))
        {
            if (simObjectRenders.Values.Contains(getPlaceableObjectRecursive(hitPoint.transform)))
                {
                    selectedSimObject = getPlaceableObjectRecursive(hitPoint.transform);
                    ShowHideInspector(true);
                    return true;
                }
        }        
        return false;
    }
    public void CreateSimObjectRender()
    {
        PlaceableObject x = SimSpaceSystem.CreateSimObjectRender(SimSpaceSystem.GetSelectedSimObject());
    }
    public void DeleteSimObjectRender(PlaceableObject toDelete)
    {
        SimSpaceSystem.DeleteSimObjectRender(toDelete);
    }
    public void RotateSimObjectRender()
    {
        SimSpaceSystem.RotateSelectedSimObject();
    }
    public void SelectPlaceableSimObject(int type, int id)
    {
        (string, SimObjectSO) x;
        SelectChangedEventArgs e = new SelectChangedEventArgs();
        e.type = (SimObject.SimObjectType)type;
        switch (type)
        {
            case 0:
                x = (SimObjectsData.agentClass_names[id], SimObjectsData.agents[id]);
                break;
            case 1:
                x = (SimObjectsData.genericClass_names[id], SimObjectsData.generics[id]);
                break;
            default:
                x = (SimObjectsData.obstacleClass_names[id], SimObjectsData.obstacles[id]);
                break;
        }
        e.class_name = x.Item1;
        e.so = x.Item2;
        OnSelectChangedEventHandler?.Invoke(this, e);
    }


    // Simulation SimObject Events
    public void CreateSimObject(PlaceableObject placedSimObject)
    {
        if(placedSimObject != null)
        {
            SimObjectCreateEventArgs e = new SimObjectCreateEventArgs();
            e.type = placedSimObject.type;
            e.class_name = placedSimObject.class_name;
            e.id = GetTemporaryId(e.type, e.class_name);
            e.parameters = GetSimObjectParams(e.type, e.class_name);        
            e.parameters.TryAdd("position", placedSimObject.Position);

            placedSimObject.id = e.id;
            simObjectRenders.TryAdd((e.type, e.class_name, e.id), placedSimObject);

            OnSimObjectCreateEventHandler?.BeginInvoke(this, e, null, null);
        }
    }
    public void ModifySimObject()
    {

    }
    public void DeleteSimObject()
    {
        SimObjectDeleteEventArgs e = new SimObjectDeleteEventArgs();
        e.type = selectedSimObject.type;
        e.class_name = selectedSimObject.class_name;
        e.id = selectedSimObject.id;

        simObjectRenders.TryRemove((e.type, e.class_name, e.id), out _);

        OnSimObjectDeleteEventHandler?.BeginInvoke(this, e, null, null);
    }

    // Step
    public void StepUp()
    {
        StepSimObjects(SimulationController.GetSimulation().Agents.Values);
        StepSimObjects(SimulationController.GetSimulation().Generics.Values.Where((g) => { if (!g.Class_name.Contains("Pheromone")) return true; else return false; }).ToArray());
        StepPheromones(SimulationController.GetSimulation().Generics.Values.Where((g) => { if (g.Class_name.Contains("Pheromone")) return true; else return false; }).ToArray());
    }
    public Quaternion OrientInSpace(PlaceableObject po, Vector3 newPosition)
    {
        Vector3 forward = newPosition - po.gameObject.transform.position;
        return Quaternion.Slerp(po.gameObject.transform.rotation, Quaternion.LookRotation(forward, Vector3.up), Time.deltaTime);    
    }
    public Vector3 CalcSimSpacePosition(object coords)
    {
        if (isDiscrete)
        {
            if (simDimensions.Count == 2)
            {
                return new Vector3(((MyList<Vector2Int>)coords)[0].x, 0, ((MyList<Vector2Int>)coords)[0].y);
            }
            else
            {
                return new Vector3(((MyList<Vector3Int>)coords)[0].x, ((MyList<Vector3Int>)coords)[0].z, ((MyList<Vector3Int>)coords)[0].y);
            }
        }
        else
        {
            if (simDimensions.Count == 2)
            {
                return new Vector3(((Vector2)coords).x, 0, ((Vector2)coords).y);
            }
            else
            {
                return new Vector3(((Vector3)coords).x, ((Vector3)coords).z, ((Vector3)coords).y);
            }
        }
    }
    public void StepSimObjects(ICollection<SimObject> simObjects)
    {
        foreach (SimObject so in simObjects)
        {
            if (simObjectRenders.TryGetValue((so.Type, so.Class_name, so.Id), out PlaceableObject po))
            {
                so.Parameters.TryGetValue("position", out object coords);
                if (isDiscrete)
                {
                    if (simDimensions.Count == 2)
                    {
                        // calcolare la worldPosition
                        Vector3 simSpacePosition = CalcSimSpacePosition(coords);
                        // calcolare l'orientamento
                        //Quaternion orientation = OrientInSpace(po, ((GridSystem)SimSpaceSystem).grid.GetWorldPosition((int)simSpacePosition.x, (int)simSpacePosition.y, (int)simSpacePosition.z));
                        Quaternion orientation = Quaternion.identity;
                        // muovere la visual
                        MyList<Vector3Int> alteredCoords = new MyList<Vector3Int>();
                        foreach (Vector2Int c in (MyList<Vector2Int>)coords) alteredCoords.Add(new Vector3Int(c.x, 0, c.y));
                        MyList<Vector3Int> old_pos = po.position;
                        SimSpaceSystem.MoveSimObjectRender(po, orientation, PlaceableObject.Dir.Down, simSpacePosition, alteredCoords);
                        //Debug.Log(so.Type + " " + so.Class_name + " " + so.Id + " moved from: " + old_pos + " to: " + po.position);
                    }
                    else
                    {

                    }
                }
                else
                {
                    if (simDimensions.Count == 2)
                    {

                    }
                    else
                    {

                    }
                }

            }
            else
            {
                so.Parameters.TryGetValue("position", out object coords);
                if (isDiscrete)
                {
                    if (simDimensions.Count == 2)
                    {
                        // calcolare la worldPosition
                        Vector3 simSpacePosition = CalcSimSpacePosition(coords);
                        // calcolare l'orientamento
                        Quaternion orientation = Quaternion.identity;
                        // spawnare la visual nella posizione corretta con l'orientamento calcolato
                        MyList<Vector3Int> alteredCoords = new MyList<Vector3Int>();
                        foreach (Vector2Int c in (MyList<Vector2Int>)coords) alteredCoords.Add(new Vector3Int(c.x, 0, c.y));
                        simObjectRenders.TryAdd((so.Type, so.Class_name, so.Id), SimSpaceSystem.CreateSimObjectRender(so.Type, so.Class_name, so.Id, GetSimObjectSO(so.Type, so.Class_name), orientation, PlaceableObject.Dir.Down, simSpacePosition, alteredCoords));
                    }
                    else
                    {

                    }
                }
                else
                {
                    if (simDimensions.Count == 2)
                    {

                    }
                    else
                    {

                    }
                }
            }
        }
    }
    public void StepPheromones(ICollection<SimObject> pheromones)
    {
        float[,] f_cells = new float[(int)width,(int)width];
        float[,] h_cells = new float[(int)width,(int)width];

        int fn = 0;

        foreach (SimObject so in pheromones)
        {
            so.Parameters.TryGetValue("position", out object coords);
            so.Parameters.TryGetValue("intensity", out object intensity);
            if (isDiscrete)
            {
                if (simDimensions.Count == 2)
                {
                    if (so.Class_name.Contains("Food"))
                    {
                        foreach (Vector2Int c in (MyList<Vector2Int>)coords)
                        {
                            f_cells[c.x, c.y] = (float)intensity;
                            fn++;
                        }
                    }
                    else
                    {
                        foreach (Vector2Int c in (MyList<Vector2Int>)coords)
                        {
                            h_cells[c.x, c.y] = (float)intensity;
                        }
                    }
                }
            }
        }
        simulationSpace.GetComponent<Renderer>().sharedMaterial.SetFloat("_Width", (int)width);

        simulationSpace.GetComponent<ShaderManager>().computeBuffers[0].SetCounterValue(0);
        simulationSpace.GetComponent<ShaderManager>().computeBuffers[1].SetCounterValue(0);
        simulationSpace.GetComponent<ShaderManager>().computeBuffers[0].SetData(f_cells);
        simulationSpace.GetComponent<ShaderManager>().computeBuffers[1].SetData(h_cells);

        //Debug.Log("Pheromones to Food: " + fn);
        //Debug.Log("Pheromones to Home: " + (pheromones.Count - fn));

    }

    // Inspector
    public void PopulateInspector()
    {
        // FARE CON EVENTI
    }
    public void ShowHideInspector(bool show)
    {
        PopulateInspector();
        // FARE CON EVENTI
    }

    // Utils
    public int GetTemporaryId(SimObject.SimObjectType type, string class_name)
    {
        int min_id = -1;
        foreach (KeyValuePair<(string op, (SimObject.SimObjectType type, string class_name, int id) obj), SimObject> update in SimulationController.uncommitted_updates)
        {
            if (update.Key.op.Equals("CRT") && update.Key.obj.type.Equals(type) && update.Key.obj.class_name.Equals(class_name))
            {
                if (min_id > update.Key.obj.id) min_id = update.Key.obj.id;
            }
        }
        return --min_id;
    }
    public SimObjectSO GetSimObjectSO(SimObject.SimObjectType type, string class_name)
    {
        switch (type)
        {
            case SimObject.SimObjectType.AGENT:
                return SimObjectsData.agents[SimObjectsData.agentClass_names.IndexOf(class_name)];
            case SimObject.SimObjectType.GENERIC:
                return SimObjectsData.generics[SimObjectsData.genericClass_names.IndexOf(class_name)];
            case SimObject.SimObjectType.OBSTACLE:
                return SimObjectsData.obstacles[SimObjectsData.obstacleClass_names.IndexOf(class_name)];
        }
        return null;
    }
    public ConcurrentDictionary<string, object> GetSimObjectParams(SimObject.SimObjectType type, string class_name)
    {
        ConcurrentDictionary<string, SimObject> dict;
        ConcurrentDictionary<string, object> parameters = new ConcurrentDictionary<string, object>();
        switch (type)
        {
            case SimObject.SimObjectType.AGENT:
                dict = SimulationController.GetSimulation().Agent_prototypes;
                break;
            case SimObject.SimObjectType.GENERIC:
                dict = SimulationController.GetSimulation().Generic_prototypes;
                break;
            default:
                dict = null;
                break;
        }
        if (dict != null)
        {
            dict.TryGetValue(class_name, out SimObject so);
            foreach (KeyValuePair<string, object> p in so.Parameters)
            {
                if (!p.Key.Equals("position")) parameters.TryAdd(p.Key, p.Value);
            }
        }
        return parameters;
    }
    public PlaceableObject getPlaceableObjectRecursive(Transform hitTransform)
    {
        if (hitTransform.parent.gameObject.GetComponent<PlaceableObject>() != null)
        {
            return hitTransform.parent.gameObject.GetComponent<PlaceableObject>();
        }
        else return getPlaceableObjectRecursive(hitTransform.parent.transform);
    } 
}
