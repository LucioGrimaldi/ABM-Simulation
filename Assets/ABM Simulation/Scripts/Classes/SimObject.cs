using System.Collections.Concurrent;
using System.Collections.Generic;
using DeepCopyExtensions;
public class SimObject
{
    private SimObjectType type;
    private string class_name;
    private int id;
    private ConcurrentDictionary<string, object> parameters = new ConcurrentDictionary<string, object>();

    public SimObjectType Type { get => type; set => type = value; }
    public string Class_name { get => class_name; set => class_name = value; }
    public int Id { get => id; set => id = value; }
    public ConcurrentDictionary<string, object> Parameters { get => parameters; set => parameters = value; }

    public enum SimObjectType
    {
        AGENT = 0,
        GENERIC = 1,
        OBSTACLE = 2
    }

    public SimObject(SimObjectType type, string class_name, int id, ConcurrentDictionary<string, object> parameters)
    {
        this.Type = type;
        this.Class_name = class_name;
        this.Id = id;
        this.Parameters = parameters;
    }
    public SimObject() {}
    public SimObject(string class_name)
    {
        Class_name = class_name;
    }


    public object GetParameter(string param_name)
    {
        object parameter;
        Parameters.TryGetValue(param_name, out parameter);
        return (!parameter.Equals(null)) ? parameter : false;
    }
    public bool AddParameter(string param_name, object value)
    {
        if (Parameters.TryAdd(param_name, value)) return true; else return false;
    }
    public bool UpdateParameter(string param_name, object value)
    {
        if (Parameters.AddOrUpdate(param_name, value, (k,v) => { return value; }).Equals(value)) return true; else return false;
    }


    public SimObject Clone()
    {
        return this.DeepCopyByExpressionTree();
    }
    public override string ToString()
    {
        return "{Type: " + type + " | Class: " + class_name + " | Id: " + id + " | Params: " + string.Join("  ", parameters) + "}";
    }
}
