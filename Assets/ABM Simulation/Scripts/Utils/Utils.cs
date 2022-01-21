using Newtonsoft.Json;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public class Utils
{
    /// <summary>
    /// Gets Step 'complete' flag
    /// </summary>
    public static bool GetCompleteFlag(byte[] payload)
    {
        // Prepare Binary Stream of Gzipped payload
        using (MemoryStream decompress_inputStream = new MemoryStream(payload))
        using (GZipStream gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress))
        using (BinaryReader deserialize_binaryReader = new BinaryReader(gZipStream))
        {
            bool complete = BitConverter.ToBoolean(deserialize_binaryReader.ReadBytes(1).Reverse().ToArray(), 0);
            return complete;
        }
    }

    /// <summary>
    /// Gets Step id from Step Message payload
    /// </summary>
    public static long GetStepId(byte[] payload)
    {
        // Prepare Binary Stream of Gzipped payload
        using (MemoryStream decompress_inputStream = new MemoryStream(payload))
        using (GZipStream gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress))
        using (BinaryReader deserialize_binaryReader = new BinaryReader(gZipStream))
        {
            deserialize_binaryReader.ReadBytes(1);
            long id = BitConverter.ToInt64(deserialize_binaryReader.ReadBytes(8).Reverse().ToArray(), 0);
            return id;
        }
    }

    /// <summary>
    /// Decompresses Gzipped message payload and get payload byte[]
    /// </summary>
    public static byte[] DecompressStepPayload(byte[] payload)
    {
        // Prepare Binary Stream of Gzipped payload
        byte[] uncompressedPayload;

        using (MemoryStream decompress_inputStream = new MemoryStream(payload))
        using (MemoryStream ms = new MemoryStream())
        using (GZipStream gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress))
        using (BinaryReader deserialize_binaryReader = new BinaryReader(gZipStream))
        {
            gZipStream.CopyTo(ms);
            uncompressedPayload = ms.ToArray();
            return uncompressedPayload;
        }

    }

    /// <summary>
    /// Combine byte[]s
    /// </summary>
    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] bytes = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
        Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
        return bytes;
    }



    // TESTS
    public void tests()
    {
        //SendCheckStatus();
        //SendConnect();
        //SendDisconnect();
        //SendSimListRequest();
        //SendSimUpdate();
        //SendSimCommand("4", "2");
        //SendResponse("001");
        //SendErrorMessage("GENERIC_ERROR", true);

        //byte[] step, compressed;

        //using (MemoryStream ms = new MemoryStream())
        //using (BinaryWriter writer = new BinaryWriter(ms, System.Text.Encoding.BigEndianUnicode))
        //{
        //    writer.Write(0);
        //    writer.Write(2);
        //    writer.Write(1);
        //    writer.Write(0);
        //    writer.Write(1.5f);
        //    writer.Write(1.5f);
        //    writer.Write(1.5f);
        //    writer.Write(1);
        //    writer.Write(3.0f);
        //    writer.Write(3.0f);
        //    writer.Write(3.0f);
        //    writer.Write(666);
        //    writer.Write(1.0f);
        //    writer.Write(1.0f);
        //    writer.Write(1.0f);
        //    writer.Write(9001);


        //    step = ms.ToArray();
        //}

        //using (var outStream = new MemoryStream())
        //{
        //    using (var tinyStream = new System.IO.Compression.GZipStream(outStream, System.IO.Compression.CompressionMode.Compress))
        //    using (var mStream = new MemoryStream(step))
        //        mStream.CopyTo(tinyStream);

        //    compressed = outStream.ToArray();
        //}
        //UnityEngine.Debug.Log("SIM_STEP: \n" + Utils.StepToJSON(compressed, (JSONObject)JSON.Parse("{ \"id\" : 0, \"name\" : \"Flockers\", \"description\" : \"....\", \"type\" : \"qualitative\", \"dimensions\" : [ { \"name\" : \"x\", \"type\" : \"System.Single\", \"default\" : 500 }, { \"name\" : \"y\", \"type\" : \"System.Single\", \"default\" : 500 }, { \"name\" : \"z\", \"type\" : \"System.Single\", \"default\" : 500 } ], \"sim_params\" : [ { \"name\" : \"cohesion\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"avoidance\", \"type\" : \"System.Single\", \"default\" : 0.5 }, { \"name\" : \"randomness\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"consistency\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"momentum\", \"type\" : \"System.Single\", \"default\" : 1 }, { \"name\" : \"neighborhood\", \"type\" : \"System.Int32\", \"default\" : 10 }, { \"name\" : \"jump\", \"type\" : \"System.Single\", \"default\" : 0.7 }, ], \"agent_prototypes\" : [ { \"name\" : \"Flocker\", \"params\": [] } ], \"generic_prototypes\" : [ { \"name\" : \"Gerardo\", \"params\": [{ \"name\" : \"scimità\", \"type\" : \"System.Int32\", \"editable_in_play\" : true, \"editable_in_pause\" : true, \"value\" : 9001 }] } ]}")).ToString());

        //simulation.InitSimulationFromJSONEditedPrototype((JSONObject)JSON.Parse("{ \"id\": 0, \"name\": \"Flockers\", \"description\": \"....\", \"type\": \"qualitative\", \"dimensions\": [ { \"name\": \"x\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"y\", \"type\": \"System.Int32\", \"default\": 500 }, { \"name\": \"z\", \"type\": \"System.Int32\", \"default\": 500 } ], \"sim_params\": [ { \"name\": \"cohesion\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"avoidance\", \"type\": \"System.Single\", \"default\": \"0.5\" }, { \"name\": \"randomness\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"consistency\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"momentum\", \"type\": \"System.Single\", \"default\": 1 }, { \"name\": \"neighborhood\", \"type\": \"System.Int32\", \"default\": 10 }, { \"name\": \"jump\", \"type\": \"System.Single\", \"default\": 0.7 } ], \"agent_prototypes\": [ { \"class\": \"Flocker\", \"position\" : { \"x\" : 1, \"y\" : 1, \"z\" : 1 }, \"default\" : 10, \"params\": [] } ], \"generic_prototypes\": [ { \"class\": \"Gerardo\", \"position\" : { \"x\" : 1, \"y\" : 1, \"z\" : 1 }, \"default\" : 1, \"params\": [ { \"name\": \"scimità\", \"type\": \"System.Int32\", \"editable_in_play\": true, \"editable_in_pause\": true, \"default\": 9001 } ] } ] }"));
        //UnityEngine.Debug.Log("SIMULATION: \n" + simulation);
        //
        //
        ////simulation.UpdateSimulationFromJSONUpdate((JSONObject)JSON.Parse("{ \"sim_params\" : { \"cohesion\" : 1.9, \"neighborhood\" : 20 }, \"agents_update\" : [ { \"id\" : 0, \"class\" : \"Flocker\", \"position\" : { \"x\" : 10, \"y\" : 11, \"z\" : 7 }, \"params\" : {} }, { \"id\" : 1, \"class\" : \"Flocker\", \"position\" : { \"x\" : 9, \"y\" : 9, \"z\" : 9 }, \"params\" : {} } ], \"generics_update\" : [ { \"id\" : 0, \"class\" : \"Gerardo\", \"position\" : { \"x\" : 2, \"y\" : 2, \"z\" : 2 }, \"params\" : {\"scimità\" : 10000, \"breathtaking\" : true }}], \"obstacles_update\" : [] }"));
        ////UnityEngine.Debug.Log("SIMULATION: \n" + simulation);
        //
        ////StoreParameterUpdate("cohesion", "100.0");
        //SimObjectModifyEventArgs e = new SimObjectModifyEventArgs();
        //e.type = SimObjectType.GENERIC;
        //e.class_name = "Gerardo";
        //e.id = 0;
        //e.m_param = false;
        //e.m_position = true;
        //e.position = (0.1f, 0.1f, 0.1f);
        //StoreSimObjectModify(e);
        //
        //UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));
        //
        //SimObjectCreateEventArgs c = new SimObjectCreateEventArgs();
        //c.type = SimObjectType.GENERIC;
        //c.class_name = "Gerardo";
        //c.position = (0.1f, 0.1f, 0.1f);
        //c.parameters = new Dictionary<string, dynamic>();
        //StoreSimObjectCreate(c);
        //StoreSimObjectCreate(c);
        //StoreSimObjectCreate(c);
        //
        //UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));
        //
        //e.type = SimObjectType.GENERIC;
        //e.class_name = "Gerardo";
        //e.id = -1;
        //e.position = (9f, 9f, 9f);
        //e.param = ("scimità", 1);
        //e.m_position = true;
        //e.m_param = true;
        //StoreSimObjectModify(e);
        //
        //UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));
        //
        //e.type = SimObjectType.GENERIC;
        //e.class_name = "Gerardo";
        //e.id = -1;
        //e.position = (1f, 1f, 1f);
        //e.param = ("scimità", 999999);
        //e.m_position = true;
        //e.m_param = true;
        //StoreSimObjectModify(e);
        //
        //UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));
        //
        //SimObjectDeleteEventArgs d = new SimObjectDeleteEventArgs();
        //d.type = SimObjectType.GENERIC;
        //d.class_name = "Gerardo";
        //d.id = -1;
        //StoreSimObjectDelete(d);
        //
        //UnityEngine.Debug.Log("Uncommitted updates: \n" + string.Join("  ", uncommitted_updates));
    }

}

