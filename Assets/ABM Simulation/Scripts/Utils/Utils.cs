using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class Utils
{
    /// Decompression/Deserialize Streams/Readers
    private static MemoryStream decompress_inputStream;
    private static MemoryStream deserialize_inputStream;
    private static BinaryReader deserialize_binaryReader;
    private static GZipStream gZipStream;

    /// <summary>
    /// Gets Step id from Step Message payload
    /// </summary>
    public static long GetStepId(byte[] payload)
    {

        // Prepare Binary Stream of Gzipped payload
        decompress_inputStream = new MemoryStream(payload);
        gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress);
        deserialize_binaryReader = new BinaryReader(gZipStream);

        long id = deserialize_binaryReader.ReadInt64();

        gZipStream.Flush();
        gZipStream.Dispose();
        gZipStream.Close();
        decompress_inputStream.Flush();
        decompress_inputStream.Dispose();
        decompress_inputStream.Close();
        deserialize_binaryReader.Dispose();
        deserialize_binaryReader.Close();


        return id;
    }

    /// <summary>
    /// Decompresses Gzipped message payload and get payload byte[]
    /// </summary>
    public static byte[] DecompressStepPayload(byte[] payload)
    {
        // Prepare Binary Stream of Gzipped payload
        decompress_inputStream = new MemoryStream(payload);
        gZipStream = new GZipStream(decompress_inputStream, CompressionMode.Decompress);
        deserialize_binaryReader = new BinaryReader(gZipStream);
        byte[] uncompressedPayload;
        using (MemoryStream ms = new MemoryStream())
        {
            gZipStream.CopyTo(ms);
            uncompressedPayload = ms.ToArray();
        }

        gZipStream.Flush();
        gZipStream.Dispose();
        gZipStream.Close();
        decompress_inputStream.Flush();
        decompress_inputStream.Dispose();
        decompress_inputStream.Close();
        deserialize_binaryReader.Dispose();
        deserialize_binaryReader.Close();

        return uncompressedPayload;
    }

    /// <summary>
    /// Extract step as JSONObject from step as byte[] 
    /// </summary>
    public static JSONObject StepToJSON(byte[] step, JSONObject sim_prototype)
    {
        // variabili
        JSONObject sim_step = new JSONObject();
        byte[] decompressed_step = DecompressStepPayload(step);

        JSONArray element_array = new JSONArray();
        JSONObject element = new JSONObject();
        JSONArray element_positions = new JSONArray();
        JSONObject a = new JSONObject();
        JSONArray element_params = new JSONArray();

        List<string> agent_class_names = new Func<List<string>>(() => {
            List<string> array = new List<string>();
            foreach (JSONObject p in (JSONArray)sim_prototype["agent_prototypes"])
            {
                array.Add(p["name"]);
            }
            return array;
        })();                                     // nomi delle classi degli agenti
        List<string> generic_class_names = new Func<List<string>>(() => {
            List<string> array = new List<string>();
            foreach (JSONObject p in (JSONArray)sim_prototype["generic_prototypes"])
            {
                array.Add(p["name"]);
            }
            return array;
        })();                                   // nomi delle classi degli oggetti
        List<List<Tuple<string, string>>> agent_params_for_each_class =
            new Func<List<List<Tuple<string, string>>>>(() => {
            List<List<Tuple<string, string>>> array = new List<List<Tuple<string, string>>>();
            for (int i = 0; i < ((JSONArray)sim_prototype["agent_prototypes"]).Count; i++)
            {
                JSONObject c = (JSONObject)((JSONArray)sim_prototype["agent_prototypes"])[i];
                array.Add(new List<Tuple<string, string>>());
                foreach (JSONObject p in c["params"] as JSONArray)
                {
                        array[i].Add(new Tuple<string,string>(p["name"], p["type"]));
                }
            }
            return array;
        })();                                             // (nome_param, tipo_param) per ogni parametro per ogni classe di agente 
        List<List<Tuple<string, string>>> generic_params_for_each_class =
            new Func<List<List<Tuple<string, string>>>>(() =>
        {
            List<List<Tuple<string, string>>> array = new List<List<Tuple<string, string>>>();
            for (int i = 0; i < ((JSONArray)sim_prototype["generic_prototypes"]).Count; i++)
            {
                JSONObject c = (JSONObject)((JSONArray)sim_prototype["generic_prototypes"])[i];
                array.Add(new List<Tuple<string, string>>());
                foreach (JSONObject p in c["params"] as JSONArray)
                {
                    array[i].Add(new Tuple<string, string>(p["name"], p["type"]));
                }
            }
            return array;
        })();                                              // (nome_param, tipo_param) per ogni parametro per ogni classe di oggetto 
        int n_agents_classes = ((JSONArray)sim_prototype["agent_prototypes"]).Count;                              // numero di classi di agenti aggiornati nello step
        int[] n_agents_for_each_class = new int[n_agents_classes];                                                // numero di agenti di ogni classe presenti nello step
        int n_generic_classes = ((JSONArray)sim_prototype["generic_prototypes"]).Count;                           // numero di classi di oggetti aggiornati nello step
        int[] n_objects_for_each_class = new int[n_generic_classes];                                              // numero di oggetti di ogni classe presenti nello step
        List<char> dimensions = new Func<List<char>>(() => {
            List<char> array = new List<char>();
            foreach(JSONObject d in (JSONArray) sim_prototype["dimensions"])
            {
                UnityEngine.Debug.Log(d["name"].ToString());
                array.Add(d["name"].ToString()[1]);
            }
        return array;})();                                                                                        // dimensioni della sim come una lista di char (es. ['x', 'y', 'z'])

        // creo stream e reader per leggere lo step
        deserialize_inputStream = new MemoryStream(decompressed_step);
        deserialize_binaryReader = new BinaryReader(deserialize_inputStream);

        // estraggo l'ID
        sim_step.Add("id", deserialize_binaryReader.ReadInt32());
        // estraggo il numero di agenti per classe presenti nello step
        for(int i = 0; i < n_agents_classes; i++)
        {
            n_agents_for_each_class[i] = deserialize_binaryReader.ReadInt32();
        }
        // estraggo il numero di oggetti per classe presenti nello step
        for (int i = 0; i < n_generic_classes; i++)
        {
            n_objects_for_each_class[i] = deserialize_binaryReader.ReadInt32();
        }

        // AGENTI
        // estraggo ogni agente per classe
        for (int i = 0; i < n_agents_classes; i++)                // i è la classe
        {
            int n_agents_of_a_class = n_agents_for_each_class[i];

            if(n_agents_of_a_class == 0) { continue; }                                                            //se non ci sono elementi di quella classe è inutile continuare

            for (int j = 0; j < n_agents_of_a_class; j++)                       // j è l'agente
            {
                element.Add("class", agent_class_names[i]);
                element.Add("id", deserialize_binaryReader.ReadInt32());

                // position
                foreach (char d in "xyz")
                {
                    if (dimensions.Contains(d))
                    {
                        a.Add(d + "", deserialize_binaryReader.ReadSingle());
                        element_positions.Add(a);
                    }
                    else
                    {
                        a.Add(d + "", 0);
                        element_positions.Add(a);
                    }
                    a = new JSONObject();
                }
                element.Add("positions", element_positions);
                element_positions = new JSONArray();

                // params
                foreach(Tuple<string,string> p in agent_params_for_each_class[i])
                {
                    
                    switch (p.Item2)
                    {
                        case "System.Single":
                            a.Add(p.Item1, deserialize_binaryReader.ReadSingle());
                            element_params.Add(a);
                            break;
                        case "System.Int32":
                            a.Add(p.Item1, deserialize_binaryReader.ReadInt32());
                            element_params.Add(a);
                            break;
                        case "System.Boolean":
                            a.Add(p.Item1, deserialize_binaryReader.ReadBoolean());
                            element_params.Add(a); break;
                        case "System.String":
                            a.Add(p.Item1, deserialize_binaryReader.ReadString());
                            element_params.Add(a);
                            break;
                    }
                    a = new JSONObject();
                }
                element.Add("params", element_params);
                element_array.Add(element);
                element = new JSONObject();
                element_params = new JSONArray();
            }
        }
        sim_step.Add("agents_update", element_array);
        element_array = new JSONArray();

        // OGGETTI
        // estraggo ogni oggetto per classe
        for (int i = 0; i < n_objects_for_each_class.Length; i++)                // i è la classe
        {
            int n_objects_of_a_class = n_objects_for_each_class[i];

            if (n_objects_of_a_class == 0) { continue; }                                                            //se non ci sono elementi di quella classe è inutile continuare

            for (int j = 0; j < n_objects_of_a_class; j++)                       // j è l'oggetto
            {
                element.Add("class", generic_class_names[i]);
                element.Add("id", deserialize_binaryReader.ReadInt32());

                // position
                foreach (char d in "xyz")
                {
                    if (dimensions.Contains(d))
                    {
                        a.Add(d + "", deserialize_binaryReader.ReadSingle());
                        element_positions.Add(a);
                    }
                    else
                    {
                        a.Add(d + "", 0);
                        element_positions.Add(a);
                    }
                    a = new JSONObject();
                }
                element.Add("positions", element_positions);

                // params
                foreach (Tuple<string, string> p in generic_params_for_each_class[i])
                {

                    switch (p.Item2)
                    {
                        case "System.Single":
                            a.Add(p.Item1, deserialize_binaryReader.ReadSingle());
                            element_params.Add(a);
                            break;
                        case "System.Int32":
                            a.Add(p.Item1, deserialize_binaryReader.ReadInt32());
                            element_params.Add(a);
                            break;
                        case "System.Boolean":
                            a.Add(p.Item1, deserialize_binaryReader.ReadBoolean());
                            element_params.Add(a); break;
                        case "System.String":
                            a.Add(p.Item1, deserialize_binaryReader.ReadString());
                            element_params.Add(a);
                            break;
                    }
                    a = new JSONObject();
                }
                element.Add("params", element_params);
                element_array.Add(element);
                element = new JSONObject();
                element_params = new JSONArray();

            }
        }
        sim_step.Add("generic_update", element_array);

        return sim_step;
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



    // ROBA UTILE

    public void AddObstacle(string name, float x, float y, float z, List<(dynamic, dynamic, dynamic)> cells)
    {
    //    SimObject o = new SimObject(name, x, y, z, cells);
    //    o.Id = simulation.Obstacles.Count;
    //    simulation.AddObstacle(o);
    //}
    //public void RemoveObstacle(int id)
    //{
    //    simulation.RemoveObstacle(id.ToString());
    //}
    //public void MoveObstacle(int id, float x, float y, float z)
    //{
    //    simulation.MoveObstacle(id, x, y, z);
    }


    //ROBA PER MASON
    public void UpdateSimulationFromUpdate(JSONObject sim_updateJSON, Dictionary<string, SimObject> sim_update)
    {
        //// SIM_PARAMS
        //JSONObject parameters = (JSONObject)sim_updateJSON["sim_params"];
        //
        //foreach (KeyValuePair<string, JSONNode> p in parameters.Dict)
        //{
        //    Parameters[p.Key] = p.Value;
        //}
        //
        //// AGENTS
        //JSONArray agents = (JSONArray)sim_updateJSON["agents_update"];
        //foreach (JSONObject agent in agents)
        //{
        //    if (!agent["id"].Equals("all"))
        //    {
        //        Agents.TryGetValue(agent["class"] + "." + agent["id"], out Agent a);
        //        if (a == null) { } // C'è qualche problema
        //
        //        a.X = ((JSONObject)agent["position"])["x"];
        //        a.Y = ((JSONObject)agent["position"])["y"];
        //        a.Z = ((JSONObject)agent["position"])["z"];
        //
        //        foreach (KeyValuePair<string, JSONNode> p in ((JSONObject)agent["params"]).Dict)
        //        {
        //            a.Parameters[p.Key] = p.Value;
        //        }
        //    }
        //    else
        //    {
        //        foreach (Agent a in Agents.Values)
        //        {
        //            if (a.Name.Equals(agent["class"]))
        //            {
        //                a.X = ((JSONObject)agent["position"])["x"];
        //                a.Y = ((JSONObject)agent["position"])["y"];
        //                a.Z = ((JSONObject)agent["position"])["z"];
        //
        //                foreach (KeyValuePair<string, JSONNode> p in ((JSONObject)agent["params"]).Dict)
        //                {
        //                    a.Parameters[p.Key] = p.Value;
        //                }
        //            }
        //        }
        //    }
        //}
        //
        //// OBSTACLES
        //
        //
        //
        //
        //
        //// GENERICS
        //JSONArray generics = (JSONArray)sim_updateJSON["generics_update"];
        //foreach (JSONObject generic in generics)
        //{
        //    if (!generic["id"].Equals("all"))
        //    {
        //        Generics.TryGetValue(generic["class"] + "." + generic["id"], out Generic g);
        //        if (g == null) { } // C'è qualche problema
        //
        //        g.X = ((JSONObject)generic["position"])["x"];
        //        g.Y = ((JSONObject)generic["position"])["y"];
        //        g.Z = ((JSONObject)generic["position"])["z"];
        //
        //        foreach (KeyValuePair<string, JSONNode> p in ((JSONObject)generic["params"]).Dict)
        //        {
        //            g.Parameters[p.Key] = p.Value;
        //        }
        //    }
        //    else
        //    {
        //        foreach (Generic g in Generics.Values)
        //        {
        //            if (g.Name.Equals(generic["class"]))
        //            {
        //                g.X = ((JSONObject)generic["position"])["x"];
        //                g.Y = ((JSONObject)generic["position"])["y"];
        //                g.Z = ((JSONObject)generic["position"])["z"];
        //
        //                foreach (KeyValuePair<string, JSONNode> p in ((JSONObject)generic["params"]).Dict)
        //                {
        //                    g.Parameters[p.Key] = p.Value;
        //                }
        //            }
        //        }
        //    }
        //}
    }

}
