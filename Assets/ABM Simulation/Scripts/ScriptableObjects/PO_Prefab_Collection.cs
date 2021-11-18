using System.Collections.Generic;
using UnityEngine;
using static SceneController;

[CreateAssetMenu(fileName = "Collection", menuName = "Sim ObjectRenders/Prefab Collection")]
public class PO_Prefab_Collection : ScriptableObject
{
    [SerializeField] public List<NamedPrefab> PO_AgentPrefabs;
    [SerializeField] public List<NamedPrefab> PO_GenericPrefabs;
    [SerializeField] public List<NamedPrefab> PO_ObstaclePrefabs;

}
