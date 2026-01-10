using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Main interface for the save data service.
    /// Handles saving, loading, and managing save slots.
    /// </summary>
    public interface ISaveDataService {
        /// <summary>
        /// The currently active save slot.
        /// </summary>
        SaveSlot CurrentSlot { get; }

        /// <summary>
        /// The save container with all slot metadata.
        /// </summary>
        SaveContainer Container { get; }

        /// <summary>
        /// Whether the service is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the save system.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Shuts down the save system.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Sets the current save slot by index.
        /// </summary>
        void SetCurrentSlot(int slotIndex);

        /// <summary>
        /// Saves a single SaveData object to the current slot.
        /// </summary>
        Task<bool> SaveAsync<T>(T data, CancellationToken cancellationToken = default) where T : SaveData;

        /// <summary>
        /// Loads a SaveData object from the current slot.
        /// </summary>
        Task<T> LoadAsync<T>(string fileName, CancellationToken cancellationToken = default) where T : SaveData;

        /// <summary>
        /// Updates all registered listeners and saves their data.
        /// </summary>
        Task<bool> UpdateAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new save in the current slot.
        /// </summary>
        Task<bool> CreateNewSaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a save slot.
        /// </summary>
        Task<bool> DeleteSlotAsync(int slotIndex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that all save data in the current slot can be loaded.
        /// </summary>
        Task<bool> ValidateSlotAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists in the current slot.
        /// </summary>
        bool FileExists(string fileName);

        /// <summary>
        /// Gets all file names in the current slot.
        /// </summary>
        IReadOnlyList<string> GetSlotFiles();

        /// <summary>
        /// Adds a save listener.
        /// </summary>
        void AddListener(ISaveListener listener);

        /// <summary>
        /// Removes a save listener.
        /// </summary>
        void RemoveListener(ISaveListener listener);
    }
}
