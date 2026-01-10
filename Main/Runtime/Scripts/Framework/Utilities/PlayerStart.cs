#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Majinfwork {
    public sealed class PlayerStart : MonoBehaviour {
        [Tooltip("Player index this spawn point is for (0 = Player 1, 1 = Player 2, etc.)")]
        [SerializeField] private int playerIndex;

        public int PlayerIndex => playerIndex;

        public static PlayerStart FindForPlayer(int index) {
            var allStarts = FindObjectsByType<PlayerStart>(FindObjectsSortMode.None);

            // First try to find exact match
            foreach (var start in allStarts) {
                if (start.playerIndex == index) return start;
            }

            // Fallback to first available if no match
            return allStarts.Length > 0 ? allStarts[0] : null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * 2);

            Gizmos.color = Color.yellow;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 1f);

            string nameLabel = $"Player Start [{playerIndex}]";
            Handles.Label(transform.position, nameLabel);
        }

        [MenuItem("GameObject/Majinfwork/Player Start", false, 10)]
        private static void CreatePlayerStart(MenuCommand menuCommand) {
            GameObject go = new GameObject("PlayerStart");
            go.AddComponent<PlayerStart>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
#endif
    }
}
