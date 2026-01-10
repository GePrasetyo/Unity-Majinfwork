using UnityEngine;

namespace Majinfwork.Network {
    public class ConnectionApprovalValidator {
        private readonly NetworkConfig config;
        private readonly ISessionManager sessionManager;

        public ConnectionApprovalValidator(NetworkConfig config, ISessionManager sessionManager) {
            this.config = config;
            this.sessionManager = sessionManager;
        }

        public (bool approved, ConnectionStatus status, ConnectionPayload payload) Validate(
            byte[] connectionData,
            int currentConnectedCount) {

            // 1. Payload size check
            if (connectionData == null || connectionData.Length == 0) {
                Debug.LogWarning("[ApprovalValidator] Empty connection data");
                return (false, ConnectionStatus.GenericFailure, null);
            }

            if (connectionData.Length > config.maxConnectPayload) {
                Debug.LogWarning($"[ApprovalValidator] Payload too large: {connectionData.Length} > {config.maxConnectPayload}");
                return (false, ConnectionStatus.PayloadTooLarge, null);
            }

            // 2. Deserialize payload
            if (!ConnectionPayload.TryFromBytes(connectionData, out var payload)) {
                Debug.LogError("[ApprovalValidator] Failed to deserialize connection payload");
                return (false, ConnectionStatus.GenericFailure, null);
            }

            // 3. Validate through session manager
            if (!sessionManager.ValidateJoinRequest(payload, out var rejectionReason)) {
                return (false, rejectionReason, payload);
            }

            Debug.Log($"[ApprovalValidator] Approved: {payload.playerName} (GUID: {payload.clientGUID})");
            return (true, ConnectionStatus.Success, payload);
        }

        public static string GetRejectionMessage(ConnectionStatus status) {
            return status.ToMessage();
        }
    }
}
