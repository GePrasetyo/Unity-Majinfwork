using UnityEngine;

namespace Majinfwork {
    public class DefaultCameraHandler : CameraHandler {
        private Camera _controlledCamera;

        public override void Construct() {
            Camera.main.gameObject.SetActive(false);

            _controlledCamera = new GameObject("Player Camera").AddComponent<Camera>();
        }

        public override void Deconstruct() {
            
        }
    }
}