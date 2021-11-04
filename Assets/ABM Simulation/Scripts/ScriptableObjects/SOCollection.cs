using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Test", menuName = "ScriptableObjects/SOCollection")]
public class SOCollection : ScriptableObject
{
    public List<SimObjectSO> agents;
    public List<SimObjectSO> generics;
    public List<SimObjectSO> obstacles;

    public List<string> agentClass_names;
    public List<string> genericClass_names;
    public List<string> obstacleClass_names;
}
