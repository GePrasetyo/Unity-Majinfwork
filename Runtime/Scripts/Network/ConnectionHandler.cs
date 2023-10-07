using System;
using System.Net;
#if NETCODE
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif
using UnityEngine;

namespace Majingari.Network {
    [RequireComponent(typeof(NetworkManager))]
    public class ConnectionHandler : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData> {
        public static event Action ConnectionEstablished;
        public static event Action ConnectionShutdown;
        private NetworkManager networkManager;
        public static event Action<IPEndPoint, DiscoveryResponseData> OnServerFound;

        void Awake() {
            if (networkManager == null) {
                networkManager = GetComponent<NetworkManager>();
            }

            networkManager.OnServerStarted += OnServerStart;
            networkManager.OnClientConnectedCallback += ClientNetworkReadyWrapper;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        void OnDestroy() {
            networkManager.OnServerStarted -= OnServerStart;
            networkManager.OnClientConnectedCallback -= ClientNetworkReadyWrapper;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

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
            var sessionProp = ServiceLocator.Resolve<SessionState>();
            response = new DiscoveryResponseData() {
                serverName = sessionProp.sessionName,
                port = ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            };
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response) {
            OnServerFound?.Invoke(sender, response);
        }
    }
}