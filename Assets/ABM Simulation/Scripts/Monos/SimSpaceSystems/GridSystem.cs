using UnityEngine;
using GerardoUtils;
using System.Collections.Concurrent;
using System.Linq;

public class GridSystem : SimSpaceSystem
{
    public class GridObject
    {
        private Grid3D<GridObject> grid;
        private int x;
        private int y;
        private int z;
        private ConcurrentDictionary<string, MyList<PO_Discrete>> placedObjects = new ConcurrentDictionary<string, MyList<PO_Discrete>>();              // LA MATTINA LA LEMPIO E LA SERA LA SVACANTO

        public GridObject(Grid3D<GridObject> grid, int x, int y, int z)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        //Set e Get transform
        public int X { get => x; set => x = value; }
        public int Y { get => y; set => y = value; }
        public int Z { get => z; set => z = value; }
        public ConcurrentDictionary<string, MyList<PO_Discrete>> PlacedObjects { get => placedObjects; set => placedObjects = value; }

        public void SetPlacedObject(PO_Discrete placedObject)
        {
            if (placedObjects.ContainsKey(placedObject.SimObject.Layer))
            {
                placedObjects[placedObject.SimObject.Layer].Add(placedObject);
                grid.TriggerGridObjectChanged(x, y, z);
            }
            else
            {
                MyList<PO_Discrete> l = new MyList<PO_Discrete>();
                l.Add(placedObject);
                placedObjects.TryAdd(placedObject.SimObject.Layer, l);
                grid.TriggerGridObjectChanged(x, y, z);
            }

        }
        public void ClearPlacedObject(PO_Discrete toRemove)
        {
            if (placedObjects.ContainsKey(toRemove.SimObject.Layer)) if (placedObjects[toRemove.SimObject.Layer].Remove(toRemove)) grid.TriggerGridObjectChanged(x, y, z);
        }
        public void ClearAllPlacedObjects()
        {
            placedObjects.Clear();
            grid.TriggerGridObjectChanged(x, y, z);
        }
        public void ClearAllPlacedObjectsInLayer(string layer)
        {
            if (placedObjects.ContainsKey(layer)) placedObjects[layer].Clear();
            grid.TriggerGridObjectChanged(x, y, z);
        }
        public MyList<PO_Discrete> GetPlacedObjectsByLayer(string layer)
        {
            if (placedObjects.ContainsKey(layer)) return placedObjects[layer]; else return null;
        }
        public bool CanBuild(SimObject so)
        {
            return !(placedObjects.ContainsKey(so.Layer) && placedObjects[so.Layer].Count > 0) || so.Shares_position;
        }
        public override string ToString()
        {
            return x + "," + y + "," + z + "\n" + placedObjects;
        }

    }

    public PO_Discrete selectedPlaceableObject = null;
    public Grid3D<GridObject> grid;
    public GameObject DEBUG_cell;
    private bool DEBUG = false;


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
    }


    // Interface Methods
    public override ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> GetPlacedObjects()
    {
        return placedObjectsDict;
    }
    public override PlaceableObject CreateGhost(SimObject simObject, PlaceableObject po, bool isMovable)
    {
        return Create(simObject, po, true, true);
    }
    public override PlaceableObject CreateSimObject(SimObject simObject, PlaceableObject po, bool isMovable)
    {
        return Create(simObject, po, false, true);
    }
    public override void ConfirmEdited()
    {
        GetPlacedObjects().Where((kvp) => kvp.Value.isGhost).All((g) => { g.Value.po.Confirm(); return true;});
    }
    public override void RotatePlacedObject(PlaceableObject toRotate)
    {
        toRotate.Rotate();
    }
    public override void DeleteSimObject(PlaceableObject toDelete)
    {
        if (toDelete != null) toDelete.Destroy();
    }
    public override Vector3 MouseClickToSpawnPosition(PlaceableObject dso)
    {
        Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
        grid.GetXYZ(mousePosition, out int x, out int y, out int z);

        if (dso != null)
        {
            Vector3Int rotationOffset = ((PO_Discrete)dso).GetRotationOffset();                                                                                            //prendo offset rotazione
            Vector3 placedObjectWorldPosition = Grid3D<GridObject>.GetWorldPosition(x, y, z) + new Vector3(rotationOffset.x, 0, rotationOffset.z) * grid.CellSize;                       //applico offset
            return placedObjectWorldPosition;
        }
        else
        {
            return mousePosition;
        }
    }
    public static Vector3 MasonToUnityPosition2D(MyList<Vector2Int> sim_position)
    {
        return Grid3D<GridObject>.GetWorldPosition(sim_position[0].x, 0, sim_position[0].y);
    }
    public static Vector3 MasonToUnityPosition3D(MyList<Vector3Int> sim_position)
    {
        return Grid3D<GridObject>.GetWorldPosition(sim_position[0].x, sim_position[0].z, sim_position[0].y);
    }

    // Other Methods
    public PlaceableObject Create(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        if (simSpaceType.Equals(SimSpaceTypeEnum.DISCRETE))
        {
            if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._2D)) return !isGhost ? (CanBuild2D(simObject) ? PlaceableObject.Create(simObject, po, isGhost, isMovable) : null) : PlaceableObject.Create(simObject, po, isGhost, isMovable);
            //if(simSpaceDimensions.Equals(SimSpaceDimensionsEnum._3D)) return CanBuild3D((PO_Discrete3D)po) ? PO_Discrete3D.Create(simObject, (PO_Discrete3D)po, isGhost, isMovable) : null;
        }
        else
        {
            //if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._2D)) return PO_Continuous2D.Create(simObject, (PO_Continuous2D)po, isGhost, isMovable);
            if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._3D)) return PO_Continuous3D.Create(simObject, (PO_Continuous3D)po, isGhost, isMovable);
        }
        return null;
    }
    public bool CanBuild2D(SimObject so)
    {
        return CheckCells2D(so);
    }
    public bool CheckCells2D(SimObject so)
    {
        bool canBuild = true;
        foreach (Vector2Int cell in (MyList<Vector2Int>)so.Parameters["position"])
        {
            if (!(cell.x < grid.Width && cell.y < grid.Lenght
                    && cell.x >= 0 && cell.y >= 0))
            {
                canBuild = false;
                UtilsClass.CreateWorldTextPopup("Out of grid Bounds!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.red);
                break;
            }
            else
            {
                if (!grid.GetGridObject(cell.x, 0, cell.y).CanBuild(so))
                {
                    canBuild = false;
                    UtilsClass.CreateWorldTextPopup("Cannot build here!", Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(grid.CellSize / 10 * 40), Color.yellow);
                    break;
                }
            }
        }
        return canBuild;
    }
    public MyList<Vector2Int> GetNeededCells2D(Vector2Int origin, PO_Discrete2D.DirEnum dir, int width, int lenght)
    {
        MyList<Vector2Int> positions = new MyList<Vector2Int>();
        switch (dir)
        {
            default:
            case PO_Discrete2D.DirEnum.NORD:
            case PO_Discrete2D.DirEnum.SUD:
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < lenght; z++)
                    {
                        positions.Add(origin + new Vector2Int(x, z)); //sommo posizioni e ritorno vector3 di posizioni da occupare
                    }
                }
                break;
            case PO_Discrete2D.DirEnum.EST:
            case PO_Discrete2D.DirEnum.OVEST:
                for (int x = 0; x < lenght; x++)
                {
                    for (int z = 0; z < width; z++)
                    {
                        positions.Add(origin + new Vector2Int(x, z));
                    }
                }
                break;
        }
        return positions;
    }

}
