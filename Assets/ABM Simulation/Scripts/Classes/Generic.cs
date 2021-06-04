using System.Collections.Generic;
using UnityEngine;

public class Generic : SimObjects
{
    public Generic(string id, string name, float x, float y, float z, GameObject prefab, Dictionary<string, string> parameters)
        : base(id, name, x, y, z, prefab, parameters)
    {

    }

    public Generic(string name)
    {
        Name = name;
    }
    public override bool Move(float x, float y, float z)
    {
        throw new System.NotImplementedException();
    }

    public override bool Remove()
    {
        throw new System.NotImplementedException();
    }

    public override bool RotateX(float rotation)
    {
        throw new System.NotImplementedException();
    }

    public override bool RotateY(float rotation)
    {
        throw new System.NotImplementedException();
    }

    public override bool RotateZ(float rotation)
    {
        throw new System.NotImplementedException();
    }

    public override bool Scale(float multiplier)
    {
        throw new System.NotImplementedException();
    }
}
