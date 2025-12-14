using UnityEngine;

namespace Majinfwork.World {
    public abstract class HUDComponent : MonoBehaviour {
        protected void Awake() {
            Initialize();
        }

        protected abstract void Initialize();
    }
}