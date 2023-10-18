using System;
using UnityEngine;

namespace Majingari.Framework.World {
    [Serializable]
    public abstract class LoadingStreamer {
        public abstract void Construct();
    }
}