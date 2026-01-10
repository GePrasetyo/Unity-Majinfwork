using System;
using UnityEngine;

namespace Majinfwork.Network.Samples {
    /// <summary>
    /// Example: Custom connection data sent from client to server.
    /// Add any fields your game needs during connection.
    /// </summary>
    [Serializable]
    public class MyConnectionData {
        public int selectedCharacter;
        public string selectedSkin;
        public int teamIndex;
    }

    /// <summary>
    /// Example: Custom discovery data broadcast by the server.
    /// Add fields that help clients decide which session to join.
    /// </summary>
    [Serializable]
    public class MyDiscoveryData {
        public string gameMode;
        public string mapName;
        public bool isRanked;
    }

    /// <summary>
    /// Example: Custom SessionInfo with game-specific data.
    /// </summary>
    public class MySessionInfo : SessionInfo {
        public string GameMode { get; private set; }
        public string MapName { get; private set; }
        public bool IsRanked { get; private set; }

        public MySessionInfo(SessionSettings settings, int protocolVersion)
            : base(settings, protocolVersion) {
        }

        protected override void OnCreated(SessionSettings settings) {
            // Read custom data from settings
            GameMode = settings.GetCustomData("gameMode", "Deathmatch");
            MapName = settings.GetCustomData("mapName", "Default");
            IsRanked = settings.GetCustomData("isRanked", false);
        }
    }

    /// <summary>
    /// Example: Custom SessionManager that creates MySessionInfo.
    /// </summary>
    public class MySessionManager : SessionManager {
        public MySessionManager(NetworkConfig config) : base(config) { }

        protected override SessionInfo CreateSessionInfo(SessionSettings settings, int protocolVersion) {
            return new MySessionInfo(settings, protocolVersion);
        }

