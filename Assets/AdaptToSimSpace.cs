using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdaptToSimSpace : MonoBehaviour
{
    private void Start()
    {
        SimSpaceSystem simSpace = GameObject.Find("SimSpaceSystem").GetComponent<SimSpaceSystem>();
        transform.localPosition = new Vector3(transform.localPosition.x, simSpace.simSpaceDimensions.Equals(SimSpaceSystem.SimSpaceDimensionsEnum._2D) ? 10f : 50f, transform.localPosition.z);
    }
}
