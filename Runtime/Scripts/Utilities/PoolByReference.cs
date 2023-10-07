using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Majingari.Framework.Pool {
    public class PoolByReference {
        private readonly Dictionary<object, object> poolbyRefCollection = new Dictionary<object, object>();
        private Transform parentPool;

        /// <summary>
        /// Initialize Pool by prefab reference
        /// </summary>
        /// <typeparam name="T">Unity component</typeparam>
        /// <param name="key">prefab reference</param>
        /// <param name="capacity">capacity to instantiate</param>
        internal void InitializePoolRef<T>(object key, int capacity = 1) where T : Component {
            if ((T)key == null) {
                return;
            }

            poolbyRefCollection[key] = new ObjectPool<T>(() => CreatePooledItem((T)key), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, capacity);
        }

        /// <summary>
        /// Get Pool by Reference
        /// </summary>
        /// <typeparam name="T">Unity component</typeparam>
        /// <param name="go">output pool</param>
        /// <param name="key">prefab as a key</param>
        /// <returns></returns>
        internal bool GetPoolRef<T>(out T output, object key) where T : Component {
            if ((T)key == null) {
                output = null;
                return false;
            }

            if (!poolbyRefCollection.ContainsKey(key)) {
                InitializePoolRef<T>(key);
            }

            var op = poolbyRefCollection[key] as ObjectPool<T>;
            output = op.Get() as T;
            return true;
        }

        /// <summary>
        /// Release object to pool
        /// </summary>
        /// <typeparam name="T">Unity Component</typeparam>
        /// <param name="key">prefab reference</param>
        /// <param name="item">item to release to pool</param>
        internal void Release<T>(object key, object item) where T : Component {
            if ((T)key == null || (T)item == null) {
                return;
            }

            if (poolbyRefCollection.TryGetValue(key, out var op)) {
                (op as ObjectPool<T>).Release((T)item);
            }
        }

        private T CreatePooledItem<T>(T item) where T : Component {
            return Object.Instantiate(item, parentPool.transform);
        }

        private void OnReturnedToPool<T>(T obj) where T : Component {
            obj.transform.SetParent(parentPool);
        }

        private void OnTakeFromPool<T>(T obj) where T : Component {
            obj.transform.SetParent(null);
        }

        private void OnDestroyPoolObject<T>(T obj) where T : Component {
            Object.Destroy(obj.gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitService() {
            var obj = new GameObject();
            obj.name = "[Service] Game Pool";
            Object.DontDestroyOnLoad(obj);

            var pool = new PoolByReference();
            pool.parentPool = obj.transform;
            ServiceLocator.Register<PoolByReference>(pool);
        }
    }

    public static class PoolRefExtension {
        public static bool InstantiatePoolRef<T>(this object key, out T output) where T : Component {
            return ServiceLocator.Resolve<PoolByReference>().GetPoolRef(out output, key);
        }

        public static void ReleaseThisPoolRef<T>(this object item, object key) where T : Component {
            ServiceLocator.Resolve<PoolByReference>().Release<T>(key, item);
        }
    }
}