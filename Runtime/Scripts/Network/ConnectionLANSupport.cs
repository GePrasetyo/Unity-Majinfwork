using System.Net;
using System;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;

namespace Majingari.Network {
    public class ConnectionLANSupport : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData> {
        public static event Action<IPEndPoint, DiscoveryResponseData> OnLocalSessionFound;
        private NetworkManager networkManager;
        private SessionState currentSession;

        public ConnectionLANSupport(NetworkManager netManager) {
            this.networkManager = netManager;

            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public void StartLocalSesssionHost() {
            networkManager.OnServerStarted += OnLocalSessionStarted;
            networkManager.StartHost();
        }

        public void AutoJoinLocalSession() {
            SearchLocalSession();
            ClientBroadcast(new DiscoveryBroadcastData());
        }

        private void OnLocalSessionStarted() {
            networkManager.OnServerStarted -= OnLocalSessionStarted;
            StartLocalSession();
        }

        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response) {
            response = new DiscoveryResponseData() {
                serverName = currentSession.sessionName,
                port = ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            };
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response) {
            UnityEngine.Debug.Log($"Broadcast my Session {sender.Address} -- {sender.Port}");

            OnLocalSessionFound?.Invoke(sender, response);
            ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Address = sender.Address.ToString();
            ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port = (ushort)sender.Port;
            networkManager.StartClient();
        }

        private void OnClientDisconnected(ulong clientId) {
            if (clientId == networkManager.LocalClientId) {
                StopDiscovery();
            }
        }
    }
}
