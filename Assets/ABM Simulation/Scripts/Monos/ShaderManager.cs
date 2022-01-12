using UnityEngine;

public class ShaderManager : MonoBehaviour
{
    public ComputeBuffer[] computeBuffers;
    private GameObject simulationSpace;

    void Awake()
    {
        simulationSpace = this.gameObject;
    }

    private void OnDestroy()
    {
        computeBuffers[0].Dispose();
        computeBuffers[1].Dispose();
    }

}
