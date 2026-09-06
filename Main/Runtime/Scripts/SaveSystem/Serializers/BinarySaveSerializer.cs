using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Compact binary serializer: a short versioned header followed by GZip-compressed
    /// UTF-8 JSON produced by Unity's JsonUtility.
    ///
    /// This replaces the original BinaryFormatter implementation. BinaryFormatter is being
    /// removed from .NET (Unity 6.6 reports it as UAC0023) and is unsafe to deserialize
    /// untrusted data with, so nothing here ever writes with it again.
    ///
    /// Save files written by the old BinaryFormatter version still load: files without the
    /// magic header are handed to <see cref="LegacyBinarySaveReader"/> and rewritten in the
    /// current format on the next save.
    /// </summary>
    public class BinarySaveSerializer : ISaveSerializer {
        /// <summary>"MJSV" - identifies files written by this serializer.</summary>
        private static readonly byte[] Magic = { (byte)'M', (byte)'J', (byte)'S', (byte)'V' };

        private const byte CurrentVersion = 1;
        private const int HeaderLength = 5; // Magic + version byte

        public string FileExtension => ".dat";

        public void Serialize<T>(Stream stream, T data) where T : class {
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, false));

            stream.Write(Magic, 0, Magic.Length);
            stream.WriteByte(CurrentVersion);

            using (var gzip = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true)) {
                gzip.Write(bytes, 0, bytes.Length);
            }
        }

        public T Deserialize<T>(Stream stream) where T : class {
            return (T)Deserialize(stream, typeof(T));
        }

        public object Deserialize(Stream stream, Type type) {
            var header = new byte[HeaderLength];
            var headerBytes = ReadExactly(stream, header);
            if (headerBytes != HeaderLength) {
                throw new InvalidDataException(
                    "[BinarySaveSerializer] Save file is empty or truncated.");
            }

            if (!HasMagic(header)) {
                // Written by the pre-Unity 6.6 BinaryFormatter version. Replay the whole
                // stream through the legacy reader; the next save rewrites it in this format.
                return LegacyBinarySaveReader.Read(Replay(stream, header, headerBytes));
            }

            var version = header[Magic.Length];
            if (version != CurrentVersion) {
                throw new InvalidDataException(
                    $"[BinarySaveSerializer] Unsupported save format version {version}, expected {CurrentVersion}.");
            }

            using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzip, Encoding.UTF8)) {
                return JsonUtility.FromJson(reader.ReadToEnd(), type);
            }
        }

        private static bool HasMagic(byte[] header) {
            for (int i = 0; i < Magic.Length; i++) {
                if (header[i] != Magic[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a stream positioned back at byte 0, stitching the already-consumed header
        /// back on for streams that cannot seek.
        /// </summary>
        private static Stream Replay(Stream stream, byte[] header, int headerBytes) {
            if (stream.CanSeek) {
                stream.Position = 0;
                return stream;
            }

            var buffered = new MemoryStream();
            buffered.Write(header, 0, headerBytes);
            stream.CopyTo(buffered);
            buffered.Position = 0;
            return buffered;
        }

        /// <summary>
        /// Fills <paramref name="buffer"/>, tolerating short reads. Returns the byte count read.
        /// </summary>
        private static int ReadExactly(Stream stream, byte[] buffer) {
            int total = 0;
            while (total < buffer.Length) {
                int read = stream.Read(buffer, total, buffer.Length - total);
                if (read <= 0) break;
                total += read;
            }
            return total;
        }
    }
}
