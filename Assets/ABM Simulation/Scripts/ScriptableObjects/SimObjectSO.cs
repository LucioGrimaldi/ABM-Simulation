using UnityEngine;

[CreateAssetMenu(fileName = "Test", menuName = "ScriptableObjects/SimObjectSO")]
public abstract class SimObjectSO : ScriptableObject
{
    public Transform prefab;
    public Transform ghost;
    public Sprite sprite;

    public Vector3 higher = new Vector3(0,0.5f,0);

}
