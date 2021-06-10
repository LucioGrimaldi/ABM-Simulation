using System.Collections.Generic;
using UnityEngine;

public class Agent : SimObjects
{
    public Agent(int id, string name, float x, float y, float z, GameObject prefab, Dictionary<string, object> parameters)
        : base(id, name, x, y, z, prefab, parameters)
    {
       
    }

    public Agent(string name)
    {
        Name = name;
    }

    public override bool Move(float x, float y, float z)
    {
        throw new System.NotImplementedException();
        // trigger gamecontroller to move
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
