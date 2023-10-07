using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

namespace Majingari.Network {
    [RequireComponent(typeof(NetworkManager))]
    public class LANDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData> {
        [Serializable]
        public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData> {
        };

        [SerializeField]
        [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
        private bool startWithServer = true;
        public string serverName = "EnterName";
        public ServerFoundEvent onServerFound;

        private NetworkManager networkManager;
        private bool hasStartedWithServer = false;

        public void Awake() {
            if(networkManager == null) {
                networkManager = GetComponent<NetworkManager>();
            }
        }

        public void Update() {
            if (startWithServer && hasStartedWithServer == false && isRunning == false) {
                if (networkManager.IsServer) {
                    StartServer();
                    hasStartedWithServer = true;
                }
            }
        }

        protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response) {
            response = new DiscoveryResponseData() {
                serverName = serverName,
                port = ((UnityTransport)networkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
            };
            return true;
        }

        protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response) {
            onServerFound.Invoke(sender, response);
        }
    }
}