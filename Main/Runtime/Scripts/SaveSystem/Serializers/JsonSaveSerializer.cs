using System.IO;
using System.Text;
using UnityEngine;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// JSON serializer using Unity's JsonUtility.
    /// Human-readable format, good for debugging.
    /// </summary>
    public class JsonSaveSerializer : ISaveSerializer {
        public string FileExtension => ".json";

        private readonly bool prettyPrint;
        private readonly Encoding encoding;

        /// <summary>
        /// Creates a JSON serializer.
        /// </summary>
        /// <param name="prettyPrint">Whether to format JSON with indentation.</param>
        /// <param name="encoding">Text encoding to use. Defaults to UTF8.</param>
        public JsonSaveSerializer(bool prettyPrint = false, Encoding encoding = null) {
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
    }

    /// <summary>
    /// JSON serializer with GZip compression for smaller file sizes.
    /// </summary>
    public class CompressedJsonSaveSerializer : ISaveSerializer {
        public string FileExtension => ".json.gz";

        private readonly bool prettyPrint;
        private readonly Encoding encoding;

        public CompressedJsonSaveSerializer(bool prettyPrint = false, Encoding encoding = null) {
            this.prettyPrint = prettyPrint;
            this.encoding = encoding ?? Encoding.UTF8;
        }

        public void Serialize<T>(Stream stream, T data) where T : class {
            var json = JsonUtility.ToJson(data, prettyPrint);
            var bytes = encoding.GetBytes(json);

            using (var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Compress, leaveOpen: true)) {
                gzip.Write(bytes, 0, bytes.Length);
            }
        }

        public T Deserialize<T>(Stream stream) where T : class {
            using (var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
            using (var reader = new StreamReader(gzip, encoding)) {
                var json = reader.ReadToEnd();
                return JsonUtility.FromJson<T>(json);
            }
        }
    }
}
