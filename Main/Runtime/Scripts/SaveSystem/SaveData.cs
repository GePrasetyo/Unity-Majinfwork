using System;
using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Base class for all save data types.
    /// Extend this class to create domain-specific save data.
    /// </summary>
    [Serializable]
    public abstract class SaveData {
        /// <summary>
        /// The file name for this save data (without extension).
        /// </summary>
        public abstract string FileName { get; }

        /// <summary>
        /// When true, this save data will be preloaded asynchronously at game start.
        /// Consumers should await WaitForPreloadAsync() before accessing preloaded data.
        /// </summary>
        public virtual bool PreloadOnInit => false;

        /// <summary>
        /// Version of this save data format for migration support.
        /// </summary>
        public string version = "1";

        /// <summary>
        /// Whether this save data has been loaded from disk.
        /// </summary>
        [NonSerialized]
        public bool isLoaded;

        /// <summary>
        /// Whether this save data has unsaved changes.
        /// </summary>
        [NonSerialized]
        public bool isDirty;

        /// <summary>
        /// Called when creating a new save file.
        /// Override to populate initial data.
        /// </summary>
        public virtual Task CreateAsync(ISaveDataService saveService, CancellationToken cancellationToken = default) {
            return saveService.SaveAsync(this, cancellationToken);
        }

        /// <summary>
        /// Called when updating an existing save.
        /// Override to customize update behavior.
        /// </summary>
        public virtual Task UpdateAsync(ISaveDataService saveService, CancellationToken cancellationToken = default) {
            return saveService.SaveAsync(this, cancellationToken);
        }

        /// <summary>
        /// Called after loading to perform any post-load initialization.
        /// Override for custom post-load logic.
        /// </summary>
        public virtual void OnLoaded() {
            isLoaded = true;
            isDirty = false;
        }

        /// <summary>
        /// Called before saving to perform any pre-save preparation.
        /// Override for custom pre-save logic.
        /// </summary>
        public virtual void OnBeforeSave() {
            isDirty = false;
        }

        /// <summary>
        /// Marks this save data as having unsaved changes.
        /// </summary>
        public void MarkDirty() {
            isDirty = true;
        }

        /// <summary>
        /// Called when migrating from an older version.
        /// Override to handle version migrations.
        /// </summary>
        /// <param name="fromVersion">The version being migrated from.</param>
        public virtual void Migrate(string fromVersion) {
            // Override in subclasses to handle migrations
        }
    }

    /// <summary>
    /// Generic save data with a typed data collection.
    /// Use for save data that stores a single object or list.
    /// </summary>
    [Serializable]
    public abstract class SaveData<T> : SaveData where T : class {
        /// <summary>
        /// The data stored in this save.
        /// </summary>
        public T data;

        /// <summary>
        /// Creates save data with the specified initial data.
        /// </summary>
        protected SaveData() { }

        /// <summary>
        /// Creates save data with the specified initial data.
        /// </summary>
        protected SaveData(T initialData) {
            data = initialData;
        }
    }
}
