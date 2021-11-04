using System.Collections.Generic;
using UnityEngine;
using GerardoUtils;

public class GridSystem : MonoBehaviour, SimSpaceSystem
{
    public PlaceableObject selectedPlaceableObject = null;
    public static GridSystem Instance { get; private set; }
    public Grid3D<GridObject> grid;
    public GameObject DEBUG_cell;
    private bool DEBUG = false;

    public GridSystem()
    {
        Instance = this;
    }

    public class GridObject
    {
        private Grid3D<GridObject> grid;
        private int x;
        private int y;
        private int z;
        private PlaceableObject placedObject = null;

        public GridObject(Grid3D<GridObject> grid, int x, int y, int z)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        //Set e Get transform
        public void SetPlacedObject(PlaceableObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, y, z); //aggiorno ToString se c'è modifica
        }
        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, y, z);
        }
        public PlaceableObject GetPlacedObject()
        {
            if (placedObject != null)
                return placedObject;
            else return null;
        }
        public bool CanBuild()
        {
            return placedObject == null;   //se è null posso costruire, altrimenti no

        }
        public override string ToString()
        {
            return x + "," + y + "," + z + "\n" + placedObject;
        }

    }


    /// <summary>
    /// We use Awake to bootstrap App
    /// </summary>
    private void Awake()
    {
       
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    private void OnEnable()
    {
        // Register to EventHandlers
        SceneController.OnSelectChangedEventHandler += OnSelectedChanged;
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
        
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    private void OnApplicationQuit()
    {

    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {
        // Unregister to EventHandlers
        SceneController.OnSelectChangedEventHandler -= OnSelectedChanged;
    }


    // Interface Methods
    public bool IsGhostSelected()
    {
        return selectedPlaceableObject != null && selectedPlaceableObject.isGhost;
    }
    public void SpawnGhost(SimObject.SimObjectType type, string class_name, SimObjectSO so)
    {
        selectedPlaceableObject = PlaceableObject.SpawnVisual(type, class_name, 0, so, Vector3.zero, new MyList<Vector3Int>(), new Quaternion(0,0,0,0), PlaceableObject.Dir.Down, true, false);
        selectedPlaceableObject.SetScale(grid.CellSize);
        selectedPlaceableObject.SetMovable(true);
        SetLayerRecursive(selectedPlaceableObject.gameObject, 10);
    }
    public void RemoveGhost()
    {
        if (selectedPlaceableObject != null)
        {
            selectedPlaceableObject.DestroyVisual();
            selectedPlaceableObject = null;
        }
    }    
    public PlaceableObject CreateSimObjectRender(SimObject.SimObjectType type, string class_name, int id, SimObjectSO obj, Quaternion orientation, PlaceableObject.Dir direction, Vector3 simSpacePosition, dynamic gridPositionList)
    {
        simSpacePosition = grid.GetWorldPosition((int)simSpacePosition.x, (int)simSpacePosition.y, (int)simSpacePosition.z);
        PlaceableObject newPlacedObject = PlaceableObject.SpawnVisual(type, class_name, id, obj, simSpacePosition, gridPositionList, orientation, direction, false, true);
        newPlacedObject.SetScale(grid.CellSize);
        SetLayerRecursive(newPlacedObject.gameObject, 9);
        foreach (Vector3Int gridPosition in gridPositionList)
        {
            grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).SetPlacedObject(newPlacedObject);
            if (DEBUG)
            {
                GameObject x = Instantiate(DEBUG_cell, new Vector3(gridPosition.x, gridPosition.y, gridPosition.z) * grid.CellSize, Quaternion.identity);
                x.transform.parent = newPlacedObject.currentTransform;
                x.transform.localScale = new Vector3(1, 1, 1);
            }
            if (DEBUG) Debug.Log(newPlacedObject.type + " " + newPlacedObject.class_name + "-" + newPlacedObject.id + " has occupied cells: " + string.Join(",", gridPositionList));
        }
        return newPlacedObject;
    }
    public PlaceableObject CreateSimObjectRender(PlaceableObject po)
    {   
        bool canBuild = true;
        if (!Vector3.zero.Equals(Mouse3DPosition.GetMouseWorldPosition()))
        {
            MyList<Vector3Int> gridPositionList = GetPositionInternal(GetMouseWorldSnappedPosition(false), po);
            foreach (Vector3Int gridPosition in gridPositionList)
            {

                if (!(gridPosition.x < grid.Width && gridPosition.y < grid.Height && gridPosition.z < grid.Lenght
                        && gridPosition.x >= 0 && gridPosition.y >= 0 && gridPosition.z >= 0))
                {
                    canBuild = false;
                    UtilsClass.CreateWorldTextPopup("Out of grid Bounds!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.red);
                    break;
                }
                else
                {
                    if (!grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).CanBuild())
                    {
                        canBuild = false;
                        UtilsClass.CreateWorldTextPopup("Cannot build here!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);
                        break;
                    }
                }
            }
            if (canBuild)
            {
                PlaceableObject newPlacedObject = PlaceableObject.SpawnVisual(po.type, po.class_name, po.id, po.Obj, GetMouseWorldSnappedPosition(true), gridPositionList, new Quaternion(0,0,0,0), po.Direction, false, false);
                newPlacedObject.SetScale(grid.CellSize);
                SetLayerRecursive(newPlacedObject.gameObject, 9);
                foreach (Vector3Int gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).SetPlacedObject(newPlacedObject);
                    if (DEBUG)
                    {
                        GameObject x = Instantiate(DEBUG_cell, new Vector3(gridPosition.x, gridPosition.y, gridPosition.z) * grid.CellSize, Quaternion.identity);
                        x.transform.parent = newPlacedObject.currentTransform;
                        x.transform.localScale = new Vector3(1,1,1);
                    }
                }
                if (DEBUG) Debug.Log(newPlacedObject.type + " " + newPlacedObject.class_name + "-" + newPlacedObject.id + " has occupied cells: " + string.Join(",", gridPositionList));
                return newPlacedObject;
            }
        }
        return null;
    }
    public void MoveSimObjectRender(PlaceableObject po, Quaternion orientation, PlaceableObject.Dir direction, Vector3 new_worldPosition, dynamic new_postion)
    {
        // spostare la visual
        po.transform.position = grid.GetWorldPosition((int)new_worldPosition.x, (int)new_worldPosition.y, (int)new_worldPosition.z);
        po.transform.rotation = orientation;
        // cambiare le celle occupate
        foreach (Vector3Int gridPosition in po.Position) grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).ClearPlacedObject();
        foreach (Vector3Int gridPosition in new_postion) grid.GetGridObject(gridPosition.x, gridPosition.y, gridPosition.z).SetPlacedObject(po);
        // cambiare position
        po.Position = new_postion; 
    }
    public void DeleteSimObjectRender(PlaceableObject toDelete)
    {
        toDelete.DestroyVisual();
        foreach (Vector3Int p in ((MyList<Vector3Int>)toDelete.GetPosition()))
        {
            grid.GetGridObject(p.x, p.y, p.z).ClearPlacedObject();
        }
}
    public void RotateSelectedSimObject()
    {
        if (selectedPlaceableObject != null && selectedPlaceableObject.isMovable)  UtilsClass.CreateWorldTextPopup("" + selectedPlaceableObject.Rotate(), Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize/10*40), Color.green);
    }
    public PlaceableObject GetSelectedSimObject()
    {
        return selectedPlaceableObject;
    }
    public dynamic GetPosition(PlaceableObject po) {

        return GetPositionInternal(po.transform.position, po);
    }
    public Vector3 GetTargetPosition()
    {
        return GetMouseWorldSnappedPosition(true);
    }
    public Quaternion GetTargetRotation()
    {
        return GetSelectedSimObjectRotation();
    }
    public dynamic GetRotationOffset(PlaceableObject po)
    {
        switch (po.Direction)
        {
            default:
            case PlaceableObject.Dir.Down: return new Vector3Int(0, 0, 0);
            case PlaceableObject.Dir.Left: return new Vector3Int(0, 0, ((SimObjectDiscreteSO)po.Obj).width);
            case PlaceableObject.Dir.Up: return new Vector3Int(((SimObjectDiscreteSO)po.Obj).width, 0, ((SimObjectDiscreteSO)po.Obj).lenght);
            case PlaceableObject.Dir.Right: return new Vector3Int(((SimObjectDiscreteSO)po.Obj).lenght, 0, 0);
        }
    }
    public MyList<Vector3Int> GetPositionInternal(Vector3 worldPosition, PlaceableObject po)
    {
        return GetNeededGridPositions(new Vector3Int(Mathf.RoundToInt(worldPosition.x/grid.CellSize), Mathf.RoundToInt(worldPosition.y / grid.CellSize), Mathf.RoundToInt(worldPosition.z / grid.CellSize)), po);
    }
    public void SpawnGhost(SelectChangedEventArgs e)
    {
        SpawnGhost(e.type, e.class_name, e.so);
    }
    private void OnSelectedChanged(object sender, SelectChangedEventArgs e)
    {
        RefreshSelectedSimObject(e);
    }
    public void RefreshSelectedSimObject(SelectChangedEventArgs e)
    {
        RemoveGhost();
        SpawnGhost(e);
    }
    public Quaternion GetSelectedSimObjectRotation()
    {
        if (selectedPlaceableObject != null)
        {
            return Quaternion.Euler(0, PlaceableObject.GetRotationAngle(selectedPlaceableObject.Direction), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }
    public MyList<Vector3Int> GetNeededGridPositions(Vector3Int origin, PlaceableObject po)
    {
        MyList<Vector3Int> positions = new MyList<Vector3Int>();
        SimObjectDiscreteSO dso = (SimObjectDiscreteSO)po.Obj;
        switch (po.Direction)
        {
            default:
            case PlaceableObject.Dir.Down:
            case PlaceableObject.Dir.Up:
                for (int x = 0; x < dso.width; x++)
                {
                    for (int y = 0; y < dso.height; y++)
                    {
                        for (int z = 0; z < dso.lenght; z++)
                        {
                            positions.Add(origin + new Vector3Int(x, y, z)); //sommo posizioni e ritorno vector3 di posizioni da occupare
                        }
                    }
                }
                break;
            case PlaceableObject.Dir.Left:
            case PlaceableObject.Dir.Right:
                for (int x = 0; x < dso.lenght; x++)
                {
                    for (int y = 0; y < dso.height; y++)
                    {
                        for (int z = 0; z < dso.width; z++)
                        {
                            positions.Add(origin + new Vector3Int(x, y, z));
                        }
                    }
                }
                break;
        }
        return positions;
    }
    public Vector3 GetMouseWorldSnappedPosition(bool offset)
    {
        Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
        grid.GetXYZ(mousePosition, out int x, out int y, out int z);
        Vector3Int rotationOffset = new Vector3Int(0, 0, 0);

        if (selectedPlaceableObject != null)
        {
            if (offset)
            {
                rotationOffset = GetRotationOffset(selectedPlaceableObject);    //prendo offset rotazione
            }
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, y, z) + new Vector3(rotationOffset.x, 0, rotationOffset.z) * grid.CellSize; //applico offset
            return placedObjectWorldPosition;
        }
        else
        {
            return mousePosition;
        }
    }
    private void SetLayerRecursive(GameObject targetGameObject, int layer)
    {
        targetGameObject.layer = layer;
        foreach (Transform child in targetGameObject.transform)
        {
            SetLayerRecursive(child.gameObject, layer); //setto il layer ghost a tutte le visual
        }
    }

 
}
