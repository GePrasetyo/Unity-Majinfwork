using System;
using UnityEngine;

namespace Majinfwork {
    [Serializable]
    public class CameraHandlerDefault : CameraHandler {
        [SerializeField] private Camera cameraPrefab;
        private Camera controlledCamera;

        public override void Construct() {
            if(Camera.main != null) {
                Camera.main.gameObject.SetActive(false);
            }

            if(cameraPrefab == null) {
                controlledCamera = new GameObject("Player Camera").AddComponent<Camera>();
            }
            else {
                controlledCamera = UnityEngine.Object.Instantiate(cameraPrefab);
            }
        }

        public override void Deconstruct() {
            
        }
    }
}