        protected override bool ValidateCustom(ConnectionPayload payload, out ConnectionStatus rejectionReason) {
            rejectionReason = ConnectionStatus.Success;

            // Example: Validate custom connection data
            var customData = payload.GetCustomData<MyConnectionData>();
            if (customData != null) {
                // Validate character selection
                if (customData.selectedCharacter < 0 || customData.selectedCharacter > 10) {
                    rejectionReason = ConnectionStatus.GenericFailure;
                    Debug.LogWarning("[MySessionManager] Invalid character selection");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Example: Custom LANDiscoveryService that adds game-specific discovery data.
    /// </summary>
    public class MyLANDiscoveryService : LANDiscoveryService {
        public MyLANDiscoveryService(NetworkConfig config, ISessionManager sessionManager, Func<ushort> getTransportPort)
            : base(config, sessionManager, getTransportPort) { }

        protected override DiscoveryResponseData CreateResponseData(SessionInfo session) {
            var response = base.CreateResponseData(session);

            // Add custom discovery data
            if (session is MySessionInfo mySession) {
                response.SetCustomData(new MyDiscoveryData {
                    gameMode = mySession.GameMode,
                    mapName = mySession.MapName,
                    isRanked = mySession.IsRanked
                });
            }

            return response;
        }

        protected override void OnSessionDiscoveredInternal(DiscoveredSession session, DiscoveryResponseData response) {
            // Parse and store custom data when session is discovered
            var customData = response.GetCustomData<MyDiscoveryData>();
            if (customData != null) {
                session.SetMetadata("gameMode", customData.gameMode);
                session.SetMetadata("mapName", customData.mapName);
                session.SetMetadata("isRanked", customData.isRanked);
            }
        }
    }

    /// <summary>
    /// Example: Custom connection handler with game-specific logic.
    /// Attach this to your NetworkManager GameObject instead of UNetcodeConnectionHandler.
    /// </summary>
    public class MyConnectionHandler : UNetcodeConnectionHandler {
        [Header("Game Settings")]
        [SerializeField] private int defaultCharacter = 0;
        [SerializeField] private string defaultSkin = "default";

        // Factory methods - return custom implementations
        protected override SessionManager CreateSessionManager(NetworkConfig config) {
            return new MySessionManager(config);
        }

        protected override LANDiscoveryService CreateLANDiscoveryService(NetworkConfig config, ISessionManager sessionManager) {
            return new MyLANDiscoveryService(config, sessionManager, GetTransportPort);
        }

        // Customize client connection payload
        protected override ConnectionPayload CreateClientPayload(string password) {
            var payload = base.CreateClientPayload(password);

            // Add custom data to the payload
            payload.SetCustomData(new MyConnectionData {
                selectedCharacter = defaultCharacter,
                selectedSkin = defaultSkin,
                teamIndex = 0
            });

            return payload;
        }

        // Handle client joining (server-side)
        protected override void OnClientJoined(ulong clientId) {
            base.OnClientJoined(clientId);

            // Get the client's custom data
            var payload = GetClientPayload(clientId);
            var customData = payload?.GetCustomData<MyConnectionData>();

            if (customData != null) {
                Debug.Log($"[MyConnectionHandler] Client {clientId} joined with character {customData.selectedCharacter}");
                // Spawn their selected character, assign team, etc.
            }
        }

        // Handle successful connection (client-side)
        protected override void OnLocalClientConnected() {
            base.OnLocalClientConnected();
            Debug.Log("[MyConnectionHandler] Successfully connected! Loading game...");
            // Transition to game scene, show lobby UI, etc.
        }

        // Handle disconnection (client-side)
        protected override void OnLocalClientDisconnected() {
            base.OnLocalClientDisconnected();
            Debug.Log("[MyConnectionHandler] Disconnected from server");
            // Return to main menu, show reconnect dialog, etc.
        }

        // Customize player spawning
        protected override void ConfigurePlayerSpawn(
            Unity.Netcode.NetworkManager.ConnectionApprovalRequest request,
            Unity.Netcode.NetworkManager.ConnectionApprovalResponse response,
            ConnectionPayload payload) {

            base.ConfigurePlayerSpawn(request, response, payload);

            // Example: Spawn at team-specific location
            var customData = payload.GetCustomData<MyConnectionData>();
            if (customData != null) {
                response.Position = GetTeamSpawnPoint(customData.teamIndex);
            }
        }

        private Vector3 GetTeamSpawnPoint(int teamIndex) {
            // Return spawn point based on team
            return teamIndex == 0 ? new Vector3(-5, 0, 0) : new Vector3(5, 0, 0);
        }
    }

    /// <summary>
    /// Example: UI controller for browsing and joining LAN sessions.
    /// Attach this to a UI GameObject in your lobby scene.
    /// </summary>
    public class LANBrowserUI : MonoBehaviour {
        private ILANDiscoveryService lanDiscovery;
        private INetworkService networkService;

        private void Start() {
            // Get services (after UNetcodeConnectionHandler.Initialize() is called)
            lanDiscovery = ServiceLocator.Resolve<ILANDiscoveryService>();
            networkService = ServiceLocator.Resolve<INetworkService>();

            if (lanDiscovery != null) {
                lanDiscovery.OnSessionDiscovered += OnSessionFound;
                lanDiscovery.OnSessionLost += OnSessionLost;
                lanDiscovery.OnSessionUpdated += OnSessionUpdated;
                lanDiscovery.OnScanComplete += OnScanFinished;
            }
        }

        private void OnDestroy() {
            if (lanDiscovery != null) {
                lanDiscovery.OnSessionDiscovered -= OnSessionFound;
                lanDiscovery.OnSessionLost -= OnSessionLost;
                lanDiscovery.OnSessionUpdated -= OnSessionUpdated;
                lanDiscovery.OnScanComplete -= OnScanFinished;
            }
        }

        // Call this from UI button
        public void RefreshSessionList() {
            Debug.Log("Scanning for LAN sessions...");
            lanDiscovery?.StartScan();
        }

        // Call this from UI button
        public void StopScanning() {
            lanDiscovery?.StopScan();
        }

        // Call this when user selects a session from the list
        public void JoinSelectedSession(DiscoveredSession session, string password = null) {
            if (session == null) {
                Debug.LogWarning("No session selected!");
                return;
            }

            // Check compatibility
            var config = Resources.Load<NetworkConfig>("Network Config");
            if (config != null && !session.IsCompatible(config.protocolVersion)) {
                Debug.LogWarning("Session version mismatch!");
                return;
            }

            // Check if session is full
            if (session.IsFull) {
                Debug.LogWarning("Session is full!");
                return;
            }

            // Check if password is required
            if (session.HasPassword && string.IsNullOrEmpty(password)) {
                Debug.LogWarning("Password required!");
                // Show password input dialog
                return;
            }

            Debug.Log($"Joining session: {session.SessionName}");
            networkService?.JoinSession(session, password);
        }

        // Call this from UI button to host a new session
        public void HostNewSession(string sessionName, int maxPlayers, string password = null) {
            var settings = new SessionSettings {
                sessionName = sessionName,
                maxPlayers = maxPlayers,
                password = password,
                mapIndex = 0
            };

            // Add custom data
            settings.SetCustomData("gameMode", "Deathmatch");
            settings.SetCustomData("mapName", "Arena");
            settings.SetCustomData("isRanked", false);

            networkService?.HostSession(settings);
        }

        private void OnSessionFound(DiscoveredSession session) {
            Debug.Log($"Found session: {session}");

            // Access custom discovery data
            var gameMode = session.GetMetadata<string>("gameMode");
            var mapName = session.GetMetadata<string>("mapName");
            var isRanked = session.GetMetadata<bool>("isRanked");

            Debug.Log($"  Mode: {gameMode}, Map: {mapName}, Ranked: {isRanked}");

            // Update your UI list here
            // sessionListUI.AddSession(session);
        }

        private void OnSessionLost(DiscoveredSession session) {
            Debug.Log($"Session no longer available: {session.SessionName}");
            // Remove from UI list
            // sessionListUI.RemoveSession(session);
        }

        private void OnSessionUpdated(DiscoveredSession session) {
            Debug.Log($"Session updated: {session}");
            // Update UI list item
            // sessionListUI.UpdateSession(session);
        }

        private void OnScanFinished() {
            Debug.Log($"Scan complete. Found {lanDiscovery?.DiscoveredSessions.Count ?? 0} sessions.");
            // Update UI state (hide loading indicator, etc.)
        }
    }

    /// <summary>
    /// Example: Bootstrap script to initialize the network system.
    /// Attach this to a persistent GameObject or call from your GameInstance.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour {
        [SerializeField] private NetworkConfig networkConfig;
        [SerializeField] private UNetcodeConnectionHandler connectionHandler;

        private void Start() {
            if (networkConfig == null) {
                networkConfig = Resources.Load<NetworkConfig>("Network Config");
            }

            if (connectionHandler == null) {
                connectionHandler = FindObjectOfType<UNetcodeConnectionHandler>();
            }

            if (connectionHandler != null && networkConfig != null) {
                connectionHandler.Initialize(networkConfig);
                Debug.Log("[NetworkBootstrap] Network system initialized");
            }
            else {
                Debug.LogError("[NetworkBootstrap] Missing NetworkConfig or ConnectionHandler!");
            }
        }

        private void OnApplicationQuit() {
            connectionHandler?.Shutdown();
        }
    }
}
