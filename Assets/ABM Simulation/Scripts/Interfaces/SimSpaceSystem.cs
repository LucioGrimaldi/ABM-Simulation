using UnityEngine;

public interface SimSpaceSystem
{
    public bool IsGhostSelected();
    public void SpawnGhost(SimObject.SimObjectType type, string class_name, SimObjectSO so);
    public void RemoveGhost();
    public PlaceableObject GetSelectedSimObject();
    public PlaceableObject CreateSimObjectRender(SimObject.SimObjectType type, string class_name, int id, SimObjectSO obj, Quaternion orientation, PlaceableObject.Dir direction, Vector3 worldPosition, dynamic position);
    public PlaceableObject CreateSimObjectRender(PlaceableObject po);
    public void MoveSimObjectRender(PlaceableObject po, Quaternion orientation, PlaceableObject.Dir direction, Vector3 new_worldPosition, dynamic new_postion);
    public void DeleteSimObjectRender(PlaceableObject toDelete);
    public void RotateSelectedSimObject();
    public dynamic GetPosition(PlaceableObject po);
    public dynamic GetRotationOffset(PlaceableObject po);
    public Vector3 GetTargetPosition();
    public Quaternion GetTargetRotation();

}
