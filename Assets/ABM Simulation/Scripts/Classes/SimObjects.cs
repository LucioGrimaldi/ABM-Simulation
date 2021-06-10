using System.Collections.Generic;
using UnityEngine;

public abstract class SimObjects : Editable
{
    private int id;
    private string name;
    private float x, y, z;
    public GameObject prefab;
    private GameObject gameObject;
    private Dictionary<string, object> parameters;

    public int Id { get => id; set => id = value; }
    public string Name { get => name; set => name = value; }
    public float X { get => x; set => x = value; }
    public float Y { get => y; set => y = value; }
    public float Z { get => z; set => z = value; }
    public GameObject Prefab { get => prefab; set => prefab = value; }
    public Dictionary<string, object> Parameters { get => parameters; set => parameters = value; }
    public GameObject GameObject { get => gameObject; set => gameObject = value; }

    public SimObjects(int id, string name, float x, float y, float z, GameObject prefab, Dictionary<string, object> parameters)
    {
        this.Id = id;
        this.Name = name;
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.Prefab = prefab;
        this.Parameters = parameters;
    }

    public SimObjects()
    {

    }

    public bool AddParameter(string param_name, object value)
    {
        if (!Parameters.ContainsKey(param_name))
        {
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }
    public bool UpdateParameter(string param_name, object value)
    {
        if (Parameters.ContainsKey(param_name))
        {
            Parameters.Remove(param_name);
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }
    public object GetParameter(string param_name)
    {
        object parameter;
        Parameters.TryGetValue(param_name, out parameter);
        return (!parameter.Equals(null)) ? parameter : false;
    }

    public abstract bool Move(float x, float y, float z);
    public abstract bool RotateX(float rotation);
    public abstract bool RotateY(float rotation);
    public abstract bool RotateZ(float rotation);
    public abstract bool Scale(float multiplier);
    public abstract bool Remove();
}
