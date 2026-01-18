using System;
using System.Collections.Generic;

namespace Majinfwork.Pool {
    /// <summary>
    /// Use PoolByUnityObject instead
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PoolByObject<T> : IDisposable {
        private Stack<T> pool;
        private readonly Func<T> factory;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly Action<T> onDestroy;

        /// <param name="factory">Function to create a new instance (e.g., () => new Coin())</param>
        /// <param name="initialCapacity">How many objects to pre-warm</param>
        /// <param name="onGet">Optional: Action to run when object is taken (e.g., Activate)</param>
        /// <param name="onRelease">Optional: Action to run when object is returned (e.g., Deactivate)</param>
        public PoolByObject(Func<T> factory, 
            int initialCapacity = 10, 
            Action<T> onGet = null, 
            Action<T> onRelease = null,
            Action<T> onDestroy = null) {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.onDestroy = onDestroy;
            pool = new Stack<T>(initialCapacity);

            for (int i = 0; i < initialCapacity; i++) {
                pool.Push(this.factory());
            }
        }

        // Get an object from the pool, or create a new one if empty
        public T Get() {
            T item = pool.Count > 0 ? pool.Pop() : factory();

            onGet?.Invoke(item);
            return item;
        }

        // Return an object to the pool
        public void Release(T item) {
            onRelease?.Invoke(item);
            pool.Push(item);
        }

        public int CountInactive => pool.Count;

        public void Dispose() {
            if (pool == null) return;

            while (pool.Count > 0) {
                T item = pool.Pop();
                onDestroy?.Invoke(item);
            }

            pool = null;
        }
    }
}