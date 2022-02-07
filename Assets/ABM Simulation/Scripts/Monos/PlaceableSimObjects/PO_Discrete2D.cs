using GerardoUtils;
using UnityEngine;
using static SimObjectRender;
using System;
using SimpleJSON;

public class PO_Discrete2D : PO_Discrete
{
    // STATIC
    protected static GridSystem gridSystem;
    protected override PlaceableObject Init(SimObject simObject, PlaceableObject po, bool isGhost, bool isMovable)
    {
        gridSystem = GameObject.Find("SimSpaceSystem").GetComponent<GridSystem>();

        PO_Discrete2D po_clone = Instantiate((PO_Discrete2D)po, GridSystem.MasonToUnityPosition2D((MyList<Vector2Int>)simObject.Parameters["position"]), Quaternion.Euler(Vector3.zero));
        po_clone.transform.Find("Model").GetComponent<Outline>().enabled = false;
        po_clone.transform.Find("Model").GetComponent<Outline>().OutlineWidth = 5f;
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
            gridSystem.placedObjectsDict.TryAdd((po_clone.simObject.Type, po_clone.simObject.Class_name, po_clone.simObject.Id),(isGhost, po_clone));
            foreach (Vector2Int cell in po_clone.pos) gridSystem.grid.GetGridObject(cell.x, 0, cell.y).SetPlacedObject(po_clone);
        }
        po_clone.isGhost = isGhost; po_clone.isMovable = isMovable;
        return po_clone;
    }

    public enum DirEnum 
    {
        NORD,
        NORD_EST,
        EST,
        SUD_EST,
        SUD,
        SUD_OVEST,
        OVEST,
        NORD_OVEST
    }

    // NON-STATIC
    [SerializeField] protected DirEnum direction;
    [SerializeField] protected int width, length;
    protected MyList<Vector2Int> pos;
    [SerializeField] protected bool isSquared = false;

    public DirEnum Direction { get => direction; set => direction = value; }
    public int Width { get => width; set => width = value; }
    public int Length { get => length; set => length = value; }
    public MyList<Vector2Int> Pos { get => pos; set => pos = value; }


    // UNITY LOOP METHODS

    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    protected virtual void Start()
    {
        isSquared = width.Equals(length) ? true : false;
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    protected virtual void OnEnable()
    {

    }
    /// <summary>
    /// LateUpdate routine (Unity Process)
    /// </summary>
    protected virtual void LateUpdate()
    {
        if (simObjectRender.RenderType.Equals(RenderTypeEnum.MESH))        // TODO (TEXTURE,PARTICLE_SYSTEM,OTHER..)
        {
            if (isGhost)
            {
                if (isMovable)
                {
                    Vector3Int rotationOffset = GetRotationOffset();                                                                                                                                    // prendo offset rotazione
                    Vector3 targetPosition = gridSystem.MouseClickToSpawnPosition(this);                                                                                                                // offset escluso
                    gridSystem.grid.GetXYZ(targetPosition, out int x, out _, out int z);
                    simObject.Parameters["position"] = pos = gridSystem.GetNeededCells2D(new Vector2Int(x, z), direction, width, length);
                    transform.position = Vector3.Lerp(transform.position, targetPosition + new Vector3(rotationOffset.x, 0, rotationOffset.z) * gridSystem.grid.CellSize, Time.deltaTime * 15f);        // offset incluso
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(GetRotationVector(direction)), Time.deltaTime * 15f);                                                     // limiti di rotazione gestiti in Rotate()
                }
            }
            else
            {
                if (isMovable)
                {
                    direction = GetFacingDirection((Quaternion)simObject.Parameters["rotation"]);
                    Vector3Int rotationOffset = GetRotationOffset();                                                                                                                                    // prendo offset rotazione
                    Vector3 targetPosition = GridSystem.MasonToUnityPosition2D((MyList<Vector2Int>)GetCells());
                    transform.position = Vector3.Lerp(transform.position, targetPosition + new Vector3(rotationOffset.x, 0, rotationOffset.z) * gridSystem.grid.CellSize, Time.deltaTime * 15f);
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(GetRotationVector(direction)), Time.deltaTime * 15f);
                    MoveInSimSpace();
                }
            }
        }
    }
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    protected virtual void OnApplicationQuit()
    {

    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    protected virtual void OnDisable()
    {

    }

    public void MoveInSimSpace()
    {
        foreach (Vector2Int cell in pos) gridSystem.grid.GetGridObject(cell.x, 0, cell.y).ClearPlacedObject(this);
        foreach (Vector2Int cell in pos = (MyList<Vector2Int>)GetCells()) gridSystem.grid.GetGridObject(cell.x, 0, cell.y).SetPlacedObject(this);
    }
    public override bool PlaceGhost(Vector3 position)
    {
        gridSystem.grid.GetXYZ(position, out int x, out _, out int z);
        simObject.Parameters["position"] = pos = gridSystem.GetNeededCells2D(new Vector2Int(x, z), direction, width, length);
        foreach (Vector2Int cell in pos) gridSystem.grid.GetGridObject(cell.x, 0, cell.y).SetPlacedObject(this);
        gridSystem.placedGhostsDict.TryAdd((simObject.Type, simObject.Class_name, simObject.Id), (isGhost, this));
        base.PlaceGhost(position);
        return true;
    }
    public override bool PlaceGhost()
    {
        foreach (Vector2Int cell in (MyList<Vector2Int>)GetCells()) gridSystem.grid.GetGridObject(cell.x, 0, cell.y).SetPlacedObject(this);
        gridSystem.placedGhostsDict.TryAdd((simObject.Type, simObject.Class_name, simObject.Id), (isGhost, this));
        base.PlaceGhost();
        return true;
    }
    public override void Destroy()
    {
        base.Destroy();
    }
    public override void Rotate()
    {
        direction = (DirEnum)(((int)direction + 2) % 8);
        simObject.Parameters["rotation"] = (Quaternion)simObject.Parameters["rotation"] * Quaternion.Euler(0, 90, 0);
        UtilsClass.CreateWorldTextPopup("" + direction, Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(gridSystem.grid.CellSize / 10 * 40), Color.green);
    }
    public override void Rotate(DirEnum dir)
    {
        direction = dir;
        UtilsClass.CreateWorldTextPopup("" + direction, Mouse3DPosition.GetMouseWorldPosition(), Mathf.RoundToInt(gridSystem.grid.CellSize / 10 * 40), Color.green);
    }
    public override void SetScale(float scale)
    {
        transform.localScale = new Vector3(scale / 10f, scale / 10f, scale / 10f);
    }
    public override void Highlight()
    {
        base.Highlight();
        gameObject.transform.Find("Ground").gameObject.GetComponent<MeshRenderer>().enabled = true;
    }
    public override void DeHighlight()
    {
        base.DeHighlight();
        gameObject.transform.Find("Ground").gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
    public override Vector3Int GetRotationOffset()
    {
        switch (direction)
        {
            default:
            case PO_Discrete2D.DirEnum.NORD: return new Vector3Int(0, 0, 0);            //width = x, lenght = z
            case PO_Discrete2D.DirEnum.EST: return new Vector3Int(0, 0, width);
            case PO_Discrete2D.DirEnum.SUD: return new Vector3Int(width, 0, length);
            case PO_Discrete2D.DirEnum.OVEST: return new Vector3Int(length, 0, 0);
        }
    }
    public override object GetCells()
    {
        if (typeof(MyList<Vector2Int>) == simObject.Parameters["position"].GetType()) return (MyList<Vector2Int>)simObject.Parameters["position"];
        else
        {
            MyList<Vector2Int> cells = new MyList<Vector2Int>();
            foreach (JSONNode e in (JSONArray)simObject.Parameters["position"])
            {
                Vector2Int cell = new Vector2Int();
                cell.x = e["x"];
                cell.y = e["y"];
                cells.Add(cell);
            }
            return simObject.Parameters["position"] = cells;
        }
    }
    public DirEnum GetFacingDirection(Vector3 last_pos, Vector3 new_pos)
    {
        if (isSquared)
        {
            Vector3 dir = new_pos - last_pos;
            if (dir.Equals(Vector3.zero)) return direction;
            float rotation = Quaternion.FromToRotation(Vector3.forward, dir.normalized).eulerAngles.y;
            return dir.Equals(Vector3.zero) ? direction : (DirEnum)Mathf.Floor(rotation / 45);
        }
        else return direction;
    }
    public DirEnum GetFacingDirection(Quaternion rotation)
    {
        return rotation.eulerAngles.y.Equals(0) ? DirEnum.NORD : (DirEnum)Mathf.Floor(rotation.eulerAngles.y / 45);
    }
    public virtual Vector3 GetRotationVector(DirEnum dir)
    {
        return new Vector3(0, ((int)dir) * 45, 0);
    }
    public MyList<Vector2Int> GetCellsFromSimObject(SimObject so)
    {
        if (typeof(MyList<Vector2Int>) == so.Parameters["position"].GetType()) return (MyList<Vector2Int>)so.Parameters["position"];
        else
        {
            MyList<Vector2Int> cells = new MyList<Vector2Int>();
            foreach (JSONNode e in (JSONArray)so.Parameters["position"])
            {
                Vector2Int cell = new Vector2Int();
                cell.x = e["x"];
                cell.y = e["y"];
                cells.Add(cell);
            }
            return cells;
        }
    }
}
