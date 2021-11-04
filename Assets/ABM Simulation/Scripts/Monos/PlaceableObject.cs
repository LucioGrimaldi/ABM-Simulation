using System.Runtime.InteropServices;
using UnityEngine;
using static SimObjectSO;


public class PlaceableObject : MonoBehaviour
{
    private static SimSpaceSystem SimSpaceSystem;

    public SimObject.SimObjectType type;
    public string class_name;
    public int id;
    public SimObjectSO obj;
    public Dir direction;
    public dynamic position;
    public bool isMovable = false;
    public Transform currentTransform;
    public bool isGhost = false;
    public bool isPrefab = false;

    public enum Dir
    {
        Down,
        Left,
        Up,
        Right,
    }

    public SimObjectSO Obj { get => obj; set => obj = value; }
    public Dir Direction { get => direction; set => direction = value; }
    public dynamic Position { get => position; set => position = value; }

    // UNITY LOOP METHODS

    /// <summary>
    /// Start routine (Unity Process)
    /// </summary>
    private void Start()
    {
        SimSpaceSystem = GameObject.Find("SimSpaceSystem").GetComponent<SimSpaceSystem>();
    }
    /// <summary>
    /// onEnable routine (Unity Process)
    /// </summary>
    private void OnEnable()
    {

    }
    /// <summary>
    /// LateUpdate routine (Unity Process)
    /// </summary>
    private void LateUpdate()
    {
        if (isMovable)
        {
            Vector3 targetPosition = SimSpaceSystem.GetTargetPosition();
            if (isGhost) targetPosition += obj.higher * ((GridSystem)SimSpaceSystem).grid.CellSize;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, GetRotationAngle(direction), 0), Time.deltaTime * 15f);
            if (isPrefab)
            {
                Position = SimSpaceSystem.GetPosition(this);
            }
        }

    }                                       // GENERALIZZARE
    /// <summary>
    /// onApplicationQuit routine (Unity Process)
    /// </summary>
    void OnApplicationQuit()
    {

    }
    /// <summary>
    /// onDisable routine (Unity Process)
    /// </summary>
    private void OnDisable()
    {

    }

    public static PlaceableObject SpawnVisual(SimObject.SimObjectType type, string class_name, int id, SimObjectSO obj, Vector3 worldPosition, dynamic position, Quaternion orientation, Dir direction, bool ghost, bool quat)
    {
        if (!quat) orientation = Quaternion.Euler(0, GetRotationAngle(direction), 0);
        Transform currentTransform = Instantiate((ghost) ? obj.ghost : obj.prefab, (ghost) ? worldPosition + obj.higher : worldPosition, orientation);
        PlaceableObject placedObject = currentTransform.gameObject.GetComponent<PlaceableObject>();
        placedObject.InitPlacedObject(type, class_name, id, obj, position, ghost);

        return placedObject;
    }
    public void InitPlacedObject(SimObject.SimObjectType type, string class_name, int id, SimObjectSO obj, dynamic position, bool ghost)
    {
        this.currentTransform = this.transform;
        this.isGhost = ghost; this.isPrefab = !ghost;
        this.type = type;
        this.class_name = class_name;
        this.id = id;
        this.obj = obj;
        this.position = position;
        this.direction = Dir.Down;
    }
    public void SetScale(float cellSize)
    {
        currentTransform.localScale = new Vector3(cellSize / 10f, cellSize / 10f, cellSize / 10f);
    }
    public void DestroyVisual()
    {
        isGhost = false; isPrefab = false;
        Destroy(currentTransform.gameObject);
        currentTransform = null;
    }
    public void SetMovable(bool movable)
    {
        this.isMovable = movable;
    }
    public dynamic GetPosition()
    {
        return this.position;
    }
    public static int GetRotationAngle(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return 0;
            case Dir.Left: return 90;
            case Dir.Up: return 180;
            case Dir.Right: return 270;
        }
    }
    public Dir Rotate()
    {
        switch (direction)
        {
            default:
            case Dir.Down: return direction = Dir.Left;
            case Dir.Left: return direction = Dir.Up;
            case Dir.Up: return direction = Dir.Right;
            case Dir.Right: return direction = Dir.Down;
        }
    }
    public override string ToString()
    {
        return "TYPE: " + type + " " + ", CLASS_NAME: " + class_name + ", ID: " + id + "."; 
    }

}
