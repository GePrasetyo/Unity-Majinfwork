using UnityEngine;
using UnityEngine.InputSystem;

namespace Majinfwork.World {
    public class PlayerInput : Actor {
        [SerializeField] private InputActionAsset inputAsset;

        private void OnEnable() {
            inputAsset.Enable();
        }

        private void OnDisable() {
            inputAsset.Disable();
        }
    }
}