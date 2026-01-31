using System;
using UnityEngine;

namespace Majinfwork {
    [Serializable]
    public class CameraHandlerDefault : CameraHandler {
        [SerializeField] private Camera cameraPrefab;
        private Camera controlledCamera;

        public override void Construct() {
            if (Camera.main != null) {
                Camera.main.gameObject.SetActive(false);
            }

            if (cameraPrefab == null) {
                controlledCamera = new GameObject("Player Camera").AddComponent<Camera>();
            }
            else {
                controlledCamera = UnityEngine.Object.Instantiate(cameraPrefab);
            }

            // Tag as MainCamera so Camera.main finds it
            controlledCamera.gameObject.tag = "MainCamera";

            // Mark as persistent - GameMode controls lifecycle
            UnityEngine.Object.DontDestroyOnLoad(controlledCamera.gameObject);
        }

        public override void Deconstruct() {
            if (controlledCamera != null) {
                UnityEngine.Object.Destroy(controlledCamera.gameObject);
                controlledCamera = null;
            }
        }
    }
}