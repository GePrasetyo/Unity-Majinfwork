namespace Majinfwork.Network {
    public enum ConnectionStatus {
        // Connection states
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Hosting = 3,

        // Success
        Success = 100,

        // Rejection reasons
        ServerFull = 200,
        IncorrectPassword = 201,
        ProtocolMismatch = 202,
        InvalidPlayerName = 203,
        PayloadTooLarge = 204,
        AlreadyConnected = 205,
        Banned = 206,
        SessionNotFound = 207,
        Timeout = 208,
        GenericFailure = 299
    }

    public static class ConnectionStatusExtensions {
        public static bool IsConnected(this ConnectionStatus status) {
            return status == ConnectionStatus.Connected || status == ConnectionStatus.Hosting;
        }

        public static bool IsRejection(this ConnectionStatus status) {
            return (int)status >= 200 && (int)status < 300;
        }

        public static string ToMessage(this ConnectionStatus status) {
            return status switch {
                ConnectionStatus.Disconnected => "Disconnected",
                ConnectionStatus.Connecting => "Connecting...",
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.Hosting => "Hosting",
                ConnectionStatus.Success => "Connection successful",
                ConnectionStatus.ServerFull => "Server is full",
                ConnectionStatus.IncorrectPassword => "Incorrect password",
                ConnectionStatus.ProtocolMismatch => "Version mismatch",
                ConnectionStatus.InvalidPlayerName => "Invalid player name",
                ConnectionStatus.PayloadTooLarge => "Connection data too large",
                ConnectionStatus.AlreadyConnected => "Already connected",
                ConnectionStatus.Banned => "Banned from server",
                ConnectionStatus.SessionNotFound => "Session not found",
                ConnectionStatus.Timeout => "Connection timed out",
                ConnectionStatus.GenericFailure => "Connection failed",
                _ => "Unknown status"
            };
        }
    }
}
