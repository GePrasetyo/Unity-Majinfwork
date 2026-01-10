using System.Threading;
using System.Threading.Tasks;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Interface for objects that need to save data when UpdateAllAsync is called.
    /// Implement this to participate in bulk save operations.
    /// </summary>
    public interface ISaveListener {
        /// <summary>
        /// Called when the save system performs a bulk save.
        /// Implementations should save their relevant SaveData objects.
        /// </summary>
        Task UpdateSaveAsync(CancellationToken cancellationToken = default);
    }
}
