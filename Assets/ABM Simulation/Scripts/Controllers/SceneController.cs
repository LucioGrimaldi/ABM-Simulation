using SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static GridSystem;


public class SceneController : MonoBehaviour
{
    [Serializable]
    public class NamedPrefab
    {
        public string name;
        public GameObject prefab;
    }

    // Set in Inspector
    [SerializeField] private PlayerPreferencesSO playerPreferencesSO;
    [SerializeField] public List<PO_Prefab_Collection> PO_Prefab_Collection;

    // Events
    public static event EventHandler<EventArgs> OnLoadSimulationSceneEventHandler;
    public static event EventHandler<SimObjectCreateEventArgs> OnSimObjectCreateEventHandler;    
    public static event EventHandler<SimObjectModifyEventArgs> OnSimObjectModifyEventHandler;    
    public static event EventHandler<SimObjectDeleteEventArgs> OnSimObjectDeleteEventHandler;

    // UI Action Queue
    public static readonly ConcurrentQueue<Action> SceneControllerThreadQueue = new ConcurrentQueue<Action>();

    private static bool showSimSpace, showEnvironment;
    private GameObject simulationSpace, visualEnvironment;

    // Controllers
    private SimulationController SimulationController;
    private UIController UIController;
    public SimSpaceSystem SimSpaceSystem;

    // Prefabs
    public GameObject[] SimSpace_prefabs_2D;
    public GameObject[] SimSpace_prefabs_3D;
    public GameObject[] Environment_prefabs;
    public Shader[] Shaders2D;
    public Shader[] Shaders3D;

    // Variables
    public static int simId;
    private static bool isDiscrete;
    private static Simulation.SimTypeEnum simType;
    private static ConcurrentDictionary<string, int> simDimensions;
    private int width = 1, height = 1, length = 1;
    public static PlaceableObject selectedGhost = null;
    public static PlaceableObject selectedPlaced = null;
    public AudioSource audioPlacedSound;

    /// UNITY LOOP METHODS ///

