using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Majinfwork.Pool {
    public static class PoolByReference {
        private static readonly Dictionary<object, object> poolbyRefCollection = new Dictionary<object, object>();
        private static Transform parentPool;

        internal static void InitializePoolRef<T>(object key, int capacity = 1) where T : Component {
            if ((T)key == null) {
                return;
            }

            poolbyRefCollection[key] = new ObjectPool<T>(() => CreatePooledItem((T)key), OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, capacity);
        }

        /// <summary>
        /// Instantiate from Pooling System
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static bool InstantiatePoolRef<T>(this object key, out T output) where T : Component {
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
        /// Instantiate from Pooling System and Set Parent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static bool InstantiatePoolRef<T>(this object key, Transform parent, out T output) where T : Component {
            if (key.InstantiatePoolRef(out output)) {
                output.transform.SetParent(parent, worldPositionStays: false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Instantiate from Pooling System
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static bool InstantiatePoolRef<T>(this object key, Vector3 position, Quaternion rotation, out T output) where T : Component {
            if (key.InstantiatePoolRef(out output)) {
                output.transform.SetPositionAndRotation(position, rotation);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Instantiate from Pooling System, Set the parent transfom > World Position
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="position">World Position</param>
        /// <param name="rotation">World Rotation</param>
        /// <param name="parent"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public static bool InstantiatePoolRef<T>(this object key, Vector3 position, Quaternion rotation, Transform parent, out T output) where T : Component {
            if (key.InstantiatePoolRef(out output)) {
                output.transform.SetParent(parent, worldPositionStays: false);
                output.transform.SetPositionAndRotation(position, rotation);
                return true;
            }

            return false;
        }

        public static void Release<T>(this object item, object key, bool keepOn = false) where T : Component {
            T itemToRelease = item as T;

            if (key == null || itemToRelease == null) {
                return;
            }

            if (ReferenceEquals(key, item)) {
                throw new System.ArgumentException("Key and item to release are the same reference. You cannot use a pooled item as the key to release.", nameof(item));
            }

            if (poolbyRefCollection.TryGetValue(key, out var op)) {
                itemToRelease.gameObject.SetActive(keepOn);
                (op as ObjectPool<T>).Release(itemToRelease);
            }
        }

        private static T CreatePooledItem<T>(T item) where T : Component {
            return Object.Instantiate(item, parentPool.transform);
        }

        private static void OnReturnedToPool<T>(T obj) where T : Component {
            obj.transform.SetParent(parentPool);
        }

        private static void OnTakeFromPool<T>(T obj) where T : Component {
            obj.transform.SetParent(null);
            obj.gameObject.SetActive(true);
        }

        private static void OnDestroyPoolObject<T>(T obj) where T : Component {
            Object.Destroy(obj.gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitService() {
            var obj = new GameObject();
            obj.name = "[Service] Game Pool";
            Object.DontDestroyOnLoad(obj);
            parentPool = obj.transform;
        }
    }
}