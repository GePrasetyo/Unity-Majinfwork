using UnityEngine;
using UnityEngine.InputSystem;

namespace Majinfwork.World {
    public class PlayerInput : Actor {
        [SerializeField] private InputActionAsset inputAsset;

        private void OnEnable() {
            if (inputAsset != null) {
                inputAsset.Enable();
            }
        }

        private void OnDisable() {
            if (inputAsset != null) {
                inputAsset.Disable();
            }
        }
    }
}