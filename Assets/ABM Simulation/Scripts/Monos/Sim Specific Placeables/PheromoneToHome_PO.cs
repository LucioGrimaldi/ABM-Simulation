using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimObjectRender;

public class PheromoneToHome_PO : PO_Discrete2D
{
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        gridSystem = GameObject.Find("SimSpaceSystem").GetComponent<GridSystem>();

        PheromoneToHome_PO po_clone = Instantiate((PheromoneToHome_PO)po, GridSystem.MasonToUnityPosition2D(GetCellsFromSimObject(simObject)), Quaternion.Euler(Vector3.zero));
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
        // add/remove from Shader Buffers
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
}
