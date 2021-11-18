using UnityEngine;
using static SimObjectRender;

public class PheromoneToFood_PO : PO_Discrete2D
{
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        gridSystem = GameObject.Find("SimSpaceSystem").GetComponent<GridSystem>();

        PheromoneToFood_PO po_clone = Instantiate((PheromoneToFood_PO)po, GridSystem.MasonToUnityPosition2D((MyList<Vector2Int>)simObject.Parameters["position"]), Quaternion.Euler(Vector3.zero));
        po_clone.simObject = simObject.Clone();
        po_clone.SetScale(gridSystem.grid.CellSize);
        if (isGhost)
        {
            SimSpaceSystem.SetLayerRecursive(po_clone.gameObject, 10);
        }
        else
        {
            SimSpaceSystem.SetLayerRecursive(po_clone.gameObject, 9);
            po_clone.pos = po_clone.GetCells();
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
        // add/remove from Shader Buffers
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
}
