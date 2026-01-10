using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Majinfwork.Network {
    /// <summary>
    /// Base network connection handler using Unity Netcode for GameObjects.
    /// Extend this class to customize network behavior for your game.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class UNetcodeConnectionHandler : MonoBehaviour, INetworkService {
        [SerializeField] protected NetworkConfig networkConfig;
        [SerializeField] protected bool lanSupport;
        [SerializeField] protected string defaultPlayerName = "Player";

        protected NetworkManager networkManager;
        protected SessionManager sessionManager;
        protected LANDiscoveryService lanDiscovery;
        protected ConnectionApprovalValidator approvalValidator;
        protected bool isInitialized;

        #region INetworkService Implementation
        public SessionInfo CurrentSession => sessionManager?.CurrentSession;
        public ConnectionStatus Status { get; protected set; } = ConnectionStatus.Disconnected;
        public bool IsConnected => Status.IsConnected();
        public bool IsHost => networkManager != null && networkManager.IsHost;

        public event Action<ConnectionStatus> OnStatusChanged;
        public event Action<ulong> OnClientConnected;
        public event Action<ulong, ConnectionStatus> OnClientDisconnected;
        #endregion

        #region Client Data Tracking
        public Dictionary<string, PlayerData> GuidToClientData { get; protected set; }
        public Dictionary<ulong, ClientRpcParams> TargetRPC { get; protected set; }

        protected Dictionary<ulong, string> clientIDToGuid;
        protected Dictionary<ulong, string> clientToScene;
        protected Dictionary<ulong, ConnectionPayload> clientPayloads;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake() {
            if (!TryGetComponent(out networkManager)) {
                Debug.LogError("[UNetcodeConnectionHandler] NetworkManager not found!");
                return;
            }

            InitializeDataStructures();
            SubscribeToNetworkEvents();
        }

        protected virtual void OnDestroy() {
            UnsubscribeFromNetworkEvents();

            if (isInitialized) {
                Shutdown();
            }
        }
        #endregion

        #region Initialization
        public virtual void Initialize(NetworkConfig config) {
            if (isInitialized) {
                Debug.LogWarning("[UNetcodeConnectionHandler] Already initialized.");
                return;
            }

            networkConfig = config;
            MainThreadDispatcher.Initialize();

            sessionManager = CreateSessionManager(config);
            approvalValidator = CreateApprovalValidator(config, sessionManager);

            if (lanSupport) {
                lanDiscovery = CreateLANDiscoveryService(config, sessionManager);
                ServiceLocator.Register<ILANDiscoveryService>(lanDiscovery);
            }

            ServiceLocator.Register<INetworkService>(this);
            ServiceLocator.Register<ISessionManager>(sessionManager);

            OnInitialized();

            isInitialized = true;
            Debug.Log("[UNetcodeConnectionHandler] Initialized");
        }

        /// <summary>
        /// Factory method for creating SessionManager. Override to use custom SessionManager.
        /// </summary>
        protected virtual SessionManager CreateSessionManager(NetworkConfig config) {
            return new SessionManager(config);
        }

        /// <summary>
        /// Factory method for creating ConnectionApprovalValidator. Override for custom validation.
        /// </summary>
        protected virtual ConnectionApprovalValidator CreateApprovalValidator(NetworkConfig config, ISessionManager sessionManager) {
            return new ConnectionApprovalValidator(config, sessionManager);
        }

        /// <summary>
        /// Factory method for creating LANDiscoveryService. Override for custom discovery.
        /// </summary>
        protected virtual LANDiscoveryService CreateLANDiscoveryService(NetworkConfig config, ISessionManager sessionManager) {
            return new LANDiscoveryService(config, sessionManager, GetTransportPort);
        }

        /// <summary>
        /// Called after initialization is complete. Override for custom setup.
        /// </summary>
        protected virtual void OnInitialized() { }

        public virtual void Shutdown() {
            LeaveSession();

            lanDiscovery?.StopScan();
            MainThreadDispatcher.Shutdown();

            ServiceLocator.Unregister<INetworkService>(out _);
            ServiceLocator.Unregister<ISessionManager>(out _);

            if (lanSupport && lanDiscovery != null) {
                ServiceLocator.Unregister<ILANDiscoveryService>(out _);
            }

            OnShutdown();

            isInitialized = false;
            Debug.Log("[UNetcodeConnectionHandler] Shutdown complete");
        }

        /// <summary>
        /// Called during shutdown. Override for custom cleanup.
        /// </summary>
        protected virtual void OnShutdown() { }
        #endregion

        #region Public API
        public virtual void HostSession(SessionSettings settings) {
            if (!isInitialized) {
                Debug.LogError("[UNetcodeConnectionHandler] Not initialized. Call Initialize() first.");
                return;
            }

            if (networkManager.IsListening) {
                Debug.LogWarning("[UNetcodeConnectionHandler] Already connected. Call LeaveSession() first.");
                return;
            }

            sessionManager.CreateSession(settings, networkConfig.protocolVersion);

            var payload = CreateHostPayload(settings);
            networkManager.NetworkConfig.ConnectionData = payload.ToBytes();

            SetStatus(ConnectionStatus.Connecting);
            networkManager.StartHost();
        }

        /// <summary>
        /// Creates the connection payload for the host. Override to add custom data.
        /// </summary>
        protected virtual ConnectionPayload CreateHostPayload(SessionSettings settings) {
            return ConnectionPayload.Create(
                settings.sessionName,
                networkConfig.protocolVersion,
                settings.password
            );
        }

        public virtual void JoinSession(string address, ushort port, string password = null) {
            if (!isInitialized) {
                Debug.LogError("[UNetcodeConnectionHandler] Not initialized. Call Initialize() first.");
                return;
            }

            if (networkManager.IsListening) {
                Debug.LogWarning("[UNetcodeConnectionHandler] Already connected. Call LeaveSession() first.");
                return;
            }

            ConfigureTransport(address, port);

            var payload = CreateClientPayload(password);
            networkManager.NetworkConfig.ConnectionData = payload.ToBytes();

            SetStatus(ConnectionStatus.Connecting);
            networkManager.StartClient();

            Debug.Log($"[UNetcodeConnectionHandler] Connecting to {address}:{port}");
        }

        /// <summary>
        /// Configures the transport layer for connection. Override for custom transport setup.
        /// </summary>
        protected virtual void ConfigureTransport(string address, ushort port) {
            if (networkManager.NetworkConfig.NetworkTransport is UnityTransport transport) {
                transport.ConnectionData.Address = address;
                transport.ConnectionData.Port = port;
            }
        }

        /// <summary>
        /// Creates the connection payload for clients. Override to add custom data.
        /// </summary>
        protected virtual ConnectionPayload CreateClientPayload(string password) {
            var playerName = GetPlayerName();
            return ConnectionPayload.Create(playerName, networkConfig.protocolVersion, password);
        }

        /// <summary>
        /// Gets the player name for connection. Override to customize player name retrieval.
        /// </summary>
        protected virtual string GetPlayerName() {
            return PlayerPrefs.GetString("PlayerName", defaultPlayerName);
        }

        public virtual void JoinSession(DiscoveredSession session, string password = null) {
            if (session == null) {
                Debug.LogError("[UNetcodeConnectionHandler] Session is null.");
                return;
            }

            JoinSession(session.Address, session.Port, password);
        }

        public virtual void LeaveSession() {
            if (!networkManager.IsListening) return;

            OnLeavingSession();

            if (networkManager.IsServer) {
                sessionManager?.DestroySession();
                lanDiscovery?.StopDiscovery();
            }

            networkManager.Shutdown();
            ClearClientData();
            SetStatus(ConnectionStatus.Disconnected);

            Debug.Log("[UNetcodeConnectionHandler] Left session");
        }

        /// <summary>
        /// Called before leaving a session. Override for custom cleanup.
        /// </summary>
        protected virtual void OnLeavingSession() { }
        #endregion

        #region Network Event Handlers
        protected virtual void SubscribeToNetworkEvents() {
            if (networkManager == null) return;

            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientConnectedCallback += OnClientConnectedInternal;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectedInternal;
            networkManager.ConnectionApprovalCallback += ApprovalCheck;
        }

        protected virtual void UnsubscribeFromNetworkEvents() {
            if (networkManager == null) return;

            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.OnClientConnectedCallback -= OnClientConnectedInternal;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectedInternal;
            networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        }

        protected virtual void OnServerStarted() {
            SetStatus(ConnectionStatus.Hosting);

            if (lanSupport && lanDiscovery != null) {
                lanDiscovery.StartHosting();
            }

            OnServerStartedCustom();

            Debug.Log("[UNetcodeConnectionHandler] Server started");
        }

        /// <summary>
        /// Called after server starts. Override for custom server setup.
        /// </summary>
        protected virtual void OnServerStartedCustom() { }

        protected virtual void OnClientConnectedInternal(ulong clientId) {
            if (networkManager.IsServer) {
                sessionManager?.IncrementPlayerCount();
                OnClientConnected?.Invoke(clientId);
                OnClientJoined(clientId);
                Debug.Log($"[UNetcodeConnectionHandler] Client {clientId} connected");
            }
            else if (clientId == networkManager.LocalClientId) {
                SetStatus(ConnectionStatus.Connected);
                OnLocalClientConnected();
                Debug.Log("[UNetcodeConnectionHandler] Successfully connected to server");
            }
        }

        /// <summary>
        /// Called on server when a client joins. Override for custom handling.
        /// </summary>
        protected virtual void OnClientJoined(ulong clientId) { }

        /// <summary>
        /// Called on client when local connection succeeds. Override for custom handling.
        /// </summary>
        protected virtual void OnLocalClientConnected() { }

        protected virtual void OnClientDisconnectedInternal(ulong clientId) {
            if (networkManager.IsServer) {
                // Remove client data
                if (clientIDToGuid.TryGetValue(clientId, out var guid)) {
                    GuidToClientData.Remove(guid);
                    clientIDToGuid.Remove(clientId);
                }
                clientToScene.Remove(clientId);
                clientPayloads.Remove(clientId);
                TargetRPC.Remove(clientId);

                sessionManager?.DecrementPlayerCount();
                OnClientLeft(clientId);
                OnClientDisconnected?.Invoke(clientId, ConnectionStatus.GenericFailure);
                Debug.Log($"[UNetcodeConnectionHandler] Client {clientId} disconnected");
            }
            else if (clientId == networkManager.LocalClientId) {
                SetStatus(ConnectionStatus.Disconnected);
                OnLocalClientDisconnected();
                OnClientDisconnected?.Invoke(clientId, ConnectionStatus.GenericFailure);
                Debug.Log("[UNetcodeConnectionHandler] Disconnected from server");
            }
        }

        /// <summary>
        /// Called on server when a client leaves. Override for custom handling.
        /// </summary>
        protected virtual void OnClientLeft(ulong clientId) { }

        /// <summary>
        /// Called on client when disconnected from server. Override for custom handling.
        /// </summary>
        protected virtual void OnLocalClientDisconnected() { }

        protected virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
            Debug.Log("[UNetcodeConnectionHandler] Processing connection approval...");

            var (approved, status, payload) = approvalValidator.Validate(
                request.Payload,
                networkManager.ConnectedClientsIds.Count
            );

            response.Approved = approved;
            response.Pending = false;

            if (!approved) {
                response.Reason = status.ToMessage();
                OnConnectionRejected(request.ClientNetworkId, status, payload);
                Debug.LogWarning($"[UNetcodeConnectionHandler] Connection rejected: {status.ToMessage()}");
                return;
            }

            // Configure spawning
            ConfigurePlayerSpawn(request, response, payload);

            // Track client data
            var clientId = request.ClientNetworkId;
            TrackClientData(clientId, payload);

            OnConnectionApproved(clientId, payload);
            Debug.Log($"[UNetcodeConnectionHandler] Connection approved: {payload.playerName}");
        }

        /// <summary>
        /// Configures player spawn settings. Override to customize spawn behavior.
        /// </summary>
        protected virtual void ConfigurePlayerSpawn(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response,
            ConnectionPayload payload) {

            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = null;
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;
        }

        /// <summary>
        /// Called when a connection is approved. Override for custom post-approval handling.
        /// </summary>
        protected virtual void OnConnectionApproved(ulong clientId, ConnectionPayload payload) { }

        /// <summary>
        /// Called when a connection is rejected. Override for custom rejection handling.
        /// </summary>
        protected virtual void OnConnectionRejected(ulong clientId, ConnectionStatus reason, ConnectionPayload payload) { }
        #endregion

        #region Helper Methods
        protected virtual void InitializeDataStructures() {
            GuidToClientData = new Dictionary<string, PlayerData>();
            TargetRPC = new Dictionary<ulong, ClientRpcParams>();
            clientIDToGuid = new Dictionary<ulong, string>();
            clientToScene = new Dictionary<ulong, string>();
            clientPayloads = new Dictionary<ulong, ConnectionPayload>();
        }

        protected virtual void ClearClientData() {
            GuidToClientData.Clear();
            TargetRPC.Clear();
            clientIDToGuid.Clear();
            clientToScene.Clear();
            clientPayloads.Clear();
        }

        protected virtual void TrackClientData(ulong clientId, ConnectionPayload payload) {
            clientToScene[clientId] = payload.clientScene;
            clientIDToGuid[clientId] = payload.clientGUID;
            clientPayloads[clientId] = payload;
            GuidToClientData[payload.clientGUID] = new PlayerData(payload.playerName, clientId);

            TargetRPC[clientId] = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
        }

        /// <summary>
        /// Gets the payload for a connected client. Returns null if not found.
        /// </summary>
        public ConnectionPayload GetClientPayload(ulong clientId) {
            return clientPayloads.TryGetValue(clientId, out var payload) ? payload : null;
        }

        protected void SetStatus(ConnectionStatus newStatus) {
            if (Status == newStatus) return;

            Status = newStatus;
            OnStatusChanged?.Invoke(newStatus);
        }

        protected virtual ushort GetTransportPort() {
            if (networkManager?.NetworkConfig?.NetworkTransport is UnityTransport transport) {
                return transport.ConnectionData.Port;
            }
            return 7777;
        }
        #endregion
    }

    /// <summary>
    /// Data about a connected player.
    /// </summary>
    public struct PlayerData {
        public string PlayerName;
        public ulong ClientID;

        public PlayerData(string playerName, ulong clientId) {
            PlayerName = playerName;
            ClientID = clientId;
        }
    }
}
