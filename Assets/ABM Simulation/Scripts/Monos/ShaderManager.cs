using UnityEngine;

public class ShaderManager : MonoBehaviour
{
    public ComputeBuffer[] computeBuffers;
    private GameObject simulationSpace;
    public float[,] f_cells;
    public float[,] h_cells;

    void Awake()
    {
        simulationSpace = this.gameObject;
    }

    private void LateUpdate()
    {
        computeBuffers[0].SetCounterValue(0);
        computeBuffers[1].SetCounterValue(0);
        computeBuffers[0].SetData(f_cells);
        computeBuffers[1].SetData(h_cells);
    }

    private void OnDestroy()
    {
        computeBuffers[0].Dispose();
        computeBuffers[1].Dispose();
    }

}
