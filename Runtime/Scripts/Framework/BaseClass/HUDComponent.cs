using UnityEngine;

namespace Majingari.Framework.World {
    public abstract class HUDComponent : MonoBehaviour {
        protected void Awake() {
            Initialize();
        }

        protected abstract void Initialize();
    }
}