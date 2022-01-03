using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle_PO : PO_Discrete2D
{
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        gridSystem = GameObject.Find("SimSpaceSystem").GetComponent<GridSystem>();

        Obstacle_PO po_clone = Instantiate((Obstacle_PO)po, GridSystem.MasonToUnityPosition2D((MyList<Vector2Int>)simObject.Parameters["position"]), Quaternion.Euler(Vector3.zero));
        po_clone.transform.Find("Model").GetComponent<Outline>().enabled = false;
        po_clone.transform.Find("Model").GetComponent<Outline>().OutlineWidth = 5f * Mathf.Max(po_clone.width, po_clone.lenght);
        po_clone.SetScale(gridSystem.grid.CellSize);
        if (isGhost)
        {
            po_clone.simObject = simObject.Clone();
            po_clone.SetMaterialRecursive(po_clone.transform.Find("Model").gameObject, po_clone.SimObjectRender.ghostMaterial);
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
        base.Start();
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    /// <summary>
    /// LateUpdate routine (Unity Process)
    /// </summary>
    protected override void LateUpdate()
    {
        base.LateUpdate();
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
    }
}
