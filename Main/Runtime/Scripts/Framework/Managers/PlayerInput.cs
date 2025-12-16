using UnityEngine;
using UnityEngine.InputSystem;

namespace Majinfwork.World {
    public class PlayerInput : Actor {
        [SerializeField] private InputActionMap inputMap;

        private void OnEnable() {
            inputMap.Enable();
        }

        private void OnDisable() {
            inputMap.Disable();
        }
    }
}