namespace Majinfwork.Network {
    /// <summary>
    /// The game being played decides whether a connecting player may join it now (Unreal's GameMode PreLogin). The server's
    /// connection approval asks the one registered with the ServiceLocator, once the connection itself is valid (version,
    /// password, room); the host's own connection is never asked about.
    /// </summary>
    public interface IPreLogin {
        /// <summary>Server: why <paramref name="payload"/>'s player may not join now (sent to them), or null to let them in.</summary>
        string PreLogin(ulong clientId, ConnectionPayload payload);
    }
}
