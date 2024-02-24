using UnityEngine;

namespace Majingari.Framework.World {
    public abstract class GameScriptableObject : ScriptableObject {
        /// <summary>
        /// This triggered after splash screen loaded and before first scene loaded
        /// </summary>
        public abstract void PreInitialize();
    }
}