public class TupleConverter<U,V> : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof((U, V)) == objectType;
    }

    public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType == Newtonsoft.Json.JsonToken.Null) return null;

        var jObject = Newtonsoft.Json.Linq.JObject.Load(reader);
        var properties = jObject.Properties().ToList();
        return new ValueTuple<U, V>(jObject[properties[0].Name].ToObject<U>(), jObject[properties[1].Name].ToObject<V>());
    }

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(((ValueTuple<string, V>)value).Item1);
        writer.WriteValue(((ValueTuple<string, V>)value).Item2);
        writer.WriteEndObject();
    }

}

public class Vec4Conv : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(Vector4))
        {
            return true;
        }
        return false;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var t = serializer.Deserialize(reader);
        var iv = JsonConvert.DeserializeObject<Vector4>(t.ToString());
        return iv;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector4 v = (Vector4)value;

        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(v.x);
        writer.WritePropertyName("y");
        writer.WriteValue(v.y);
        writer.WritePropertyName("z");
        writer.WriteValue(v.z);
        writer.WritePropertyName("w");
        writer.WriteValue(v.w);
        writer.WriteEndObject();
    }
}

public class Vec3Conv : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(Vector3))
        {
            return true;
        }
        return false;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var t = serializer.Deserialize(reader);
        var iv = JsonConvert.DeserializeObject<Vector3>(t.ToString());
        return iv;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector3 v = (Vector3)value;

        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(v.x);
        writer.WritePropertyName("y");
        writer.WriteValue(v.y);
        writer.WritePropertyName("z");
        writer.WriteValue(v.z);
        writer.WriteEndObject();
    }
}

