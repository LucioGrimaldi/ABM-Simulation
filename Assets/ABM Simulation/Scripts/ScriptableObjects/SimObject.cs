using System;
using System.Collections.Concurrent;
using DeepCopyExtensions;
using SimpleJSON;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "SimObjects", menuName = "Sim Objects/Sim Object")]
public class SimObject : ScriptableObject
{
    [SerializeField] private SimObjectType type;
    [SerializeField] private string class_name;
    [SerializeField] private int id;
    [SerializeField] private bool is_in_step = false;
    [SerializeField] private bool to_keep_if_not_in_step = false;
    [SerializeField] private string layer = "default";
    [SerializeField] private bool shares_position = false;

    //[SerializeField] private StringObjectDictionary parameters;
    private ConcurrentDictionary<String, object> parameters = new ConcurrentDictionary<string, object>();

    public SimObjectType Type { get => type; set => type = value; }
    public string Class_name { get => class_name; set => class_name = value; }
    public int Id { get => id; set => id = value; }
    public ConcurrentDictionary<String, object> Parameters { get => parameters; set => parameters = value; }
    public bool Is_in_step { get => is_in_step; set => is_in_step = value; }
    public bool To_keep_if_not_in_step { get => to_keep_if_not_in_step; set => to_keep_if_not_in_step = value; }
    public string Layer { get => layer; set => layer = value; }
    public bool Shares_position { get => shares_position; set => shares_position = value; }

    public enum SimObjectType
    {
        AGENT = 0,
        GENERIC = 1,
        OBSTACLE = 2
    }

    public SimObject() {}

    public object GetParameter(string param_name)
    {
        object parameter;
        Parameters.TryGetValue(param_name, out parameter);
        return (!parameter.Equals(null)) ? parameter : false;
    }
    public bool AddParameter(string param_name, object value)
    {
        try
        {
            Parameters.TryAdd(param_name, value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    public bool UpdateParameter(string param_name, object value)
    {
        Parameters.AddOrUpdate(param_name, value, (key, old_value) => value);
        return true;
    }
    public JSONArray GetParametersJSON()
    {
        return (JSONArray) JSON.Parse(JsonUtility.ToJson(Parameters));
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
