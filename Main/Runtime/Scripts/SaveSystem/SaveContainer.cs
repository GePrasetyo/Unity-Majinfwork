using System;
using System.Collections.Generic;

namespace Majinfwork.SaveSystem {
    /// <summary>
    /// Container for save slot metadata.
    /// Stored as a single file that tracks all save slots.
    /// </summary>
    [Serializable]
    public class SaveContainer {
        /// <summary>
        /// File name for the container (without extension).
        /// </summary>
        public const string ContainerFileName = "SaveContainer";

        /// <summary>
        /// All save slots.
        /// </summary>
        public List<SaveSlot> slots = new List<SaveSlot>();

        /// <summary>
        /// Version of the container format.
        /// </summary>
        public string version = "1";

        /// <summary>
        /// When the container was last modified.
        /// </summary>
        public DateTime lastModified;

        /// <summary>
        /// Creates an empty container.
        /// </summary>
        public SaveContainer() { }

        /// <summary>
        /// Creates a container with the specified number of slots.
        /// </summary>
        public SaveContainer(int slotCount) {
            for (int i = 0; i < slotCount; i++) {
                slots.Add(CreateSlot(i));
            }
            lastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Factory method to create a slot. Override to use custom SaveSlot types.
        /// </summary>
        protected virtual SaveSlot CreateSlot(int index) {
            return new SaveSlot(index);
        }

        /// <summary>
        /// Gets a slot by index. Returns null if out of range.
        /// </summary>
        public SaveSlot GetSlot(int index) {
            if (index < 0 || index >= slots.Count) return null;
            return slots[index];
        }

        /// <summary>
        /// Gets all valid (non-empty) slots.
        /// </summary>
        public IEnumerable<SaveSlot> GetValidSlots() {
            foreach (var slot in slots) {
                if (slot.isValid) yield return slot;
            }
        }

        /// <summary>
        /// Gets the first empty slot, or null if all slots are used.
        /// </summary>
        public SaveSlot GetFirstEmptySlot() {
            foreach (var slot in slots) {
                if (!slot.isValid) return slot;
            }
            return null;
        }

        /// <summary>
        /// Gets the number of valid saves.
        /// </summary>
        public int ValidSaveCount {
            get {
                int count = 0;
                foreach (var slot in slots) {
                    if (slot.isValid) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Total number of slots.
        /// </summary>
        public int SlotCount => slots.Count;

        /// <summary>
        /// Adds a new slot and returns it.
        /// </summary>
        public SaveSlot AddSlot() {
            var slot = CreateSlot(slots.Count);
            slots.Add(slot);
            return slot;
        }

        /// <summary>
        /// Updates the last modified timestamp.
        /// </summary>
        public void Touch() {
            lastModified = DateTime.UtcNow;
        }
    }
}