    /// <summary>
    /// We use Awake to bootstrap App
    /// </summary>
    private void Awake()
    {
        // Ignore collisions for performance
        Physics.IgnoreLayerCollision(9, 9, true);
        Physics.IgnoreLayerCollision(9, 10, true);

        // Bind Controllers
        SimulationController = GameObject.Find("SimulationController").GetComponent<SimulationController>();
        UIController = GameObject.Find("UIController").GetComponent<UIController>();

        // Trigger SimController to pass references
        OnLoadSimulationSceneEventHandler?.Invoke(this, new EventArgs());

        // Init Scene
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
        Simulation.OnSimObjectNotInStepEventHandler += onSimObjectNotInStep;
        UIController.OnSimObjectParamsUpdateEventHandler += onSimObjectModified;
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
        if (!SceneControllerThreadQueue.IsEmpty)
        {
            while (SceneControllerThreadQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        LockUp();
        if (SimulationController.GetSimState().Equals(Simulation.StateEnum.PLAY)) StepUp();
        if (SimulationController.GetSimState().Equals(Simulation.StateEnum.STEP)) { StepUp(); if(SimulationController.Steps_to_consume==0) SimulationController.GetSimState().Equals(Simulation.StateEnum.PAUSE); }
        ShowHideSimEnvironment();      // può essere sostituito con event
        CheckForUserInput();
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    private void OnApplicationQuit()
    {
        while (SceneControllerThreadQueue.TryDequeue(out _)) ;
    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {
        // Unregister to EventHandlers
        Simulation.OnSimObjectNotInStepEventHandler -= onSimObjectNotInStep;
        UIController.OnSimObjectParamsUpdateEventHandler -= onSimObjectModified;
    }
    /// <summary>
    /// onDestroy routine (Unity Process)
    /// </summary>
    private void OnDestroy()
    {
        while (SceneControllerThreadQueue.TryDequeue(out _)) ;
    }


    /// Methods ///
    // Initialization
    public void InitSimSpaceSystem()
    {
        if (isDiscrete)
        {
            SimSpaceSystem = new GameObject("SimSpaceSystem").AddComponent<GridSystem>();
            SimSpaceSystem.simSpaceType = SimSpaceSystem.SimSpaceTypeEnum.DISCRETE;
        }
        else
        {
            SimSpaceSystem = new GameObject("SimSpaceSystem").AddComponent<ContinuousSystem>();
            SimSpaceSystem.simSpaceType = SimSpaceSystem.SimSpaceTypeEnum.CONTINUOUS;
        }
        if (simDimensions.Count == 2) SimSpaceSystem.simSpaceDimensions = SimSpaceSystem.SimSpaceDimensionsEnum._2D;
        else SimSpaceSystem.simSpaceDimensions = SimSpaceSystem.SimSpaceDimensionsEnum._3D;
    }
    public void InitScene()
    {
        float scaleFactor;

        // get sim dimensions (and add z in case)
        if (simDimensions.ContainsKey("x")) simDimensions.TryGetValue("x", out width);
        if (simDimensions.ContainsKey("y")) simDimensions.TryGetValue("y", out length);      // swap y-z
        if (simDimensions.ContainsKey("z")) simDimensions.TryGetValue("z", out height);
        if (isDiscrete) { scaleFactor = Mathf.Max((int)width, (int)height, (int)length) / 10f; }
        else { scaleFactor = Mathf.Max((int)width, (int)height, (int)length) / 10f; }

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
            ((GridSystem)SimSpaceSystem).grid = new Grid3D<GridObject>((int)width, (int)height, (int)length, 10f / scaleFactor, choosenSimSpace.transform.position - new Vector3(50, 0, 50), ((g, x, y, z) => new GridObject(g, x, y, z)));
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
            ContinuousSystem.width = width;
            ContinuousSystem.height = height;
            ContinuousSystem.length = length;
            ContinuousSystem.simSpace = simulationSpace;
        }
    }
    public void InitShader()
    {
        simulationSpace.GetComponent<Renderer>().material.shader = Shaders2D[simId];
        simulationSpace.AddComponent<ShaderManager>();
        simulationSpace.GetComponent<Renderer>().sharedMaterial.SetFloat("_GridSize", Mathf.Max((int)width, (int)height, (int)length));
        if(simId == 1)
        {
            simulationSpace.GetComponent<ShaderManager>().computeBuffers = new ComputeBuffer[2];
            simulationSpace.GetComponent<ShaderManager>().computeBuffers[0] = new ComputeBuffer((int)width * (int)width, sizeof(float), ComputeBufferType.Append);          // food
            simulationSpace.GetComponent<ShaderManager>().computeBuffers[1] = new ComputeBuffer((int)width * (int)width, sizeof(float), ComputeBufferType.Append);          // home
            simulationSpace.GetComponent<ShaderManager>().computeBuffers[0].SetData(new float[(int)width * (int)width]);
            simulationSpace.GetComponent<ShaderManager>().computeBuffers[1].SetData(new float[(int)width * (int)width]);
            simulationSpace.GetComponent<Renderer>().sharedMaterial.SetBuffer("_FoodGrid", simulationSpace.GetComponent<ShaderManager>().computeBuffers[0]);
            simulationSpace.GetComponent<Renderer>().sharedMaterial.SetBuffer("_HomeGrid", simulationSpace.GetComponent<ShaderManager>().computeBuffers[1]);
        }
    }
    public void ResetSimSpace()
    {
        simDimensions = SimulationController.GetSimDimensions();
     
        float scaleFactor;

        // get sim dimensions (and add z in case)
        if (simDimensions.ContainsKey("x")) simDimensions.TryGetValue("x", out width);
        if (simDimensions.ContainsKey("y")) simDimensions.TryGetValue("y", out length);      // swap y-z
        if (simDimensions.ContainsKey("z")) simDimensions.TryGetValue("z", out height);
        if (isDiscrete) { scaleFactor = Mathf.Max((int)width, (int)height, (int)length) / 10f; }
        else { scaleFactor = Mathf.Max((int)width, (int)height, (int)length) / 10f; }

        ClearSimSpace();

        if (isDiscrete)
        {
            if(simDimensions.Count == 2) InitShader();
            ((GridSystem)SimSpaceSystem).grid = new Grid3D<GridObject>((int)width, (int)height, (int)length, 10f / scaleFactor, simulationSpace.transform.position - new Vector3(50, 0, 50), ((g, x, y, z) => new GridObject(g, x, y, z)));
        }
        else
        {
            ContinuousSystem.width = width;
            ContinuousSystem.height = height;
            ContinuousSystem.length = length;
            ContinuousSystem.simSpace = simulationSpace;
        }
    }
    public void ClearSimSpace()
    {
        SceneControllerThreadQueue.Enqueue(() => {
            foreach ((bool isGhost, PlaceableObject g) x in SimSpaceSystem.GetTemporaryGhosts().Values) x.g.Destroy();
            foreach ((bool isGhost, PlaceableObject o) x in SimSpaceSystem.GetPlacedObjects().Values) x.o.Destroy();
            if(isDiscrete && simDimensions.Count == 2)
            {
                simulationSpace.GetComponent<ShaderManager>().computeBuffers[0].SetData(new float[(int)width * (int)width]);
                simulationSpace.GetComponent<ShaderManager>().computeBuffers[1].SetData(new float[(int)width * (int)width]);
            }
            SimSpaceSystem.ClearSimSpaceSystem();
        });
    }

    // Interaction
    public void SelectGhost(int type, int id)
    {
        RefreshSelectedGhost(GetSimObjectPrototype((SimObject.SimObjectType)type, id), GetPlaceableObjectPrefab((SimObject.SimObjectType)type, id));
    }
    public void ShowHideSimEnvironment()
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
    }      // FARE CON EVENTI
    public void CheckForUserInput()
    {
        // Select/Spawn
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hitPoint;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hitPoint, Mathf.Infinity))            // Check if UI is not hit
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) { }
                else if (selectedGhost != null && SimulationController.admin)
                {
                    if (SimSpaceSystem.CanBuild(selectedGhost))
                    {
                        if (selectedPlaced != null) selectedPlaced.DeHighlight();
                        selectedPlaced = selectedGhost;
                        selectedPlaced.PlaceGhost(SimSpaceSystem.MouseClickToSpawnPosition(selectedPlaced));
                        CreateSimObject(selectedPlaced);
                        if (selectedPlaced != null) selectedPlaced.Highlight();
                        ShowInspector(selectedPlaced);
                        selectedGhost = CreateGhost(GetSimObjectPrototype(selectedGhost.SimObject.Type, selectedGhost.SimObject.Class_name), GetPlaceableObjectPrefab(selectedGhost.SimObject.Type, selectedGhost.SimObject.Class_name), true);
                        selectedGhost.SimObject.Id = GetTemporaryId(selectedGhost.SimObject.Type, selectedGhost.SimObject.Class_name);
                        audioPlacedSound.Play();
                    }
                }
                else if (SelectSimObject(hitPoint)) { Debug.Log("SELECTED:" + selectedPlaced.SimObject.Type + " " + selectedPlaced.SimObject.Class_name + " " + selectedPlaced.SimObject.Id + (selectedPlaced.IsGhost ? " -unconfirmed-" : "")); }
                else
                {
                    DeselectSimObject();
                    UIController.followToggle.GetComponent<Toggle>().isOn = false;
                    UIController.selected = null;
                    UIController.OnFollowToggleClicked();
                }
            }
            else
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) { }
                else
                {
                    DeselectSimObject();
                    UIController.followToggle.GetComponent<Toggle>().isOn = false;
                    UIController.selected = null;
                    UIController.OnFollowToggleClicked();
                }
            }
        }
        // Destroy
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (selectedPlaced != null && SimulationController.admin && (SimulationController.GetSimState().Equals(Simulation.StateEnum.PAUSE) || SimulationController.GetSimState().Equals(Simulation.StateEnum.READY)))
            {                
                PlacedObjectToGhost(selectedPlaced);
                DeleteSimObject(selectedPlaced);
            }
        }
        // Modify

        /// <summary>
        /// MODIFY 
        ///if (Input.GetKeyDown(KeyCode.M))
        ///{
        ///    if(selectedSimObject != null)
        ///    {
        ///        selectedSimObject.SetMovable(true);
        ///    }
        ///    String buildName;
        ///    Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
        ///    if (GridSystem.grid.GetGridObject(mousePosition) != null)
        ///    {
        ///        // Valid Grid Position
        ///        PlaceableObject placedObject = GridSystem.grid.GetGridObject(mousePosition).GetPlacedObject();
        ///        if (placedObject != null)
        ///        {
        ///            buildName = placedObject.name;
        ///            //Debug.Log("Nome prefab: " + buildName);
        ///            // Distruggi
        ///            placedObject.Destroy();
        ///            List<Vector3Int> gridPositionList = placedObject.GetGridPositionList();
        ///            foreach (Vector3Int gridPosition in gridPositionList)
        ///            {
        ///                GridSystem.grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).ClearPlacedObject();
        ///            }
        ///            foreach (SimObjectDiscreteSO scriptableObj in GridSystem.placeableAgentSos)
        ///            {
        ///                if (buildName.Contains(scriptableObj.name))
        ///                {
        ///                    GridSystem.selectedPlaceableObject = scriptableObj;
        ///                    UtilsClass.CreateWorldTextPopup("Moving Building", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);
        ///                    GridSystem.RefreshSelectedSimObject();
        ///                    //DeselectObjectType();
        ///                    break;
        ///                }
        ///            }
        ///        }
        ///        else
        ///            UtilsClass.CreateWorldTextPopup("Nothing to MOVE!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);
        ///    }
        ///    else
        ///        //do nothing
        ///        UtilsClass.CreateWorldTextPopup("Nothing to MOVE!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);
        ///}
        /// </summary>

        // Follow
        if (Input.GetKeyDown(KeyCode.Space))
        { 
            if (UIController.showInspectorPanel)
            {
                UIController.followToggle.GetComponent<Toggle>().isOn = !UIController.followToggle.GetComponent<Toggle>().isOn;
                UIController.OnFollowToggleClicked();
            }
        }
        // Rotate
        if (Input.GetKeyDown(KeyCode.R)) { if ((selectedPlaced != null || selectedGhost != null) && (SimulationController.GetSimState().Equals(Simulation.StateEnum.PAUSE) || SimulationController.GetSimState().Equals(Simulation.StateEnum.READY))) SimSpaceSystem.RotatePlacedObject((selectedGhost != null) ? selectedGhost : selectedPlaced); }
        // Deselect
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (selectedGhost != null) {
                DeleteGhost();
                selectedGhost = null;
                if (UIController.followToggle.GetComponent<Toggle>().isOn && UIController.selected.Equals(selectedGhost))
                {
                    UIController.followToggle.GetComponent<Toggle>().isOn = false;
                    UIController.selected = null;
                    UIController.OnFollowToggleClicked();
                }
            } 
            if(selectedPlaced != null) { 
                DeselectSimObject();
                if (UIController.followToggle.GetComponent<Toggle>().isOn && UIController.selected.Equals(selectedPlaced))
                {
                    UIController.followToggle.GetComponent<Toggle>().isOn = false;
                    UIController.selected = null;
                    UIController.OnFollowToggleClicked();
                }
            }
        }
    }
    public bool SelectSimObject(RaycastHit hitPoint)
    {
        if(hitPoint.transform.gameObject.layer.Equals(9) || hitPoint.transform.gameObject.layer.Equals(10))
        {
            DeselectSimObject();
            selectedPlaced = GetPOFromTransformRecursive(hitPoint.transform);
            selectedPlaced.IsSelected = true;
            UIController.followToggle.GetComponent<Toggle>().interactable = true;
            UIController.OnChangeSelectedFollow();
            if (selectedPlaced != null) selectedPlaced.Highlight();
            ShowInspector(selectedPlaced);
            return true;
        }
        return false;
    }
    public void DeselectSimObject()
    {
        UIController.followToggle.GetComponent<Toggle>().interactable = false;
        UIController.EmptyInspectorParams();
        UIController.tempSimObjectParams.Clear();
        HideInspector();
        if(selectedPlaced != null)
        {
            selectedPlaced.IsSelected = false;
            selectedPlaced.DeHighlight();
            selectedPlaced = null;
            UIController.selected = null;
        }
    }
    public void RefreshSelectedGhost(SimObject so, PlaceableObject po)
    {
        DeleteGhost();
        selectedGhost = CreateGhost(so, po, true);
        ShowInspector(selectedGhost);
    }

    // Inspector
    public void ShowInspector(PlaceableObject po)
    {
        PopulateInspector(po);
        if (!UIController.showInspectorPanel) UIController.ShowHidePanelInspector();
    }
    public void HideInspector()
    {
        if (UIController.showInspectorPanel) UIController.ShowHidePanelInspector();
    }
    public void PopulateInspector(PlaceableObject po)
    {
        UIController.PopulateInspector(po);
    }
    public void EmptyInspector()
    {
        UIController.EmptyInspectorInfos();
        UIController.EmptyInspectorParams();
    }

    // PlacedObjects
    public PlaceableObject CreateGhost(SimObject simObject, PlaceableObject ghost, bool isMovable)
    {
        return SimSpaceSystem.CreateGhost(simObject, ghost, isMovable);
    }
    public void DeleteGhost()
    {
        if(selectedGhost != null)
        {
            SimSpaceSystem.DeleteSimObject(selectedGhost);
            selectedGhost.Destroy();
            selectedGhost = null;
        }
    }
    public void DeleteTempGhost(SimObject.SimObjectType type, String class_name, int id)
    {
        if (SimSpaceSystem.GetTemporaryGhosts().TryRemove((type, class_name, id), out (bool, PlaceableObject) x))
        {
            if (UIController.followToggle.GetComponent<Toggle>().isOn && UIController.selected.Equals(x.Item2))
            {
                UIController.followToggle.GetComponent<Toggle>().isOn = false;
                UIController.selected = null;
                UIController.OnFollowToggleClicked();
            }
            DeselectSimObject();
            SimSpaceSystem.DeleteSimObject(x.Item2);
            SceneControllerThreadQueue.Enqueue(() => { x.Item2.Destroy(); });
        }
    }
    public void ConfirmTempGhosts()
    {
        SimSpaceSystem.GetTemporaryGhosts().ToList().ForEach((element) => { if (element.Value.isGhost) { SimSpaceSystem.GetPlacedObjects().TryRemove((element.Key.type, element.Key.class_name, element.Key.id), out (bool isGhost, PlaceableObject po) x); x.po.Confirm(); }});
    }
    public void DeleteTempGhosts()
    {
        SimSpaceSystem.GetTemporaryGhosts().ToList().ForEach((element) => { SimSpaceSystem.GetTemporaryGhosts().TryRemove((element.Key.type, element.Key.class_name, element.Key.id), out (bool isGhost, PlaceableObject po) x); x.po.Destroy();});
    }
    public PlaceableObject CreatePlaceableObject(SimObject simObject, PlaceableObject toPlace, bool isMovable)
    {
        return SimSpaceSystem.CreateSimObject(simObject, toPlace, isMovable);
    }
    public void RotatePlacedObject(PlaceableObject toRotate)
    {
        SimSpaceSystem.RotatePlacedObject(toRotate);
    }
    public void DeletePlacedObject(PlaceableObject toDelete)
    {
        if (SimSpaceSystem.GetPlacedObjects().TryRemove((toDelete.SimObject.Type, toDelete.SimObject.Class_name, toDelete.SimObject.Id), out (bool, PlaceableObject) x))
        {
            if (UIController.followToggle.GetComponent<Toggle>().isOn && UIController.selected.Equals(x.Item2))
            {
                UIController.followToggle.GetComponent<Toggle>().isOn = false;
                UIController.selected = null;
                UIController.OnFollowToggleClicked();
            }
            DeselectSimObject();
            SimSpaceSystem.DeleteSimObject(x.Item2);
            SceneControllerThreadQueue.Enqueue(() => { x.Item2.Destroy(); });
        }
    }
    public void PlacedObjectToGhost(PlaceableObject toMakeGhost)
    {
        if(toMakeGhost != null)
        {
            toMakeGhost.MakeGhost(false);
            if (toMakeGhost.SimObject.To_keep_if_absent)
            {
                toMakeGhost.SimObject.To_keep_if_absent = false;
                SimulationController.GetSimulation().ToDeleteIfAbsent.Add((toMakeGhost.SimObject.Type, toMakeGhost.SimObject.Class_name, toMakeGhost.SimObject.Id));
                SimulationController.GetSimulation().ToKeepIfAbsent.Remove((toMakeGhost.SimObject.Type, toMakeGhost.SimObject.Class_name, toMakeGhost.SimObject.Id));
                SimulationController.GetSimulation().Temp2.Remove((toMakeGhost.SimObject.Type, toMakeGhost.SimObject.Class_name, toMakeGhost.SimObject.Id));
            }
        }
    }

    // SimObjects
    public void CreateSimObject(PlaceableObject po)
    {
        if(po != null)
        {
            SimObjectCreateEventArgs e = new SimObjectCreateEventArgs();
            e.type = po.SimObject.Type;
            e.class_name = po.SimObject.Class_name;
            e.id = po.SimObject.Id;
            e.parameters = po.SimObject.Parameters;
            OnSimObjectCreateEventHandler?.BeginInvoke(this, e, null, null);
        }
    }
    public void ModifySimObject(SimObjectParamsUpdateEventArgs e2)
    {
        SimObjectModifyEventArgs e = new SimObjectModifyEventArgs();
        e.type = e2.type;
        e.class_name = e2.class_name;
        e.id = e2.id;
        e.parameters = e2.parameters;
        OnSimObjectModifyEventHandler?.BeginInvoke(this, e, null, null);
    }
    public void DeleteSimObject(PlaceableObject po)
    {
        SimObjectDeleteEventArgs e = new SimObjectDeleteEventArgs();
        e.type = po.SimObject.Type;
        e.class_name = po.SimObject.Class_name;
        e.id = po.SimObject.Id;
        OnSimObjectDeleteEventHandler?.BeginInvoke(this, e, null, null);
    }

    // Event Handles
    public void onSimObjectNotInStep(object sender, SimObjectDeleteEventArgs e)
    {
        if (SimSpaceSystem.GetPlacedObjects().TryRemove((e.type, e.class_name, e.id), out (bool, PlaceableObject) x))
        {
            SceneControllerThreadQueue.Enqueue(() => {
                UIController.followToggle.GetComponent<Toggle>().isOn = false;
                UIController.selected = null;
                UIController.OnFollowToggleClicked();
                EmptyInspector();
                HideInspector();
                SimSpaceSystem.DeleteSimObject(x.Item2);
                x.Item2.Destroy(); 
            });
        }
    }
    public void onSimObjectModified(object sender, SimObjectParamsUpdateEventArgs e)
    {
        ModifySimObject(e);
    }
    // Step
    public void StepUp()
    {
        UpdatePlacedObjects(SimSpaceSystem.GetPlacedObjects(), true);
        if(SimSpaceSystem.simSpaceDimensions.Equals(SimSpaceSystem.SimSpaceDimensionsEnum._2D)) UpdatePheromones(SimulationController.GetSimulation().Generics.Values.Where((g) => { if (g.Class_name.Contains("Pheromone")) return true; else return false; }).ToArray());
    }
    public void LockUp()
    {
        LockPlacedObjects(SimSpaceSystem.GetPlacedObjects());
    }
    public void UpdatePlacedObjects(ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> placedObjects, bool movable)
    {
        SimObject[] a = SimulationController.GetSimulation().Agents.Values.ToArray();
        SimObject[] g = SimulationController.GetSimulation().Generics.Values.ToArray();
        SimObject[] o = SimulationController.GetSimulation().Obstacles.Values.ToArray();

        foreach (SimObject so in a)
        {
            if (placedObjects.ContainsKey((so.Type, so.Class_name, so.Id))) { if (!placedObjects[(so.Type, so.Class_name, so.Id)].po.IsGhost) placedObjects[(so.Type, so.Class_name, so.Id)].po.IsMovable = movable; }
            else
            {
                PlaceableObject _prefab = GetPlaceableObjectPrefab(so.Type, so.Class_name);
                PlaceableObject _old = GetGhostToReplace(so);
                PlaceableObject _new = CreatePlaceableObject(so, _prefab, movable);
                if (_old != null)
                {
                    DeleteTempGhost(so.Type, so.Class_name, _old.SimObject.Id);
                    EmptyInspector();
                    SimSpaceSystem.CopyRotation(_old, _new);
                }
            }
        }
        foreach (SimObject so in g)
        {
            if (!placedObjects.ContainsKey((so.Type, so.Class_name, so.Id)))
            {
                PlaceableObject _prefab = GetPlaceableObjectPrefab(so.Type, so.Class_name);
                PlaceableObject _old = GetGhostToReplace(so);
                PlaceableObject _new = CreatePlaceableObject(so, _prefab, movable);
                if (_old != null)
                {
                    DeleteTempGhost(so.Type, so.Class_name, _old.SimObject.Id);
                    EmptyInspector();
                    SimSpaceSystem.CopyRotation(_old, _new);
                }
            }
        }
        foreach (SimObject so in o) {
            if (!placedObjects.ContainsKey((so.Type, so.Class_name, so.Id)))
            {
                PlaceableObject _prefab = GetPlaceableObjectPrefab(so.Type, so.Class_name);
                PlaceableObject _old = GetGhostToReplace(so);
                PlaceableObject _new = CreatePlaceableObject(so, _prefab, movable);
                if (_old != null)
                {
                    DeleteTempGhost(so.Type, so.Class_name, _old.SimObject.Id);
                    EmptyInspector();
                    SimSpaceSystem.CopyRotation(_old, _new);
                }                
            }
        }
    }
    public void LockPlacedObjects(ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> placedObjects)
    {
        foreach ((bool isGhost, PlaceableObject po) entry in placedObjects.Values)
        {
           entry.po.IsMovable = false;
        }
    }
    public void UpdatePheromones(ICollection<SimObject> pheromones)
    {
        float[,] f_cells = new float[(int)width,(int)width];
        float[,] h_cells = new float[(int)width,(int)width];

        int fn = 0;

        foreach (SimObject so in pheromones)
        {
            so.Parameters.TryGetValue("position", out object coords);
            while(coords == null) so.Parameters.TryGetValue("position", out coords);
            so.Parameters.TryGetValue("intensity", out object intensity);
            while(intensity == null) so.Parameters.TryGetValue("intensity", out intensity);
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
    }

    // Utils
    public PlaceableObject GetGhostToReplace(SimObject so)
    {
        return SimSpaceSystem.GetGhostFromSO(so);
    }
    public PlaceableObject GetPlaceableObjectPrefab(SimObject.SimObjectType type, int id)
    {
        switch (type)
        {
            case SimObject.SimObjectType.AGENT:
                return PO_Prefab_Collection[simId].PO_AgentPrefabs[id].prefab.GetComponent<PlaceableObject>();
            case SimObject.SimObjectType.GENERIC:
                return PO_Prefab_Collection[simId].PO_GenericPrefabs[id].prefab.GetComponent<PlaceableObject>();
            case SimObject.SimObjectType.OBSTACLE:
                return PO_Prefab_Collection[simId].PO_ObstaclePrefabs[id].prefab.GetComponent<PlaceableObject>();
            default:
                return null;
        }
    }
    public PlaceableObject GetPlaceableObjectPrefab(SimObject.SimObjectType type, string class_name)
    {
        switch (type)
        {
            case SimObject.SimObjectType.AGENT:
                return PO_Prefab_Collection[simId].PO_AgentPrefabs.Find(p => p.name.Equals(class_name)).prefab.GetComponent<PlaceableObject>();
            case SimObject.SimObjectType.GENERIC:
                return PO_Prefab_Collection[simId].PO_GenericPrefabs.Find(p => p.name.Equals(class_name)).prefab.GetComponent<PlaceableObject>();
            case SimObject.SimObjectType.OBSTACLE:
                return PO_Prefab_Collection[simId].PO_ObstaclePrefabs.Find(p => p.name.Equals(class_name)).prefab.GetComponent<PlaceableObject>();
            default:
                return null;
        }
    }
    public SimObject GetSimObjectPrototype(SimObject.SimObjectType type, int id)
    {
        string class_name;
        switch (type)
        {
            case SimObject.SimObjectType.AGENT:
                class_name = PO_Prefab_Collection[simId].PO_AgentPrefabs[id].name;
                SimObject a = SimulationController.GetSimulation().Agent_prototypes[class_name];
                a.Id = GetTemporaryId(a.Type, a.Class_name);
                return a;
            case SimObject.SimObjectType.GENERIC:
                class_name = PO_Prefab_Collection[simId].PO_GenericPrefabs[id].name;
                SimObject g = SimulationController.GetSimulation().Generic_prototypes[class_name];
                g.Id = GetTemporaryId(g.Type, g.Class_name);
                return g;
            case SimObject.SimObjectType.OBSTACLE:
                class_name = PO_Prefab_Collection[simId].PO_ObstaclePrefabs[id].name;
                SimObject o = SimulationController.GetSimulation().Obstacle_prototypes[class_name];
                o.Id = GetTemporaryId(o.Type, o.Class_name);
                return o;
            default:
                return null;
        }
    }
    public SimObject GetSimObjectPrototype(SimObject.SimObjectType type, string class_name)
    {
        switch (type)
        {
            case SimObject.SimObjectType.AGENT:
                SimObject a = SimulationController.GetSimulation().Agent_prototypes[class_name];
                a.Id = GetTemporaryId(a.Type, a.Class_name);
                return a;
            case SimObject.SimObjectType.GENERIC:
                SimObject g = SimulationController.GetSimulation().Generic_prototypes[class_name];
                g.Id = GetTemporaryId(g.Type, g.Class_name);
                return g;
            case SimObject.SimObjectType.OBSTACLE:
                SimObject o = SimulationController.GetSimulation().Obstacle_prototypes[class_name];
                o.Id = GetTemporaryId(o.Type, o.Class_name);
                return o;
            default:
                return null;
        }
    }
    public JSONArray GetSimObjectParamsPrototype(SimObject.SimObjectType type, string class_name)
    {
        switch (type)
        {
            case SimObject.SimObjectType.AGENT:
                return (JSONArray)((JSONObject)((JSONArray)((JSONObject)SimulationController.sim_list_editable[simId])["agent_prototypes"]).Linq.Where((node) => node.Value["class"].Equals(class_name)).ToArray()[0])["params"];
            case SimObject.SimObjectType.GENERIC:
                return (JSONArray)((JSONObject)((JSONArray)((JSONObject)SimulationController.sim_list_editable[simId])["generic_prototypes"]).Linq.Where((node) => node.Value["class"].Equals(class_name)).ToArray()[0])["params"];
            case SimObject.SimObjectType.OBSTACLE:
                return (JSONArray)((JSONObject)((JSONArray)((JSONObject)SimulationController.sim_list_editable[simId])["obstacle_prototypes"]).Linq.Where((node) => node.Value["class"].Equals(class_name)).ToArray()[0])["params"];
            default:
                return null;
        }
    }
    public PlaceableObject GetPOFromTransformRecursive(Transform hitTransform)
    {
        if (hitTransform.parent.gameObject.GetComponent<PlaceableObject>() != null)
        {
            return hitTransform.parent.gameObject.GetComponent<PlaceableObject>();
        }
        else return GetPOFromTransformRecursive(hitTransform.parent.transform);
    } 
    public int GetTemporaryId(SimObject.SimObjectType type, string class_name)
    {
        int min_id = -1;
        SimSpaceSystem.GetTemporaryGhosts().ToList().ForEach((element) =>
        {
            if (element.Key.id < 0 && element.Key.type.Equals(type) && element.Key.class_name.Equals(class_name)) min_id = Math.Min(min_id, element.Key.id);
        });
        return --min_id;
    }
}
