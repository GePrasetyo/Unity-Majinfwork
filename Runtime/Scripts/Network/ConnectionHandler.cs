using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Majingari.Network {
    [RequireComponent(typeof(NetworkManager))]
    public class ConnectionHandler : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData> {
        public static event Action ConnectionEstablished;
        public static event Action ConnectionShutdown;
        private NetworkManager networkManager;
        public static event Action<IPEndPoint, DiscoveryResponseData> OnServerFound;
        
        private SessionState currentSessionState;

        void Awake() {
            if (networkManager == null) {
                networkManager = GetComponent<NetworkManager>();
            }

            networkManager.OnServerStarted += OnServerStart;
            networkManager.OnClientConnectedCallback += ClientNetworkReadyWrapper;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;

            port = NetworkUtility.GetAvailablePort();
            ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port = port;
        }

        void OnDestroy() {
            networkManager.OnServerStarted -= OnServerStart;
            networkManager.OnClientConnectedCallback -= ClientNetworkReadyWrapper;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        #region Host & Join
        public void StartGameSesssionServer() {
            networkManager.StartServer();
        }

        public void StartGameSesssionHost() {
            networkManager.StartHost();
        }

        public void StartGameSesssionClient() {
            networkManager.StartClient();
        }

        public void AutoJoinLocalSession() {
            SearchLocalSession();
            ClientBroadcast(new DiscoveryBroadcastData());
        }
        #endregion

        private void OnClientDisconnected(ulong clientId) {
            if (clientId == networkManager.LocalClientId) {
                Debug.Log("I'm disconnected");
                ConnectionShutdown?.Invoke();
                StopDiscovery();
            }
            else {
                Debug.Log($"Somone Disconnected {clientId}");
            }
        }

        private void OnServerStart() {
            ConnectionEstablished?.Invoke();
            StartServer();
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

        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response) {
            Debug.Log($"Broadcast my Session {sender.Address} -- {sender.Port}");
            
            response = new DiscoveryResponseData() {
                serverName = currentSessionState.sessionName,
                port = ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            };
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response) {
            OnServerFound?.Invoke(sender, response);
            ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Address = sender.Address.ToString();
            ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port = (ushort)sender.Port;
            StartGameSesssionClient();
        }
    }
}