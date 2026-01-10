using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Binary serializer using BinaryFormatter.
    /// Provides compact save files but requires [Serializable] attribute.
    /// </summary>
    public class BinarySaveSerializer : ISaveSerializer {
        public string FileExtension => ".dat";

        private readonly BinaryFormatter formatter = new BinaryFormatter();

        public void Serialize<T>(Stream stream, T data) where T : class {
            formatter.Serialize(stream, data);
        }

        public T Deserialize<T>(Stream stream) where T : class {
            return (T)formatter.Deserialize(stream);
        }
    }
}
