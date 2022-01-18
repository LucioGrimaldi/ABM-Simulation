using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PO_Continuous2D : PO_Continuous
{
    // NON-STATIC
    [SerializeField] protected DirEnum direction;

    public enum DirEnum
    {
        NORD,
        NORD_EST,
        EST,
        SUD_EST,
        SUD,
        SUD_OVEST,
        OVEST,
        NORD_OVEST
    }

    public DirEnum Direction { get => direction; set => direction = value; }


    public override void Rotate()
    {
        throw new System.NotImplementedException();
    }
}
