using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.Settings {
    /// <summary>
    /// Main interface for the settings service.
    /// Handles loading, saving, applying, and discovering settings categories.
    /// </summary>
    public interface ISettingsService {
        /// <summary>
        /// Whether the service has been initialized and settings loaded.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the service: creates directory, discovers settings types,
        /// loads from disk (or creates defaults), and applies all.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Shuts down the service and clears all cached data.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Gets a settings instance by type. Returns the cached in-memory copy.
        /// Creates and caches a default instance if the type hasn't been loaded yet.
        /// </summary>
        T Get<T>() where T : SettingsData;

        /// <summary>
        /// Applies a settings category: pushes values to engine, saves to disk, notifies listeners.
        /// </summary>
        Task<bool> ApplyAsync<T>(T settings, CancellationToken cancellationToken = default) where T : SettingsData;

        /// <summary>
        /// Applies a settings category (non-generic overload for runtime-typed usage).
        /// </summary>
        Task<bool> ApplyAsync(SettingsData settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all dirty settings to disk without calling Apply.
        /// </summary>
        Task<bool> SaveAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reloads all settings from disk, discarding in-memory changes.
        /// </summary>
        Task ReloadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets a settings category to defaults, applies, and saves.
        /// </summary>
        Task<bool> ResetToDefaultsAsync<T>(CancellationToken cancellationToken = default) where T : SettingsData;

        /// <summary>
        /// Returns all loaded settings categories, sorted by Order.
        /// Useful for dynamically building a settings UI.
        /// </summary>
        IReadOnlyList<SettingsData> GetAll();

        /// <summary>
        /// Adds a settings change listener.
        /// </summary>
        void AddListener(ISettingsListener listener);

        /// <summary>
        /// Removes a settings change listener.
        /// </summary>
        void RemoveListener(ISettingsListener listener);
    }
}