public class Vec2Conv : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(Vector2))
        {
            return true;
        }
        return false;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var t = serializer.Deserialize(reader);
        var iv = JsonConvert.DeserializeObject<Vector2>(t.ToString());
        return iv;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector2 v = (Vector2)value;

        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(v.x);
        writer.WritePropertyName("y");
        writer.WriteValue(v.y);
        writer.WriteEndObject();
    }
}

public class QuaternionConv : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Quaternion) == objectType;
    }

    public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType == Newtonsoft.Json.JsonToken.Null) return null;

        var jObject = Newtonsoft.Json.Linq.JObject.Load(reader);
        var properties = jObject.Properties().ToList();
        return new Quaternion(jObject[properties[0].Name].ToObject<float>(), jObject[properties[1].Name].ToObject<float>(), jObject[properties[2].Name].ToObject<float>(), jObject[properties[3].Name].ToObject<float>());
    }

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
        Quaternion v = (Quaternion)value;

        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(v.x);
        writer.WritePropertyName("y");
        writer.WriteValue(v.y);
        writer.WritePropertyName("z");
        writer.WriteValue(v.z);
        writer.WritePropertyName("w");
        writer.WriteValue(v.w);
        writer.WriteEndObject();
    }

}




public class MyList<T> : List<T>
{
    public MyList() { }
    public MyList(IEnumerable<T> collection) : base(collection) {}
    public override string ToString()
    {
        return string.Join("  ", this.ToArray());
    }
}