using System;
using System.Collections.Generic;

namespace Majinfwork.Network {
    /// <summary>
    /// Settings for creating a new session. Extend this struct or use CustomData for additional fields.
    /// </summary>
    [Serializable]
    public struct SessionSettings {
        public string sessionName;
        public int maxPlayers;
        public string password;
        public int mapIndex;

        /// <summary>
        /// Custom key-value data that can be extended by developers.
        /// This data is NOT automatically serialized over network - use for local configuration only.
        /// </summary>
        public Dictionary<string, object> CustomData;

        public bool HasPassword => !string.IsNullOrEmpty(password);

        public static SessionSettings Default => new SessionSettings {
            sessionName = "",
            maxPlayers = 4,
            password = null,
            mapIndex = 0,
            CustomData = null
        };

        public static SessionSettings Create(string name, int maxPlayers = 4, string password = null, int mapIndex = 0) {
            return new SessionSettings {
                sessionName = name,
                maxPlayers = maxPlayers,
                password = password,
                mapIndex = mapIndex,
                CustomData = null
            };
        }

        /// <summary>
        /// Gets a custom data value or default if not found.
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default) {
            if (CustomData == null || !CustomData.TryGetValue(key, out var value)) {
                return defaultValue;
            }
            return value is T typedValue ? typedValue : defaultValue;
        }

        /// <summary>
        /// Sets a custom data value.
        /// </summary>
        public void SetCustomData<T>(string key, T value) {
            CustomData ??= new Dictionary<string, object>();
            CustomData[key] = value;
        }
    }
}
