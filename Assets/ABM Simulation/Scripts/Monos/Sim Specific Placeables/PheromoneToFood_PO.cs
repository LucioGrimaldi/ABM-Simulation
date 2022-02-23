using UnityEngine;

public class PheromoneToFood_PO : PO_Discrete2D
{
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        gridSystem = GameObject.Find("SimSpaceSystem").GetComponent<GridSystem>();

        PheromoneToFood_PO po_clone = Instantiate((PheromoneToFood_PO)po, GridSystem.MasonToUnityPosition2D(GetCellsFromSimObject(simObject)), po.gameObject.transform.rotation);
        po_clone.SetScale(gridSystem.grid.CellSize);
        if (isGhost)
        {        
            po_clone.simObject = simObject.Clone();
            SimSpaceSystem.SetLayerRecursive(po_clone.gameObject, 10);
        }
        else
        {
            po_clone.simObject = simObject;
            SimSpaceSystem.SetLayerRecursive(po_clone.gameObject, 9);
            po_clone.pos = (MyList<Vector2Int>)po_clone.GetCells();
            gridSystem.placedObjectsDict.TryAdd((po_clone.simObject.Type, po_clone.simObject.Class_name, po_clone.simObject.Id), (isGhost, po_clone));
            foreach (Vector2Int cell in po_clone.pos) gridSystem.grid.GetGridObject(cell.x, 0, cell.y).SetPlacedObject(po_clone);
        }
        po_clone.isGhost = isGhost; po_clone.isMovable = isMovable;
        return po_clone;
    }

    // UNITY LOOP METHODS

    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    protected override void Start()
    {
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    protected override void OnEnable()
    {

    }
    /// <summary>
    /// LateUpdate routine (Unity Process)
    /// </summary>
    protected override void LateUpdate()
    {
        if (isGhost)
        {
            if (isMovable)
            {
                Vector3 targetPosition = gridSystem.MouseClickToSpawnPosition(this);
                transform.position = targetPosition;
            }
        }
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    protected override void OnApplicationQuit()
    {

    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    protected override void OnDisable()
    {

    }

    public override void _Update()
    {
        // add/remove from Shader Buffers
        GameObject.Find("SceneController").GetComponent<SceneController>().simulationSpace.GetComponent<ShaderManager>().f_cells[((MyList<Vector2Int>)simObject.Parameters["position"])[0].x, ((MyList<Vector2Int>)simObject.Parameters["position"])[0].y] = (float)simObject.Parameters["intensity"];

        //if(!isGhost)
        //{
        //    if (isMovable)
        //    {
        //        Vector3 targetPosition = GridSystem.MasonToUnityPosition2D((MyList<Vector2Int>)GetCells());
        //        transform.position = targetPosition;
        //        MoveInSimSpace();
        //    }
        //}
    }
    public override void _Delete()
    {
        // remove itself from Shader Buffers
        GameObject.Find("SceneController").GetComponent<SceneController>().simulationSpace.GetComponent<ShaderManager>().f_cells[((MyList<Vector2Int>)simObject.Parameters["position"])[0].x, ((MyList<Vector2Int>)simObject.Parameters["position"])[0].y] = 0f;
    }
}
