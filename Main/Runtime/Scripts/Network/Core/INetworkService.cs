using System;
using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.Network {
    /// <summary>
    /// Network service for hosting, joining, and leaving multiplayer sessions.
    /// All operations are async for clean, modern C# code.
    /// </summary>
    public interface INetworkService {
        /// <summary>Current session info (null if not in a session).</summary>
        SessionInfo CurrentSession { get; }

        /// <summary>Current connection status.</summary>
        ConnectionStatus Status { get; }

        /// <summary>True if connected (as client or host).</summary>
        bool IsConnected { get; }

        /// <summary>True if hosting a session.</summary>
        bool IsHost { get; }

        /// <summary>Fired when connection status changes.</summary>
        event Action<ConnectionStatus> OnStatusChanged;

        /// <summary>Fired on server when a client connects.</summary>
        event Action<ulong> OnClientConnected;

        /// <summary>Fired when a client disconnects (with reason).</summary>
        event Action<ulong, ConnectionStatus> OnClientDisconnected;

        /// <summary>Initializes the network service. Must be called before other methods.</summary>
        void Initialize(NetworkConfig config);

        /// <summary>Shuts down the network service. Call on application quit.</summary>
        void Shutdown();

        /// <summary>
        /// Hosts a session and waits until the server starts or fails.
        /// </summary>
        /// <param name="settings">Session configuration.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>ConnectionStatus.Hosting on success, or failure reason.</returns>
        Task<ConnectionStatus> HostSessionAsync(SessionSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Joins a session by address and waits until connected or rejected.
        /// </summary>
        /// <param name="address">Server IP address.</param>
        /// <param name="port">Server port.</param>
        /// <param name="password">Session password (null if none).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>ConnectionStatus.Connected on success, or rejection reason.</returns>
        Task<ConnectionStatus> JoinSessionAsync(string address, ushort port, string password = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Joins a discovered session and waits until connected or rejected.
        /// </summary>
        /// <param name="session">Session discovered via LAN scan.</param>
        /// <param name="password">Session password (null if none).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>ConnectionStatus.Connected on success, or rejection reason.</returns>
        Task<ConnectionStatus> JoinSessionAsync(DiscoveredSession session, string password = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Leaves the current session and waits until fully disconnected.
        /// </summary>
        Task LeaveSessionAsync();
    }
}
