using System;
using UnityEngine.Pool;

namespace Majinfwork.Pool {
    public class PoolByUnityObject<T> : IDisposable  where T : class {
        private readonly ObjectPool<T> pool;

        public PoolByUnityObject(
            Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            int defaultCapacity = 10,
            int maxSize = 10000) {
            pool = new ObjectPool<T>(
                createFunc,           // How to create a new item
                actionOnGet,          // What to do when retrieved
                actionOnRelease,      // What to do when returned
                actionOnDestroy,      // What to do if the pool exceeds maxSize and must destroy the item
                true,                 // Collection check: Throws an error if you release the same item twice
                defaultCapacity,
                maxSize
            );
        }

        public T Get() => pool.Get();

        public void Release(T item) => pool.Release(item);

        public void Clear() => pool.Clear();

        public void Dispose() {
            pool.Dispose();
        }
    }
}