using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousSystem : SimSpaceSystem
{
    public override PlaceableObject CreateGhost(SimObject simObject, PlaceableObject placeableObject, bool isMovable)
    {
        throw new System.NotImplementedException();
    }
    public override PlaceableObject CreateSimObject(SimObject simObject, PlaceableObject placeableObject, bool isMovable)
    {
        throw new System.NotImplementedException();
    }
    public void MoveSimObject(PlaceableObject toMove, Vector3 new_position)
    {
        throw new System.NotImplementedException();
    }
    public void RotatePlacedSimObject()
    {
        throw new System.NotImplementedException();
    }
    public override void RotatePlacedObject(PlaceableObject toRotate)
    {
        throw new System.NotImplementedException();
    }
    public override void DeleteSimObject(PlaceableObject toDelete)
    {
        throw new System.NotImplementedException();
    }
    public override ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> GetPlacedObjects()
    {
        throw new System.NotImplementedException();
    }
    public override Vector3 MouseClickToSpawnPosition(PlaceableObject cso)
    {
        return Mouse3DPosition.GetMouseWorldPosition();
    }
    public Vector3 GetSimSpacePosition(PlaceableObject po)
    {
        throw new System.NotImplementedException();
    }
    public Vector3 MasonToWorldPosition2D(Vector2 sim_position)
    {
        return new Vector3(sim_position.x, 0, sim_position.y);
    }
    public Vector3 MasonToWorldPosition3D(Vector3 sim_position)
    {
        return new Vector3(sim_position.x, sim_position.z, sim_position.y);
    }

    public override bool CanBuild(PlaceableObject toPlace)
    {
        throw new System.NotImplementedException();
    }

    public override ConcurrentDictionary<(SimObject.SimObjectType type, string class_name, int id), (bool isGhost, PlaceableObject po)> GetTemporaryGhosts()
    {
        throw new System.NotImplementedException();
    }

    public override PlaceableObject GetGhostFromSO(SimObject so)
    {
        throw new System.NotImplementedException();
    }

    public override void CopyRotation(PlaceableObject _old, PlaceableObject _new)
    {
        throw new System.NotImplementedException();
    }
}
