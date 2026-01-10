using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Majinfwork.Network {
    public abstract class NetworkDiscovery<TBroadCast, TResponse>
        where TBroadCast : INetworkSerializable, new()
        where TResponse : INetworkSerializable, new() {

        private enum MessageType : byte {
            BroadCast = 0,
            Response = 1,
        }

        private UdpClient udpClient;
        private CancellationTokenSource discoveryTokenSource;

        protected ushort port = 47777;
        protected const long LANBroadcastID = 5687486546;

        public bool IsRunning { get; private set; }
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }

        public void ClientBroadcast(TBroadCast broadCast) {
            if (!IsClient) {
                Debug.LogWarning("[NetworkDiscovery] Cannot send broadcast while not in client mode.");
                return;
            }

            if (udpClient == null) {
                Debug.LogWarning("[NetworkDiscovery] UDP client is null.");
                return;
            }

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, port);

            using (FastBufferWriter writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64)) {
                WriteHeader(writer, MessageType.BroadCast);
                writer.WriteNetworkSerializable(broadCast);
                var data = writer.ToArray();

                try {
                    udpClient.SendAsync(data, data.Length, endPoint);
                }
                catch (ObjectDisposedException) {
                    // Socket was closed, ignore
                }
                catch (Exception e) {
                    Debug.LogError($"[NetworkDiscovery] Failed to send broadcast: {e.Message}");
                }
            }
        }

        protected void StartLocalSession() {
            StartDiscovery(true);
        }

        protected void SearchLocalSession() {
            StartDiscovery(false);
        }

        public void StopDiscovery() {
            // Cancel any running async operations
            discoveryTokenSource?.Cancel();
            discoveryTokenSource?.Dispose();
            discoveryTokenSource = null;

            IsClient = false;
            IsServer = false;
            IsRunning = false;

            if (udpClient != null) {
                try {
                    udpClient.Close();
                }
                catch (Exception) {
                    // Socket exception during close is expected
                }
                udpClient = null;
            }
        }

        /// <summary>
        /// Gets called whenever a broadcast is received (on main thread).
        /// Creates a response based on the incoming broadcast data.
        /// </summary>
        protected abstract bool ProcessBroadcast(IPEndPoint sender, TBroadCast broadCast, out TResponse response);

        /// <summary>
        /// Gets called when a response to a broadcast gets received (on main thread).
        /// </summary>
        protected abstract void ResponseReceived(IPEndPoint sender, TResponse response);

        private void StartDiscovery(bool asServer) {
            StopDiscovery();

            IsServer = asServer;
            IsClient = !asServer;

            // Create cancellation token for this discovery session
            discoveryTokenSource = new CancellationTokenSource();

            // If we are not a server we use port 0 (let UDP client assign a free port)
            var bindPort = asServer ? port : 0;

            try {
                udpClient = new UdpClient(bindPort) {
                    EnableBroadcast = true,
                    MulticastLoopback = false
                };
            }
            catch (SocketException ex) {
                Debug.LogError($"[NetworkDiscovery] Failed to create UDP client on port {bindPort}: {ex.Message}");
                return;
            }

            // Start listening in background
            var token = discoveryTokenSource.Token;
            _ = ListenAsync(
                asServer ? () => ReceiveBroadcastAsync(token) : () => ReceiveResponseAsync(token),
                token
            );

            IsRunning = true;
            Debug.Log($"[NetworkDiscovery] Started as {(asServer ? "server" : "client")} on port {(asServer ? port : "dynamic")}");
        }

        private async Task ListenAsync(Func<Task> onReceiveTask, CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    await onReceiveTask();
                }
                catch (OperationCanceledException) {
                    // Expected when cancellation is requested
                    break;
                }
                catch (ObjectDisposedException) {
                    // Socket has been closed
                    break;
                }
                catch (SocketException ex) {
                    Debug.LogWarning($"[NetworkDiscovery] Socket error: {ex.Message}");
                    // Continue listening unless cancelled
                }
                catch (Exception ex) {
                    Debug.LogError($"[NetworkDiscovery] Unexpected error in receive loop: {ex}");
                }
            }
        }

        private async Task ReceiveResponseAsync(CancellationToken token) {
            if (udpClient == null) return;

            // Use Task.WhenAny to support cancellation with ReceiveAsync
            var receiveTask = udpClient.ReceiveAsync();
            var delayTask = Task.Delay(Timeout.Infinite, token);

            var completedTask = await Task.WhenAny(receiveTask, delayTask);

            if (completedTask == delayTask || token.IsCancellationRequested) {
                token.ThrowIfCancellationRequested();
                return;
            }

            var udpReceiveResult = await receiveTask;
            var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);

            // Use Allocator.Temp instead of Persistent
            using var reader = new FastBufferReader(segment, Allocator.Temp);

            try {
                if (!ReadAndCheckHeader(reader, MessageType.Response)) {
                    return;
                }

                reader.ReadNetworkSerializable(out TResponse receivedResponse);

                // Queue callback to main thread
                var endpoint = udpReceiveResult.RemoteEndPoint;
                MainThreadDispatcher.Enqueue(() => ResponseReceived(endpoint, receivedResponse));
            }
            catch (Exception ex) {
                Debug.LogError($"[NetworkDiscovery] Error processing response: {ex.Message}");
            }
        }

        private async Task ReceiveBroadcastAsync(CancellationToken token) {
            if (udpClient == null) return;

            // Use Task.WhenAny to support cancellation with ReceiveAsync
            var receiveTask = udpClient.ReceiveAsync();
            var delayTask = Task.Delay(Timeout.Infinite, token);

            var completedTask = await Task.WhenAny(receiveTask, delayTask);

            if (completedTask == delayTask || token.IsCancellationRequested) {
                token.ThrowIfCancellationRequested();
                return;
            }

            var udpReceiveResult = await receiveTask;
            var segment = new ArraySegment<byte>(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);

            // Use Allocator.Temp instead of Persistent
            using var reader = new FastBufferReader(segment, Allocator.Temp);

            try {
                if (!ReadAndCheckHeader(reader, MessageType.BroadCast)) {
                    return;
                }

                reader.ReadNetworkSerializable(out TBroadCast receivedBroadcast);

                // Process on main thread and send response
                var endpoint = udpReceiveResult.RemoteEndPoint;
                MainThreadDispatcher.Enqueue(() => {
                    if (ProcessBroadcast(endpoint, receivedBroadcast, out TResponse response)) {
                        SendResponseAsync(endpoint, response);
                    }
                });
            }
            catch (Exception ex) {
                Debug.LogError($"[NetworkDiscovery] Error processing broadcast: {ex.Message}");
            }
        }

        private async void SendResponseAsync(IPEndPoint endpoint, TResponse response) {
            if (udpClient == null) return;

            // Use Allocator.Temp instead of Persistent
            using var writer = new FastBufferWriter(1024, Allocator.Temp, 1024 * 64);
            WriteHeader(writer, MessageType.Response);
            writer.WriteNetworkSerializable(response);
            var data = writer.ToArray();

            try {
                await udpClient.SendAsync(data, data.Length, endpoint);
            }
            catch (ObjectDisposedException) {
                // Socket was closed, ignore
            }
            catch (Exception ex) {
                Debug.LogError($"[NetworkDiscovery] Failed to send response: {ex.Message}");
            }
        }

        private void WriteHeader(FastBufferWriter writer, MessageType messageType) {
            writer.WriteValueSafe(LANBroadcastID);
            writer.WriteByteSafe((byte)messageType);
        }

        private bool ReadAndCheckHeader(FastBufferReader reader, MessageType expectedType) {
            reader.ReadValueSafe(out long receivedApplicationId);
            if (receivedApplicationId != LANBroadcastID) {
                return false;
            }

            reader.ReadByteSafe(out byte messageType);
            if (messageType != (byte)expectedType) {
                return false;
            }

            return true;
        }
    }
}
