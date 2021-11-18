using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Collection", menuName = "Sim Objects/Obstacle Collection")]
public class Obstacle_SO_Collection : ScriptableObject
{
    public List<SimObject> Obstacles = new List<SimObject>()
    {
        { new SimObject(SimObject.SimObjectType.OBSTACLE, "Barrell", 0, new ConcurrentDictionary<string, object>( new Dictionary<string, object>() { {"position", new MyList<Vector2Int>() { Vector2Int.zero } } }))},
        { new SimObject()},
        { new SimObject()},
        { new SimObject()},
        { new SimObject()},
        { new SimObject()},
        { new SimObject()},
        { new SimObject()},

    };

}
