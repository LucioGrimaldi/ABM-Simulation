using System.Collections.Generic;
using DeepCopyExtensions;
public class SimObject
{
    private SimulationController.SimObjectType type;
    private string class_name;
    private int id;
    private float x, y, z;
    private Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();

    public SimulationController.SimObjectType Type { get => type; set => type = value; }
    public string Class_name { get => class_name; set => class_name = value; }
    public int Id { get => id; set => id = value; }
    public float X { get => x; set => x = value; }
    public float Y { get => y; set => y = value; }
    public float Z { get => z; set => z = value; }
    public Dictionary<string, dynamic> Parameters { get => parameters; set => parameters = value; }

    public SimObject(SimulationController.SimObjectType type, string class_name, int id, float x, float y, float z, Dictionary<string, dynamic> parameters)
    {
        this.Type = type;
        this.Class_name = class_name;
        this.Id = id;
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.Parameters = parameters;
    }

    public SimObject()
    {

    }
    public SimObject(string class_name)
    {
        Class_name = class_name;
    }



    public bool AddParameter(string param_name, dynamic value)
    {
        if (!Parameters.ContainsKey(param_name))
        {
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }
    public bool UpdateParameter(string param_name, dynamic value)
    {
        if (Parameters.ContainsKey(param_name))
        {
            Parameters.Remove(param_name);
            Parameters.Add(param_name, value);
            return true;
        }
        return false;
    }
    public dynamic GetParameter(string param_name)
    {
        dynamic parameter;
        Parameters.TryGetValue(param_name, out parameter);
        return (!parameter.Equals(null)) ? parameter : false;
    }

    public SimObject Clone()
    {
        return this.DeepCopyByExpressionTree();
    }

    public override string ToString()
    {
        return "{Type: " + type + "| Class: " + class_name + "| Id: " + id + "| x: " + x + ", y: " + y + ", z: " + z + "| Params: " + string.Join("  ", parameters) + "}";
    }
}
