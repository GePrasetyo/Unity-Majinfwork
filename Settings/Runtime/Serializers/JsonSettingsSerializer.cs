using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Majinfwork.Settings {
    /// <summary>
    /// JSON serializer using Unity's JsonUtility.
    /// Human-readable format, preferred for settings files.
    /// </summary>
    public class JsonSettingsSerializer : ISettingsSerializer {
        public string FileExtension => ".json";

        private readonly bool prettyPrint;
        private readonly Encoding encoding;

        public JsonSettingsSerializer(bool prettyPrint = true, Encoding encoding = null) {
            this.prettyPrint = prettyPrint;
            this.encoding = encoding ?? Encoding.UTF8;
        }

        public void Serialize<T>(Stream stream, T data) where T : class {
            var json = JsonUtility.ToJson(data, prettyPrint);
            var bytes = encoding.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
        }

        public T Deserialize<T>(Stream stream) where T : class {
            using (var reader = new StreamReader(stream, encoding)) {
                var json = reader.ReadToEnd();
                return JsonUtility.FromJson<T>(json);
            }
        }

        public object Deserialize(Stream stream, Type type) {
            using (var reader = new StreamReader(stream, encoding)) {
                var json = reader.ReadToEnd();
                return JsonUtility.FromJson(json, type);
            }
        }
    }
}
