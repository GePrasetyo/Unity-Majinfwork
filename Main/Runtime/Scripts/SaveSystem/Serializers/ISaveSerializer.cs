using System.IO;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Interface for save data serialization.
    /// Implement to support different formats (Binary, JSON, etc.)
    /// </summary>
    public interface ISaveSerializer {
        /// <summary>
        /// File extension for this serializer (e.g., ".dat", ".json").
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Serializes data to a stream.
        /// </summary>
        void Serialize<T>(Stream stream, T data) where T : class;

        /// <summary>
        /// Deserializes data from a stream.
        /// </summary>
        T Deserialize<T>(Stream stream) where T : class;
    }
}
