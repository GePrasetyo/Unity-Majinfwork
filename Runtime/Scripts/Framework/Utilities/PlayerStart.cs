#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace Majinfwork {
    public sealed class PlayerStart : MonoBehaviour {
#if UNITY_EDITOR
        private void OnDrawGizmos() {
            //Draw facing
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward*2);

            //Draw Box
            Gizmos.color = Color.yellow;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 1f);

            const string nameLabel = "Player Start";
            Handles.Label(transform.position, nameLabel);
        }
#endif
    }
}