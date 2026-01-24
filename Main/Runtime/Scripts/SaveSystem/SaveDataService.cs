using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Main save data service implementation.
    /// Handles async file I/O with configurable serialization.
    /// Extend this class to customize save behavior.
    /// </summary>
    public class SaveDataService : ISaveDataService {
        protected readonly string baseDirectory;
        protected readonly int slotCount;
        protected readonly ISaveSerializer serializer;
        protected readonly List<ISaveListener> listeners = new List<ISaveListener>();

        protected SaveContainer container;
        protected SaveSlot currentSlot;
        protected bool isInitialized;

        // Cached paths to avoid repeated Path.Combine allocations
        private string cachedContainerPath;
        private string cachedSlotDirectory;
        private int cachedSlotIndex = -1;

        // Cached reflection results
        private Type[] cachedSaveDataTypes;

        // Preloaded save data instances
        protected readonly Dictionary<Type, SaveData> preloadedData = new Dictionary<Type, SaveData>();
        protected bool isPreloadComplete;
        protected TaskCompletionSource<bool> preloadCompletionSource = new TaskCompletionSource<bool>();

        public SaveSlot CurrentSlot => currentSlot;
        public SaveContainer Container => container;
        public bool IsInitialized => isInitialized;
        public bool IsPreloadComplete => isPreloadComplete;

        protected string ContainerPath => cachedContainerPath ??= Path.Combine(baseDirectory, SaveContainer.ContainerFileName + serializer.FileExtension);

        protected string CurrentSlotDirectory {
            get {
                if (currentSlot == null) return null;
                if (cachedSlotIndex != currentSlot.index) {
                    cachedSlotIndex = currentSlot.index;
                    cachedSlotDirectory = Path.Combine(baseDirectory, currentSlot.index.ToString());
                }
                return cachedSlotDirectory;
            }
        }

        /// <summary>
        /// Creates a new save data service.
        /// </summary>
        /// <param name="baseDirectory">Base directory for saves. Defaults to persistentDataPath/Saves.</param>
        /// <param name="slotCount">Number of save slots.</param>
        /// <param name="serializer">Serializer to use. Defaults to BinarySaveSerializer.</param>
        public SaveDataService(string baseDirectory = null, int slotCount = 3, ISaveSerializer serializer = null) {
            this.baseDirectory = baseDirectory ?? Path.Combine(Application.persistentDataPath, "Saves");
            this.slotCount = slotCount;
            this.serializer = serializer ?? new BinarySaveSerializer();
        }

        #region Initialization

        public virtual async Task InitializeAsync(CancellationToken cancellationToken = default) {
            if (isInitialized) {
                Debug.LogWarning("[SaveDataService] Already initialized.");
                return;
            }

            // Ensure base directory exists
            if (!Directory.Exists(baseDirectory)) {
                Directory.CreateDirectory(baseDirectory);
            }

            // Load or create container
            if (File.Exists(ContainerPath)) {
                await LoadContainerAsync(cancellationToken).ConfigureAwait(false);
            }
            else {
                await CreateContainerAsync(cancellationToken).ConfigureAwait(false);
            }

            OnInitialized();
            isInitialized = true;
            Debug.Log($"[SaveDataService] Initialized with {slotCount} slots at {baseDirectory}");
        }

        /// <summary>
        /// Called after initialization. Override for custom setup.
        /// </summary>
        protected virtual void OnInitialized() { }

        public virtual void Shutdown() {
            OnShutdown();
            listeners.Clear();
            container = null;
            currentSlot = null;
            isInitialized = false;
            Debug.Log("[SaveDataService] Shutdown complete.");
        }

        /// <summary>
        /// Called during shutdown. Override for custom cleanup.
        /// </summary>
        protected virtual void OnShutdown() { }

        #endregion

        #region Slot Management

        public virtual void SetCurrentSlot(int slotIndex) {
            if (container == null) {
                Debug.LogError("[SaveDataService] Container not loaded. Call InitializeAsync first.");
                return;
            }

            currentSlot = container.GetSlot(slotIndex);
            if (currentSlot == null) {
                Debug.LogError($"[SaveDataService] Invalid slot index: {slotIndex}");
                return;
            }

            // Ensure slot directory exists
            var slotDir = CurrentSlotDirectory;
            if (!Directory.Exists(slotDir)) {
                Directory.CreateDirectory(slotDir);
            }

            OnSlotChanged(currentSlot);
            Debug.Log($"[SaveDataService] Set current slot to {slotIndex}");
        }

        /// <summary>
        /// Called when the current slot changes. Override for custom handling.
        /// </summary>
        protected virtual void OnSlotChanged(SaveSlot slot) { }

        #endregion

        #region Save Operations

        public virtual async Task<bool> SaveAsync<T>(T data, CancellationToken cancellationToken = default) where T : SaveData {
            if (currentSlot == null) {
                Debug.LogError("[SaveDataService] No slot selected. Call SetCurrentSlot first.");
                return false;
            }

            try {
                data.OnBeforeSave();

                var filePath = GetFilePath(data.FileName);
                Debug.Log($"[SaveDataService] Saving {filePath}");

                await Task.Run(() => {
                    using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write)) {
                        serializer.Serialize(stream, data);
                    }
                }, cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException) {
                Debug.Log("[SaveDataService] Save cancelled.");
                throw;
            }
            catch (Exception e) {
                Debug.LogError($"[SaveDataService] Save failed: {e.Message}");
                return false;
            }
        }

        public virtual async Task<T> LoadAsync<T>(string fileName, CancellationToken cancellationToken = default) where T : SaveData {
            if (currentSlot == null) {
                Debug.LogError("[SaveDataService] No slot selected. Call SetCurrentSlot first.");
                return null;
            }

            try {
                var filePath = GetFilePath(fileName);
                Debug.Log($"[SaveDataService] Loading {filePath}");

                T data = null;
                await Task.Run(() => {
                    using (var stream = File.OpenRead(filePath)) {
                        data = serializer.Deserialize<T>(stream);
                    }
                }, cancellationToken).ConfigureAwait(false);

                data?.OnLoaded();
                return data;
            }
            catch (OperationCanceledException) {
                Debug.Log("[SaveDataService] Load cancelled.");
                throw;
            }
            catch (Exception e) {
                Debug.LogError($"[SaveDataService] Load failed: {e.Message}");
                return null;
            }
        }

        public virtual async Task<bool> UpdateAllAsync(CancellationToken cancellationToken = default) {
            if (currentSlot == null) {
                Debug.LogError("[SaveDataService] No slot selected.");
                return false;
            }

            try {
                // Run all listeners in parallel - avoid LINQ allocations
                var tasks = new List<Task>(listeners.Count);
                for (int i = 0; i < listeners.Count; i++) {
                    var listener = listeners[i];
                    if (listener != null) {
                        tasks.Add(listener.UpdateSaveAsync(cancellationToken));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                // Update slot metadata
                currentSlot.lastSaveTime = DateTime.UtcNow;
                await SaveContainerAsync(cancellationToken).ConfigureAwait(false);

                Debug.Log($"[SaveDataService] Updated all ({tasks.Count} listeners)");
                return true;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                Debug.LogError($"[SaveDataService] UpdateAll failed: {e.Message}");
                return false;
            }
        }

        public virtual async Task<bool> CreateNewSaveAsync(CancellationToken cancellationToken = default) {
            if (currentSlot == null) {
                Debug.LogError("[SaveDataService] No slot selected.");
                return false;
            }

            try {
                // Get all SaveData types and create instances
                var saveDataTypes = GetAllSaveDataTypes();

                foreach (var type in saveDataTypes) {
                    cancellationToken.ThrowIfCancellationRequested();

                    var instance = Activator.CreateInstance(type) as SaveData;
                    if (instance != null) {
                        await instance.CreateAsync(this, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Mark slot as valid
                currentSlot.isValid = true;
                currentSlot.lastSaveTime = DateTime.UtcNow;
                await SaveContainerAsync(cancellationToken).ConfigureAwait(false);

                OnNewSaveCreated(currentSlot);
                Debug.Log($"[SaveDataService] Created new save in slot {currentSlot.index}");
                return true;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                Debug.LogError($"[SaveDataService] CreateNewSave failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Called after a new save is created. Override to add custom slot metadata.
        /// </summary>
        protected virtual void OnNewSaveCreated(SaveSlot slot) { }

        public virtual async Task<bool> DeleteSlotAsync(int slotIndex, CancellationToken cancellationToken = default) {
            var slot = container?.GetSlot(slotIndex);
            if (slot == null) {
                Debug.LogError($"[SaveDataService] Invalid slot index: {slotIndex}");
                return false;
            }

            try {
                var slotDir = Path.Combine(baseDirectory, slotIndex.ToString());

                await Task.Run(() => {
                    if (Directory.Exists(slotDir)) {
                        var files = Directory.GetFiles(slotDir);
                        foreach (var file in files) {
                            File.Delete(file);
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);

                slot.Reset();
                await SaveContainerAsync(cancellationToken).ConfigureAwait(false);

                Debug.Log($"[SaveDataService] Deleted slot {slotIndex}");
                return true;
            }
            catch (Exception e) {
                Debug.LogError($"[SaveDataService] DeleteSlot failed: {e.Message}");
                return false;
            }
        }

        public virtual async Task<bool> ValidateSlotAsync(CancellationToken cancellationToken = default) {
            if (currentSlot == null) return false;

            try {
                var saveDataTypes = GetAllSaveDataTypes();

                foreach (var type in saveDataTypes) {
                    cancellationToken.ThrowIfCancellationRequested();

                    var instance = Activator.CreateInstance(type) as SaveData;
                    if (instance == null) continue;

                    var filePath = GetFilePath(instance.FileName);
                    if (!File.Exists(filePath)) {
                        Debug.LogWarning($"[SaveDataService] Missing file: {instance.FileName}");
                        return false;
                    }

                    // Try to deserialize
                    await Task.Run(() => {
                        using (var stream = File.OpenRead(filePath)) {
                            serializer.Deserialize<SaveData>(stream);
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception e) {
                Debug.LogError($"[SaveDataService] Validation failed: {e.Message}");
                return false;
            }
        }

        #endregion

        #region File Operations

        public bool FileExists(string fileName) {
            if (currentSlot == null) return false;
            return File.Exists(GetFilePath(fileName));
        }

        public IReadOnlyList<string> GetSlotFiles() {
            if (currentSlot == null) return Array.Empty<string>();

            var slotDir = CurrentSlotDirectory;
            if (!Directory.Exists(slotDir)) return Array.Empty<string>();

            // Avoid LINQ allocations
            var files = Directory.GetFiles(slotDir);
            var result = new List<string>(files.Length);
            for (int i = 0; i < files.Length; i++) {
                result.Add(Path.GetFileNameWithoutExtension(files[i]));
            }
            return result;
        }

        protected string GetFilePath(string fileName) {
            return Path.Combine(CurrentSlotDirectory, fileName + serializer.FileExtension);
        }

        #endregion

        #region Container Operations

        protected virtual async Task LoadContainerAsync(CancellationToken cancellationToken) {
            await Task.Run(() => {
                using (var stream = File.OpenRead(ContainerPath)) {
                    container = serializer.Deserialize<SaveContainer>(stream);
                }
            }, cancellationToken).ConfigureAwait(false);

            // Ensure slot directories exist
            foreach (var slot in container.slots) {
                var slotDir = Path.Combine(baseDirectory, slot.index.ToString());
                if (!Directory.Exists(slotDir)) {
                    Directory.CreateDirectory(slotDir);
                }
            }
        }

        protected virtual async Task CreateContainerAsync(CancellationToken cancellationToken) {
            container = CreateContainer(slotCount);

            // Create slot directories
            foreach (var slot in container.slots) {
                var slotDir = Path.Combine(baseDirectory, slot.index.ToString());
                Directory.CreateDirectory(slotDir);
            }

            await SaveContainerAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Factory method to create the container. Override to use custom SaveContainer.
        /// </summary>
        protected virtual SaveContainer CreateContainer(int slotCount) {
            return new SaveContainer(slotCount);
        }

        protected virtual async Task SaveContainerAsync(CancellationToken cancellationToken) {
            container.Touch();

            await Task.Run(() => {
                using (var stream = File.Open(ContainerPath, FileMode.Create, FileAccess.Write)) {
                    serializer.Serialize(stream, container);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Listener Management

        public void AddListener(ISaveListener listener) {
            if (listener != null && !listeners.Contains(listener)) {
                listeners.Add(listener);
            }
        }

        public void RemoveListener(ISaveListener listener) {
            listeners.Remove(listener);
        }

        #endregion

        #region Reflection Helpers

        /// <summary>
        /// Gets all SaveData types across all loaded assemblies.
        /// Results are cached after first call.
        /// Override to customize which types are included.
        /// </summary>
        protected virtual Type[] GetAllSaveDataTypes() {
            if (cachedSaveDataTypes != null) return cachedSaveDataTypes;

            var result = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++) {
                var assembly = assemblies[i];

                // Skip system and Unity assemblies for performance
                var assemblyName = assembly.GetName().Name;
                if (assemblyName.StartsWith("System") ||
                    assemblyName.StartsWith("Unity") ||
                    assemblyName.StartsWith("mscorlib") ||
                    assemblyName.StartsWith("netstandard")) {
                    continue;
                }

                try {
                    var types = assembly.GetTypes();
                    for (int j = 0; j < types.Length; j++) {
                        var type = types[j];
                        if (!type.IsAbstract && type.IsSubclassOf(typeof(SaveData))) {
                            result.Add(type);
                        }
                    }
                }
                catch (ReflectionTypeLoadException) {
                    // Skip assemblies that can't be loaded
                }
            }

            cachedSaveDataTypes = result.ToArray();
            return cachedSaveDataTypes;
        }

        /// <summary>
        /// Clears the cached SaveData types. Call if types are added dynamically.
        /// </summary>
        public void ClearTypeCache() {
            cachedSaveDataTypes = null;
        }

        #endregion

        #region Preloading

        public virtual async Task PreloadAllAsync(CancellationToken cancellationToken = default) {
            if (currentSlot == null) {
                Debug.LogError("[SaveDataService] No slot selected. Call SetCurrentSlot first.");
                preloadCompletionSource.TrySetResult(false);
                return;
            }

            var preloadTypes = GetPreloadSaveDataTypes();
            foreach (var type in preloadTypes) {
                cancellationToken.ThrowIfCancellationRequested();

                var instance = Activator.CreateInstance(type) as SaveData;
                if (instance == null) continue;

                var filePath = GetFilePath(instance.FileName);
                if (File.Exists(filePath)) {
                    try {
                        var loaded = await Task.Run(() => {
                            using (var stream = File.OpenRead(filePath)) {
                                return serializer.Deserialize<SaveData>(stream);
                            }
                        }, cancellationToken).ConfigureAwait(false);

                        if (loaded != null) {
                            loaded.OnLoaded();
                            preloadedData[type] = loaded;
                            Debug.Log($"[SaveDataService] Preloaded {type.Name}");
                        }
                    }
                    catch (Exception e) {
                        Debug.LogError($"[SaveDataService] Failed to preload {type.Name}: {e.Message}");
                    }
                }
                else {
                    // Create default instance if file doesn't exist
                    await instance.CreateAsync(this, cancellationToken).ConfigureAwait(false);
                    preloadedData[type] = instance;
                    Debug.Log($"[SaveDataService] Created default preload for {type.Name}");
                }
            }

            isPreloadComplete = true;
            preloadCompletionSource.TrySetResult(true);
            Debug.Log("[SaveDataService] Preload complete.");
        }

        public Task WaitForPreloadAsync() {
            return preloadCompletionSource.Task;
        }

        public virtual T GetPreloaded<T>() where T : SaveData {
            if (preloadedData.TryGetValue(typeof(T), out var data)) {
                return data as T;
            }
            return null;
        }

        /// <summary>
        /// Gets all SaveData types that have PreloadOnInit = true.
        /// </summary>
        protected virtual Type[] GetPreloadSaveDataTypes() {
            var allTypes = GetAllSaveDataTypes();
            var result = new List<Type>();
            for (int i = 0; i < allTypes.Length; i++) {
                var type = allTypes[i];
                var instance = Activator.CreateInstance(type) as SaveData;
                if (instance?.PreloadOnInit == true) {
                    result.Add(type);
                }
            }
            return result.ToArray();
        }

        #endregion
    }
}
