using UnityEngine;

[CreateAssetMenu(fileName = "Test", menuName = "ScriptableObjects/SimObjectContinuousSO")]
public class SimObjectContinuousSO : SimObjectSO
{
    public dynamic GetPosition(Vector3 worldPosition, PlaceableObject.Dir direction)
    {
        return worldPosition;
    }
}
