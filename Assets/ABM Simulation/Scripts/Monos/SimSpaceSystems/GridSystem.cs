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
            if (placedObjects.ContainsKey(layer)) return placedObjects[layer]; else return new MyList<PO_Discrete>();
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
    public override ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> GetTemporaryGhosts()
    {
        return placedGhostsDict;
    }
    public override PlaceableObject CreateGhost(SimObject simObject, PlaceableObject po, bool isMovable)
    {
        return Create(simObject, po, true, isMovable);
    }
    public override PlaceableObject CreateSimObject(SimObject simObject, PlaceableObject po, bool isMovable)
    {
        return Create(simObject, po, false, isMovable);
    }
    public override void DeleteSimObject(PlaceableObject toDelete)
    {
        if (toDelete != null)
        {
            foreach (Vector2Int cell in (MyList<Vector2Int>)((PO_Discrete)toDelete).GetCells()) grid.GetGridObject(cell.x, 0, cell.y).ClearPlacedObject((PO_Discrete)toDelete);
            if(toDelete.IsGhost) placedGhostsDict.TryRemove((toDelete.SimObject.Type, toDelete.SimObject.Class_name, toDelete.SimObject.Id), out _);
            else placedObjectsDict.TryRemove((toDelete.SimObject.Type, toDelete.SimObject.Class_name, toDelete.SimObject.Id), out _);
        }
    }
    public override void RotatePlacedObject(PlaceableObject toRotate)
    {
        toRotate.Rotate();
    }
    public override void CopyRotation(PlaceableObject _old, PlaceableObject _new)
    {
        if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._2D)) ((PO_Discrete2D)_new).Direction = ((PO_Discrete2D)_old).Direction;
    }
    public override Vector3 MouseClickToSpawnPosition(PlaceableObject po)
    {
        Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
        grid.GetXYZ(mousePosition, out int x, out int y, out int z);

        if (po != null) return Grid3D<GridObject>.GetWorldPosition(x, y, z);
        else return mousePosition;
    }
    public override bool CanBuild(PlaceableObject toPlace)
    {
        if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._2D)) return CanBuild2D(toPlace.SimObject);
        //else if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._3D)) return CanBuild3D(toPlace.SimObject);
        return false;
    }
    public override PlaceableObject GetGhostFromSO(SimObject so)
    {
        int max_id = int.MinValue;
        foreach ((bool isGhost, PlaceableObject g) in placedGhostsDict.Values)
        {
            if (g.SimObject.Type.Equals(so.Type) && g.SimObject.Class_name.Equals(so.Class_name))
            {
                if (g.SimObject.Id > max_id)
                {
                    max_id = g.SimObject.Id;
                }
            }
        }
        placedGhostsDict.TryGetValue((so.Type, so.Class_name, max_id), out (bool, PlaceableObject) x);
        return x.Item2;
    }
    // Other Methods
    public PlaceableObject Create(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._2D)) return PlaceableObject.Create(simObject, po, isGhost, isMovable);
        //else if(simSpaceDimensions.Equals(SimSpaceDimensionsEnum._3D)) return CanBuild3D((PO_Discrete3D)po) ? PO_Discrete3D.Create(simObject, (PO_Discrete3D)po, isGhost, isMovable) : null;
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
    public static Vector3 MasonToUnityPosition2D(MyList<Vector2Int> sim_position)
    {
        return Grid3D<GridObject>.GetWorldPosition(sim_position[0].x, 0, sim_position[0].y);
    }
    public static Vector3 MasonToUnityPosition3D(MyList<Vector3Int> sim_position)
    {
        return Grid3D<GridObject>.GetWorldPosition(sim_position[0].x, sim_position[0].z, sim_position[0].y);
    }
}
