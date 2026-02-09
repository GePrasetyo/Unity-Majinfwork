using System;
using System.IO;

namespace Majinfwork.Settings {
    /// <summary>
    /// Interface for settings data serialization.
    /// </summary>
    public interface ISettingsSerializer {
        /// <summary>
        /// File extension including the dot (e.g., ".json").
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Serializes data to a stream.
        /// </summary>
        void Serialize<T>(Stream stream, T data) where T : class;

        /// <summary>
        /// Deserializes data from a stream using a compile-time type.
        /// </summary>
        T Deserialize<T>(Stream stream) where T : class;

        /// <summary>
        /// Deserializes data from a stream using a runtime type.
        /// Required for reflection-based loading of discovered settings types.
        /// </summary>
        object Deserialize(Stream stream, Type type);
    }
}
