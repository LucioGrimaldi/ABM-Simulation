using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneController : MonoBehaviour
{

    ///// Game-related Assets ///
    //private GameObject simSpace;
    //private Vector3 simSpacePosition;
    //private Quaternion simSpaceRotation;
    //private bool AGENTS_READY = false;

    //// Access Methods

    //// Start is called before the first frame update
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    //private void InstantiateAgents()
    //{
    //    simSpacePosition = simSpace.transform.position;
    //    simSpaceRotation = simSpace.transform.rotation;
    //    for (int i = 0; i < flockSim.NumAgents; i++)
    //    {
    //        agents.Add(Instantiate(AgentPrefab, simSpacePosition, simSpaceRotation));
    //        agents[i].transform.SetParent(simSpace.transform);

    //    }
    //    AGENTS_READY = true;
    //    Debug.Log("Agents Instantiated.");
    //}

    //private void DestroyAgents()
    //{
    //    for (int i = 0; i < flockSim.NumAgents; i++)
    //    {
    //        Destroy(agents[i]);
    //    }
    //    agents.Clear();
    //    AGENTS_READY = false;
    //    Debug.Log("Agents Destroyed.");
    //}

    //private void UpdateAgents()
    //{
    //    if (AGENTS_READY == true)
    //    {
    //        long current_id = Ready_buffer.Item1;
    //        if (current_id > CurrentSimStep)
    //        {
    //            for (int i = 0; i < Ready_buffer.Item2.Length; i++)
    //            {
    //                var old_pos = agents[i].transform.position;
    //                agents[i].transform.localPosition = Ready_buffer.Item2[i];
    //                Vector3 velocity = agents[i].transform.position - old_pos;
    //                if (!velocity.Equals(Vector3.zero))
    //                {
    //                    agents[i].transform.rotation = Quaternion.Slerp(agents[i].transform.rotation, Quaternion.LookRotation(velocity, Vector3.up), 4 * Time.deltaTime);
    //                }
    //            }
    //            CurrentSimStep = current_id;
    //        }
    //    }
    //}


}
