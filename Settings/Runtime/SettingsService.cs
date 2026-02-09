using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Majinfwork.Settings {
    /// <summary>
    /// Core settings service implementation.
    /// Manages global settings with JSON persistence.
    /// </summary>
    public class SettingsService : ISettingsService {
        private readonly string directory;
        private readonly ISettingsSerializer serializer;
        private readonly Dictionary<Type, SettingsData> settings = new Dictionary<Type, SettingsData>();
        private readonly List<ISettingsListener> listeners = new List<ISettingsListener>();

        private List<SettingsData> sortedCache;
        private bool sortedCacheDirty = true;
        private bool isInitialized;

        public bool IsInitialized => isInitialized;

        /// <param name="directory">Settings file directory. Defaults to persistentDataPath/Settings.</param>
        /// <param name="serializer">Serializer to use. Defaults to pretty-printed JSON.</param>
        public SettingsService(string directory = null, ISettingsSerializer serializer = null) {
            this.directory = directory ?? Path.Combine(Application.persistentDataPath, "Settings");
            this.serializer = serializer ?? new JsonSettingsSerializer(prettyPrint: true);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default) {
            if (isInitialized) {
                Debug.LogWarning("[SettingsService] Already initialized.");
                return;
            }

            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var types = SettingsRegistry.GetAllSettingsTypes();

            for (int i = 0; i < types.Length; i++) {
                cancellationToken.ThrowIfCancellationRequested();

                var type = types[i];
                var instance = (SettingsData)Activator.CreateInstance(type);
                var filePath = GetFilePath(instance.FileName);

                if (File.Exists(filePath)) {
                    try {
                        SettingsData loaded = null;
                        await Task.Run(() => {
                            using (var stream = File.OpenRead(filePath)) {
                                loaded = (SettingsData)serializer.Deserialize(stream, type);
                            }
                        }, cancellationToken);

                        if (loaded != null) {
                            loaded.OnLoaded();
                            settings[type] = loaded;
                        }
                        else {
                            instance.ResetToDefaults();
                            settings[type] = instance;
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception e) {
                        Debug.LogError($"[SettingsService] Failed to load {type.Name}: {e.Message}");
                        instance.ResetToDefaults();
                        settings[type] = instance;
                    }
                }
                else {
                    instance.ResetToDefaults();
                    settings[type] = instance;
                }
            }

            foreach (var kvp in settings) {
                try {
                    kvp.Value.Apply();
                }
                catch (Exception e) {
                    Debug.LogError($"[SettingsService] Failed to apply {kvp.Key.Name}: {e.Message}");
                }
            }

            sortedCacheDirty = true;
            isInitialized = true;
            Debug.Log($"[SettingsService] Initialized with {settings.Count} categories.");
        }

        public void Shutdown() {
            listeners.Clear();
            settings.Clear();
            sortedCache = null;
            isInitialized = false;
        }

        public T Get<T>() where T : SettingsData {
            if (settings.TryGetValue(typeof(T), out var data)) {
                return (T)data;
            }

            var instance = Activator.CreateInstance<T>();
            instance.ResetToDefaults();
            settings[typeof(T)] = instance;
            sortedCacheDirty = true;
            return instance;
        }

        public Task<bool> ApplyAsync<T>(T data, CancellationToken cancellationToken = default) where T : SettingsData {
            return ApplyAsync((SettingsData)data, cancellationToken);
        }

        public async Task<bool> ApplyAsync(SettingsData data, CancellationToken cancellationToken = default) {
            if (data == null) return false;

            try {
                var type = data.GetType();
                settings[type] = data;
                sortedCacheDirty = true;

                data.Apply();

                data.OnBeforeSave();
                var filePath = GetFilePath(data.FileName);
                await Task.Run(() => {
                    using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                        serializer.Serialize(stream, data);
                    }
                }, cancellationToken).ConfigureAwait(false);

                NotifyListeners(data, type);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) {
                Debug.LogError($"[SettingsService] Apply failed for {data.GetType().Name}: {e.Message}");
                return false;
            }
        }

        public async Task<bool> SaveAllAsync(CancellationToken cancellationToken = default) {
            try {
                foreach (var kvp in settings) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var data = kvp.Value;
                    if (!data.isDirty) continue;

                    data.OnBeforeSave();
                    var filePath = GetFilePath(data.FileName);
                    await Task.Run(() => {
                        using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                            serializer.Serialize(stream, data);
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) {
                Debug.LogError($"[SettingsService] SaveAll failed: {e.Message}");
                return false;
            }
        }

        public async Task ReloadAsync(CancellationToken cancellationToken = default) {
            settings.Clear();
            sortedCache = null;
            isInitialized = false;
            await InitializeAsync(cancellationToken);
        }

        public async Task<bool> ResetToDefaultsAsync<T>(CancellationToken cancellationToken = default) where T : SettingsData {
            var instance = Activator.CreateInstance<T>();
            instance.ResetToDefaults();
            return await ApplyAsync(instance, cancellationToken);
        }

        public IReadOnlyList<SettingsData> GetAll() {
            if (sortedCacheDirty || sortedCache == null) {
                sortedCache = new List<SettingsData>(settings.Values);
                sortedCache.Sort((a, b) => a.Order.CompareTo(b.Order));
                sortedCacheDirty = false;
            }
            return sortedCache;
        }

        public void AddListener(ISettingsListener listener) {
            if (listener != null && !listeners.Contains(listener)) {
                listeners.Add(listener);
            }
        }

        public void RemoveListener(ISettingsListener listener) {
            listeners.Remove(listener);
        }

        private string GetFilePath(string fileName) {
            return Path.Combine(directory, fileName + serializer.FileExtension);
        }

        private void NotifyListeners(SettingsData data, Type type) {
            for (int i = listeners.Count - 1; i >= 0; i--) {
                try {
                    var listener = listeners[i];
                    listener.OnSettingsApplied(type);
                    DispatchTypedListener(listener, data, type);
                }
                catch (Exception e) {
                    Debug.LogError($"[SettingsService] Listener error: {e.Message}");
                }
            }
        }

        private static void DispatchTypedListener(ISettingsListener listener, SettingsData data, Type type) {
            var genericInterface = typeof(ISettingsListener<>).MakeGenericType(type);
            if (genericInterface.IsInstanceOfType(listener)) {
                var method = genericInterface.GetMethod("OnSettingsApplied", new[] { type });
                method?.Invoke(listener, new object[] { data });
            }
        }
    }
}
