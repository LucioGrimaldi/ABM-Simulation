using UnityEngine;
using System.Collections.Concurrent;

public abstract class SimSpaceSystem : MonoBehaviour
{
    public enum SimSpaceTypeEnum
    {
        CONTINUOUS,
        DISCRETE
    }
    public enum SimSpaceDimensionsEnum
    {
        _2D,
        _3D
    }

    public SimSpaceTypeEnum simSpaceType;
    public SimSpaceDimensionsEnum simSpaceDimensions;
    public ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> placedObjectsDict = new ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)>();
    public ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> placedGhostsDict = new ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)>();

    public abstract ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> GetPlacedObjects();
    public abstract ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> GetTemporaryGhosts();
    public abstract void ClearSimSpaceSystem();
    public abstract PlaceableObject CreateGhost(SimObject simObject, PlaceableObject po, bool isMovable);
    public abstract PlaceableObject CreateSimObject(SimObject simObject, PlaceableObject po, bool isMovable);
    public abstract void DeleteSimObject(PlaceableObject toDelete);
    public abstract void RotatePlacedObject(PlaceableObject toRotate);
    public abstract void CopyRotation(PlaceableObject _old, PlaceableObject _new);
    public abstract Vector3 MouseClickToSpawnPosition(PlaceableObject toSpawn);
    public abstract bool CanBuild(PlaceableObject toPlace);
    public static void SetLayerRecursive(GameObject targetGameObject, int layer)
    {
        targetGameObject.layer = layer;
        foreach (Transform child in targetGameObject.transform)
        {
            SetLayerRecursive(child.gameObject, layer); //setto il layer ghost a tutte le visual
        }
    }
    public abstract PlaceableObject GetGhostFromSO(SimObject so);
}
