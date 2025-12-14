using System;
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Majinfwork.Network {
    [RequireComponent(typeof(NetworkManager))]
    public class UNetcodeConnectionHandler : MonoBehaviour {
        public static event Action ConnectionEstablished;
        public static event Action ConnectionShutdown;
        public static event Action<IPEndPoint, DiscoveryResponseData> OnLocalSessionFound;

        [SerializeField] private bool lanSupport;

        internal NetworkManager networkManager;
        internal SessionState currentSessionState;

        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        private const int maxConnectPayload = 256;

        #region Session Holder
        public Dictionary<string, PlayerData> guidToClientData { get; private set; }
        public Dictionary<ulong, ClientRpcParams> targetRPC { get; private set; }

        private Dictionary<ulong, string> clientIDToGuid = new Dictionary<ulong, string>();
        private Dictionary<ulong, string> clientToScene = new Dictionary<ulong, string>();
        #endregion

        void Awake() {
            if (networkManager == null) {
                if (!TryGetComponent(out networkManager)) {
                    return;
                }
            }

            if (lanSupport) {
                var lanHandler = new ConnectionLANSupport(this);
                ServiceLocator.Register<ConnectionLANSupport>(lanHandler);
            }

            networkManager.OnServerStarted += OnServerStart;
            networkManager.OnClientConnectedCallback += ClientNetworkReadyWrapper;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.ConnectionApprovalCallback += ApprovalCheck;

            guidToClientData = new Dictionary<string, PlayerData>();
            targetRPC = new Dictionary<ulong, ClientRpcParams>();
            
        }

        void OnDestroy() {
            networkManager.OnServerStarted -= OnServerStart;
            networkManager.OnClientConnectedCallback -= ClientNetworkReadyWrapper;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        public void StartGameSesssionServer() {
            //port = NetworkUtility.GetAvailablePort();
            networkManager.StartServer();
        }

        public void StartGameSessionHost() {
            var payload = JsonUtility.ToJson(new ConnectionPayload() {
                clientGUID = Guid.NewGuid().ToString(),
                clientScene = SceneManager.GetActiveScene().name,
                playerName = "",
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            networkManager.NetworkConfig.ConnectionData = payloadBytes;
            networkManager.StartHost();
        }

        public void StartGameSesssionClient() {
            var payload = JsonUtility.ToJson(new ConnectionPayload() {
                clientGUID = Guid.NewGuid().ToString(),
                clientScene = SceneManager.GetActiveScene().name,
                playerName = "",
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            networkManager.NetworkConfig.ConnectionData = payloadBytes;
            networkManager.StartClient();
        }

        private void OnClientDisconnected(ulong clientId) {
            if (clientId == networkManager.LocalClientId) {
                Debug.Log("I'm disconnected");
                ConnectionShutdown?.Invoke();
            }
            else {
                Debug.Log($"Somone Disconnected {clientId}");
            }
        }

        private void OnServerStart() {
            ConnectionEstablished?.Invoke();
            currentSessionState = new SessionState("");
        }

        private void ClientNetworkReadyWrapper(ulong clientId) {
            if (networkManager.IsServer)
                return;

            if (clientId == networkManager.LocalClientId) {
                Debug.Log("I'm(client) success connect to Server");
                ConnectionEstablished?.Invoke();
            }
        }

        protected virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
            Debug.Log("Server/Host Checking Approval!");

            // The client identifier to be authenticated
            var clientId = request.ClientNetworkId;
            // Additional connection data defined by user code
            var connectionData = request.Payload;

            if (connectionData.Length > maxConnectPayload) {
                response.Approved = false;
                response.Reason = ConnectionMessage.ConnectionDataLong;
                return;
            }

            string payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

            string clientScene = connectionPayload.clientScene;
            string playerSessionID = connectionPayload.clientGUID;

            Debug.Log("Host ApprovalCheck: connecting client GUID: " + playerSessionID);

            response.Approved = true;
            response.Pending = false;

            //Spawning  Player Prefab
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = null;
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;

            clientToScene[clientId] = clientScene;
            clientIDToGuid[clientId] = connectionPayload.clientGUID;
            guidToClientData[connectionPayload.clientGUID] = new PlayerData(connectionPayload.playerName, clientId);

            ClientRpcParams clientRpcParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            targetRPC[clientId] = clientRpcParams;
        }
    }

    internal static class ConnectionMessage{
        internal const string ConnectionDataLong = "Connection Data Exceed Max Payload";
    }

    [Serializable]
    public class ConnectionPayload {
        public string clientGUID;
        public string clientScene = "";
        public string playerName;
    }

    public enum ConnectStatus {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        LoggedInAgain,            //logged in on a separate client, causing this one to be kicked out.
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user. 
        GenericDisconnect,        //server disconnected, but no specific reason given.
    }

    public struct PlayerData {
        public string PlayerName;  //name of the player
        public ulong ClientID; //the identifying id of the client

        public PlayerData(string playerName, ulong clientId) {
            PlayerName = playerName;
            ClientID = clientId;
        }
    }
}