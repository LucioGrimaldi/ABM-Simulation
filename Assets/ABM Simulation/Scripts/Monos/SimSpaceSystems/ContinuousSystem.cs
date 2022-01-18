using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousSystem : SimSpaceSystem
{
    public static int width, height, lenght;
    public static GameObject simSpace;

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
            if (toDelete.IsGhost) placedGhostsDict.TryRemove((toDelete.SimObject.Type, toDelete.SimObject.Class_name, toDelete.SimObject.Id), out _);
            else placedObjectsDict.TryRemove((toDelete.SimObject.Type, toDelete.SimObject.Class_name, toDelete.SimObject.Id), out _);
        }
    }
    public override void RotatePlacedObject(PlaceableObject toRotate)
    {
        toRotate.Rotate();
    }
    public override void CopyRotation(PlaceableObject _old, PlaceableObject _new)
    {
        if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._2D)) ((PO_Continuous2D)_new).Direction = ((PO_Continuous2D)_old).Direction;
    }
    public override Vector3 MouseClickToSpawnPosition(PlaceableObject toSpawn)
    {
        Vector3 mousePosition = Mouse3DPosition.GetMouseWorldPosition();
        return mousePosition;
    }
    public override bool CanBuild(PlaceableObject toPlace)
    {
        if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._2D)) return CanBuild2D(toPlace.SimObject);
        else if (simSpaceDimensions.Equals(SimSpaceDimensionsEnum._3D)) return CanBuild3D(toPlace.SimObject);
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
        return PlaceableObject.Create(simObject, po, isGhost, isMovable);
    }
    public bool CanBuild2D(SimObject so)
    {
        return true;
    }
    public bool CanBuild3D(SimObject so)
    {
        return true;
    }
    public static Vector3 MasonToUnityPosition2D(Vector3 sim_position)
    {
        return new Vector3(sim_position.x / (width / simSpace.transform.localScale.x), sim_position.z / (height / simSpace.transform.localScale.y), sim_position.y / (lenght / simSpace.transform.localScale.z));
    }
    public static Vector3 MasonToUnityPosition3D(Vector3 sim_position)
    {
        return new Vector3(sim_position.x / (width / simSpace.transform.localScale.x), sim_position.z / (height / simSpace.transform.localScale.y), sim_position.y / (lenght / simSpace.transform.localScale.z));
    }
    public static Vector3 UnityToMasonPosition3D(Vector3 sim_position)
    {
        return new Vector3(sim_position.x * (width / simSpace.transform.localScale.x), sim_position.z * (height / simSpace.transform.localScale.y), sim_position.y * (lenght / simSpace.transform.localScale.z));
    }
}
