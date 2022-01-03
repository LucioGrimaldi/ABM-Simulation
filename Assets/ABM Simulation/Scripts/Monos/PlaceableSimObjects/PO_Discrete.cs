using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PO_Discrete : PlaceableObject
{
    public abstract Vector3Int GetRotationOffset();
    public abstract object GetCells();
    
}
