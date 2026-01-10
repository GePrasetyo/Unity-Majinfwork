using System;
using System.Collections.Generic;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Serializable key-value pair for metadata storage.
    /// Used instead of Dictionary for Unity serialization compatibility.
    /// </summary>
    [Serializable]
    public struct MetadataEntry {
        public string key;
        public string value;

        public MetadataEntry(string key, string value) {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>
    /// Represents a save slot with metadata.
    /// Extend to add game-specific slot information.
    /// </summary>
    [Serializable]
    public class SaveSlot {
        /// <summary>
        /// The slot index.
        /// </summary>
        public int index;

        /// <summary>
        /// Whether this slot has valid save data.
        /// </summary>
        public bool isValid;

        /// <summary>
        /// Display name for this save (e.g., player name, save name).
        /// </summary>
        public string displayName;

        /// <summary>
        /// When the save was last modified.
        /// </summary>
        public DateTime lastSaveTime;

        /// <summary>
        /// Total playtime in seconds.
        /// </summary>
        public double totalPlayTimeSeconds;

        /// <summary>
        /// Custom metadata list for game-specific data.
        /// Uses List instead of Dictionary for serialization compatibility.
        /// </summary>
        public List<MetadataEntry> metadata = new List<MetadataEntry>();

        // Runtime cache for fast lookups (not serialized)
        [NonSerialized]
        private Dictionary<string, int> metadataIndexCache;

        /// <summary>
        /// Creates a new empty save slot.
        /// </summary>
        public SaveSlot() { }

        /// <summary>
        /// Creates a new save slot with the specified index.
        /// </summary>
        public SaveSlot(int slotIndex) {
            index = slotIndex;
            isValid = false;
            displayName = $"Slot {slotIndex + 1}";
            lastSaveTime = default;
            totalPlayTimeSeconds = 0;
        }

        /// <summary>
        /// Gets a metadata value or default if not found.
        /// </summary>
        public string GetMetadata(string key, string defaultValue = null) {
            EnsureMetadataCache();
            if (metadataIndexCache.TryGetValue(key, out var index)) {
                return metadata[index].value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Sets a metadata value.
        /// </summary>
        public void SetMetadata(string key, string value) {
            EnsureMetadataCache();
            if (metadataIndexCache.TryGetValue(key, out var index)) {
                metadata[index] = new MetadataEntry(key, value);
            }
            else {
                metadataIndexCache[key] = metadata.Count;
                metadata.Add(new MetadataEntry(key, value));
            }
        }

        /// <summary>
        /// Checks if metadata contains the specified key.
        /// </summary>
        public bool HasMetadata(string key) {
            EnsureMetadataCache();
            return metadataIndexCache.ContainsKey(key);
        }

        /// <summary>
        /// Clears all metadata.
        /// </summary>
        public void ClearMetadata() {
            metadata.Clear();
            metadataIndexCache?.Clear();
        }

        private void EnsureMetadataCache() {
            if (metadataIndexCache == null) {
                metadataIndexCache = new Dictionary<string, int>(metadata.Count);
                for (int i = 0; i < metadata.Count; i++) {
                    metadataIndexCache[metadata[i].key] = i;
                }
            }
        }

        /// <summary>
        /// Resets this slot to empty state.
        /// </summary>
        public virtual void Reset() {
            isValid = false;
            displayName = $"Slot {index + 1}";
            lastSaveTime = default;
            totalPlayTimeSeconds = 0;
            ClearMetadata();
        }

        /// <summary>
        /// Formatted play time string (HH:MM:SS).
        /// </summary>
        public string FormattedPlayTime {
            get {
                var span = TimeSpan.FromSeconds(totalPlayTimeSeconds);
                return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
            }
        }

        /// <summary>
        /// Formatted last save time string.
        /// </summary>
        public string FormattedLastSaveTime {
            get {
                if (lastSaveTime == default) return "Never";
                return lastSaveTime.ToString("yyyy-MM-dd HH:mm");
            }
        }
    }
}
