using System.IO;
#if !MAJINFWORK_NO_LEGACY_BINARY_SAVES
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Reads save files written by the pre-Unity 6.6 BinaryFormatter version of
    /// <see cref="BinarySaveSerializer"/>, so save files already on players' disks keep
    /// loading after the format change.
    ///
    /// Migration is transparent and needs no action from the game: a legacy file is read
    /// here, and the next save rewrites it in the current GZip+JSON format.
    ///
    /// BinaryFormatter is deprecated (UAC0023 / SYSLIB0011) and .NET will eventually remove
    /// it, so every remaining use is confined to this one file. Once your players have
    /// rolled over to the new format, define MAJINFWORK_NO_LEGACY_BINARY_SAVES to compile
    /// this path out entirely and drop the dependency for good.
    /// </summary>
    internal static class LegacyBinarySaveReader {
#if MAJINFWORK_NO_LEGACY_BINARY_SAVES
        /// <summary>Whether legacy saves can still be read in this build.</summary>
        public const bool IsAvailable = false;

        public static object Read(Stream stream) {
            throw new InvalidDataException(
                "[BinarySaveSerializer] This file is in the pre-Unity 6.6 BinaryFormatter save " +
                "format, and legacy save support was compiled out via MAJINFWORK_NO_LEGACY_BINARY_SAVES.");
        }
#else
        /// <summary>Whether legacy saves can still be read in this build.</summary>
        public const bool IsAvailable = true;

        /// <summary>
        /// Deserializes a legacy BinaryFormatter payload. The current save types deliberately
        /// keep the same field names and types the old format recorded, so they populate
        /// directly - no serialization binder or surrogate is required.
        /// </summary>
        public static object Read(Stream stream) {
            // The only way to read the old format is the API that wrote it. Confined to this
            // method; nothing in the framework ever writes with BinaryFormatter again.
#pragma warning disable UAC0023, SYSLIB0011
            var formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
#pragma warning restore UAC0023, SYSLIB0011
        }
#endif
    }
}
