using UnityEngine;

public class ShaderManager : MonoBehaviour
{
    public ComputeBuffer[] computeBuffers;
    private GameObject simulationSpace;

    void Awake()
    {
        simulationSpace = this.gameObject;
    }


}
