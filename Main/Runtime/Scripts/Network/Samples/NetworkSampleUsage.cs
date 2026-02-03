using System;
using System.Threading;
using System.Threading.Tasks;
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
    /// Example: Session browser UI using async enumerable pattern.
    /// Attach this to a UI GameObject in your lobby scene.
    /// </summary>
    public class SessionBrowserUI : MonoBehaviour {
        [SerializeField] private float scanDuration = 10f;

        private ILANDiscoveryService lanDiscovery;
        private INetworkService networkService;
        private CancellationTokenSource scanCts;

        private void Start() {
            // Get services (after UNetcodeConnectionHandler.Initialize() is called)
            lanDiscovery = ServiceLocator.Resolve<ILANDiscoveryService>();
            networkService = ServiceLocator.Resolve<INetworkService>();
        }

        private void OnDestroy() {
            StopScanning();
        }

        /// <summary>
        /// Start scanning for sessions. Call from UI button.
        /// </summary>
        public async void StartScanning() {
            if (lanDiscovery == null) {
                Debug.LogWarning("LAN Discovery not available!");
                return;
            }

            StopScanning();
            scanCts = new CancellationTokenSource();

            Debug.Log("Starting session scan...");

            try {
                await foreach (var evt in lanDiscovery.ScanAsync(
                    timeout: TimeSpan.FromSeconds(scanDuration),
                    cancellationToken: scanCts.Token)) {

                    switch (evt.Type) {
                        case DiscoveryEventType.ScanStarted:
                            Debug.Log("Scan started");
                            // Show loading indicator, clear old UI list
                            break;

                        case DiscoveryEventType.Discovered:
                            Debug.Log($"Found: {evt.Session}");
                            // Add session to UI list
                            // Access custom metadata if using custom LANDiscoveryService:
                            var gameMode = evt.Session.GetMetadata<string>("gameMode", "Unknown");
                            Debug.Log($"  Game Mode: {gameMode}");
                            break;

                        case DiscoveryEventType.Updated:
                            Debug.Log($"Updated: {evt.Session}");
                            // Update session in UI list (player count changed, etc.)
                            break;

                        case DiscoveryEventType.Lost:
                            Debug.Log($"Lost: {evt.Session.SessionName}");
                            // Remove session from UI list
                            break;

                        case DiscoveryEventType.ScanComplete:
                            Debug.Log($"Scan complete. Found {lanDiscovery.DiscoveredSessions.Count} session(s)");
                            // Hide loading indicator, enable refresh button
                            break;
                    }
                }
            }
            catch (OperationCanceledException) {
                Debug.Log("Scan cancelled");
            }
        }

        /// <summary>
        /// Stop the current scan. Call from UI button or OnDestroy.
        /// </summary>
        public void StopScanning() {
            scanCts?.Cancel();
            scanCts?.Dispose();
            scanCts = null;
        }

        /// <summary>
        /// Join a selected session. Call when user clicks on a session in the list.
        /// </summary>
        public async void JoinSession(DiscoveredSession session, string password = null) {
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

            if (session.IsFull) {
                Debug.LogWarning("Session is full!");
                return;
            }

            if (session.HasPassword && string.IsNullOrEmpty(password)) {
                Debug.LogWarning("Password required!");
                // Show password input dialog
                return;
            }

            StopScanning();

            // Show "Connecting..." UI
            Debug.Log($"Joining {session.SessionName}...");

            var status = await networkService.JoinSessionAsync(session, password);

            switch (status) {
                case ConnectionStatus.Connected:
                    Debug.Log("Successfully joined!");
                    // Load game scene
                    break;
                case ConnectionStatus.ServerFull:
                    Debug.LogWarning("Server became full!");
                    break;
                case ConnectionStatus.IncorrectPassword:
                    Debug.LogWarning("Incorrect password!");
                    break;
                case ConnectionStatus.Timeout:
                    Debug.LogWarning("Connection timed out!");
                    break;
                default:
                    Debug.LogWarning($"Failed to join: {status.ToMessage()}");
                    break;
            }
        }

        /// <summary>
        /// Host a new session. Call from UI button.
        /// </summary>
        public async void HostSession(string sessionName, int maxPlayers, string password = null) {
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

            StopScanning();

            // Show "Starting server..." UI
            Debug.Log("Starting server...");

            var status = await networkService.HostSessionAsync(settings);

            if (status == ConnectionStatus.Hosting) {
                Debug.Log($"Hosting: {networkService.CurrentSession.SessionName}");
                // Load game scene or show lobby
            } else {
                Debug.LogError($"Failed to host: {status.ToMessage()}");
            }
        }
    }

    /// <summary>
    /// Example: Quick Join / Matchmaking using FindSessionAsync.
    /// </summary>
    public class QuickJoinUI : MonoBehaviour {
        [SerializeField] private float searchTimeout = 15f;

        private ILANDiscoveryService lanDiscovery;
        private INetworkService networkService;
        private CancellationTokenSource searchCts;

        private void Start() {
            lanDiscovery = ServiceLocator.Resolve<ILANDiscoveryService>();
            networkService = ServiceLocator.Resolve<INetworkService>();
        }

        private void OnDestroy() {
            CancelSearch();
        }

        /// <summary>
        /// Quick join - find and join the first available session.
        /// </summary>
        public async void QuickJoin() {
            if (lanDiscovery == null || networkService == null) {
                Debug.LogWarning("Network services not available!");
                return;
            }

            CancelSearch();
            searchCts = new CancellationTokenSource();

            Debug.Log("Searching for available session...");
            // Show "Searching..." UI

            var config = Resources.Load<NetworkConfig>("Network Config");
            var protocolVersion = config?.protocolVersion ?? 1;

            try {
                var session = await lanDiscovery.FindSessionAsync(
                    predicate: s => !s.IsFull && !s.HasPassword && s.IsCompatible(protocolVersion),
                    timeout: TimeSpan.FromSeconds(searchTimeout),
                    cancellationToken: searchCts.Token
                );

                if (session != null) {
                    Debug.Log($"Found session: {session.SessionName}. Joining...");

                    var status = await networkService.JoinSessionAsync(session, cancellationToken: searchCts.Token);

                    if (status == ConnectionStatus.Connected) {
                        Debug.Log("Successfully joined!");
                        // Load game scene
                    } else {
                        Debug.LogWarning($"Failed to join: {status.ToMessage()}");
                        // Show error and offer to try again or host
                    }
                }
                else {
                    Debug.Log("No available session found. Consider hosting?");
                    // Show "No sessions found" dialog with option to host
                }
            }
            catch (OperationCanceledException) {
                Debug.Log("Search cancelled");
            }
        }

        /// <summary>
        /// Cancel the current search.
        /// </summary>
        public void CancelSearch() {
            searchCts?.Cancel();
            searchCts?.Dispose();
            searchCts = null;
        }
    }

    /// <summary>
    /// Example: Continuous background scanning (e.g., for a lobby that auto-updates).
    /// </summary>
    public class ContinuousScannerUI : MonoBehaviour {
        private ILANDiscoveryService lanDiscovery;
        private CancellationTokenSource scanCts;

        private void Start() {
            lanDiscovery = ServiceLocator.Resolve<ILANDiscoveryService>();
        }

        private void OnDestroy() {
            StopScanning();
        }

        /// <summary>
        /// Start continuous scanning (no timeout - runs until stopped).
        /// </summary>
        public async void StartContinuousScanning() {
            if (lanDiscovery == null) return;

            StopScanning();
            scanCts = new CancellationTokenSource();

            try {
                // timeout: null means scan indefinitely
                await foreach (var evt in lanDiscovery.ScanAsync(timeout: null, cancellationToken: scanCts.Token)) {
                    switch (evt.Type) {
                        case DiscoveryEventType.Discovered:
                        case DiscoveryEventType.Updated:
                        case DiscoveryEventType.Lost:
                            // Refresh UI with current session list
                            RefreshUI(lanDiscovery.DiscoveredSessions);
                            break;
                    }
                }
            }
            catch (OperationCanceledException) {
                // Expected when stopping
            }
        }

        public void StopScanning() {
            scanCts?.Cancel();
            scanCts?.Dispose();
            scanCts = null;
        }

        private void RefreshUI(System.Collections.Generic.IReadOnlyList<DiscoveredSession> sessions) {
            // Update your UI list here
            Debug.Log($"Sessions: {sessions.Count}");
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
