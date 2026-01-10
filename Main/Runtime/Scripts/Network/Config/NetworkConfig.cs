using UnityEngine;

namespace Majinfwork.Network {
    [CreateAssetMenu(fileName = "Network Config", menuName = "MFramework/Config Object/Network Config")]
    public class NetworkConfig : ScriptableObject {
        [Header("Protocol")]
        [Tooltip("Version number for protocol compatibility checking")]
        public int protocolVersion = 1;

        [Header("Discovery")]
        [Tooltip("UDP port for LAN discovery broadcasts")]
        public ushort discoveryPort = 47777;

        [Tooltip("How long to scan for sessions (seconds)")]
        public float discoveryTimeout = 5f;

        [Tooltip("How often to send broadcast during scan (seconds)")]
        public float broadcastInterval = 1f;

        [Tooltip("Remove sessions not seen for this duration (seconds)")]
        public float sessionStaleTimeout = 3f;

        [Header("Connection")]
        [Tooltip("Maximum size of connection payload in bytes")]
        public int maxConnectPayload = 256;

        [Tooltip("Connection attempt timeout (seconds)")]
        public float connectionTimeout = 10f;

        [Header("Session Defaults")]
        [Tooltip("Default maximum players per session")]
        public int defaultMaxPlayers = 4;

        [Tooltip("Prefix for auto-generated session names")]
        public string defaultSessionNamePrefix = "Session";
    }
}
