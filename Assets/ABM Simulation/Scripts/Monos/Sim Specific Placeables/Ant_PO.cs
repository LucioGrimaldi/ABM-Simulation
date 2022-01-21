using GerardoUtils;
using UnityEngine;
using static SimObjectRender;

public class Ant_PO : PO_Discrete2D
{
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        return base.Init(simObject, po, isGhost, isMovable);
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
        if (isGhost)
        {
            if (isMovable)
            {
                Vector3 targetPosition = gridSystem.MouseClickToSpawnPosition(this);
                transform.position = targetPosition;
                transform.Find("Model").rotation = Quaternion.Euler(GetRotationVector(direction));
            }
        }
        else
        {
            ChangeColor((bool)simObject.Parameters["hasFoodItem"]);
            if (isMovable)
            {
                Vector3 targetPosition = GridSystem.MasonToUnityPosition2D((MyList<Vector2Int>)GetCells());
                direction = GetFacingDirection(transform.position, targetPosition);
                transform.position = targetPosition;
                transform.Find("Model").rotation = Quaternion.Euler(GetRotationVector(direction));
                MoveInSimSpace();
            }
        }
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


    public void ChangeColor(bool hasFoodItem)
    {
        transform.Find("Model").gameObject.GetComponent<MeshRenderer>().material = (hasFoodItem) ? simObjectRender.Materials["hasFoodItem"] : simObjectRender.Materials["default"];
    }
    public override void Rotate()
    {
        direction = (DirEnum)(((int)direction + 1) % 8);
        simObject.Parameters["rotation"] = (Quaternion)simObject.Parameters["rotation"] * Quaternion.Euler(0, 45, 0);
        UtilsClass.CreateWorldTextPopup("" + direction, Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(gridSystem.grid.CellSize / 10 * 40), Color.green);
    }
    public override Vector3 GetRotationVector(DirEnum dir)
    {
        return new Vector3(-90, 0, ((int)dir) * 45);
    }
}
