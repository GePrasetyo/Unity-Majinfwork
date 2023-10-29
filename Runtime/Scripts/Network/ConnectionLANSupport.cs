using System.Net;
using System;
using Unity.Netcode.Transports.UTP;

namespace Majingari.Network {
    public class ConnectionLANSupport : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData> {
        public static event Action<IPEndPoint, DiscoveryResponseData> OnLocalSessionFound;
        private UNetcodeConnectionHandler connectionHandler;

        public ConnectionLANSupport(UNetcodeConnectionHandler connectionHandler) {
            this.connectionHandler = connectionHandler;

            connectionHandler.networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public void StartLocalSesssionHost() {
            connectionHandler.networkManager.OnServerStarted += OnLocalSessionStarted;
            connectionHandler.StartGameSessionHost();
        }

        public void AutoJoinLocalSession() {
            SearchLocalSession();
            ClientBroadcast(new DiscoveryBroadcastData());
        }

        private void OnLocalSessionStarted() {
            connectionHandler.networkManager.OnServerStarted -= OnLocalSessionStarted;
            StartLocalSession();
        }

        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response) {
            response = new DiscoveryResponseData() {
                serverName = connectionHandler.currentSessionState.sessionName,
                port = ((UnityTransport)connectionHandler.networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            };
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response) {
            UnityEngine.Debug.Log($"Receive a Session {sender.Address} -- {sender.Port}");

            OnLocalSessionFound?.Invoke(sender, response);
            ((UnityTransport)connectionHandler.networkManager.NetworkConfig.NetworkTransport).ConnectionData.Address = sender.Address.ToString();
            ((UnityTransport)connectionHandler.networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port = (ushort)sender.Port;
            connectionHandler.StartGameSesssionClient();
        }

        private void OnClientDisconnected(ulong clientId) {
            if (clientId == connectionHandler.networkManager.LocalClientId) {
                StopDiscovery();
            }
        }
    }
}
