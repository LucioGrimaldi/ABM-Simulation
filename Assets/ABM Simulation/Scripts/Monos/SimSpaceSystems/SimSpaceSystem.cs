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

    public abstract ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> GetPlacedObjects();
    public abstract PlaceableObject CreateGhost(SimObject simObject, PlaceableObject po, bool isMovable);
    public abstract PlaceableObject CreateSimObject(SimObject simObject, PlaceableObject po, bool isMovable);
    public abstract void ConfirmEdited();
    public abstract void DeleteSimObject(PlaceableObject toDelete);
    public abstract void RotatePlacedObject(PlaceableObject roRotate);
    public abstract Vector3 MouseClickToSpawnPosition(PlaceableObject toSpawn);
    public static void SetLayerRecursive(GameObject targetGameObject, int layer)
    {
        targetGameObject.layer = layer;
        foreach (Transform child in targetGameObject.transform)
        {
            SetLayerRecursive(child.gameObject, layer); //setto il layer ghost a tutte le visual
        }
    }

}
