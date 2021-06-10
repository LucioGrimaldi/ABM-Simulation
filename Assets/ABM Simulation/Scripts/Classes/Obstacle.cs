using System.Collections.Generic;
using System;
using UnityEngine;

public class Obstacle : SimObjects
{
    public static int quantity = 0;
    private string type;
    private List<(string, string)> cells;

    public Obstacle(int id, string name, string type, float x, float y, float z, GameObject prefab, List<(string, string)> cells, Dictionary<string, object> parameters)
        : base(id, name, x, y, z, prefab, parameters)
    {
        Type = type;
        Cells = cells;
    }

    public Obstacle(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public List<(string, string)> Cells { get => cells; set => cells = value; }
    public string Type { get => type; set => type = value; }

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
