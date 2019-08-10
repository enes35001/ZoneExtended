using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace ExtendedZones
{
    class JsonHelper
    {
        public static T Deserialize<T>(string stringJson)
        {
            return JsonConvert.DeserializeObject<T>(stringJson);
        }

        public static string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static void SaveFile<T>(T obj, string path)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                StreamWriter sw = File.CreateText(path);

                using (JsonWriter writerr = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writerr, obj);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error saveFile: " + ex);
            }
        }

        public static T ReadyFile<T>(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
                }
                else
                {
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error saveFile: " + ex);
                return default(T);
            }

        }
    }
}