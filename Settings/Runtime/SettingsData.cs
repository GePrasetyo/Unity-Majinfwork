using System;

namespace Majinfwork.Settings {
    /// <summary>
    /// Base class for all settings categories.
    /// Extend to create domain-specific settings (graphics, audio, gameplay, etc.).
    /// Each subclass persists as its own JSON file.
    /// </summary>
    [Serializable]
    public abstract class SettingsData {
        internal SettingsData() { }
        /// <summary>
        /// File name for this settings category (without extension).
        /// Must be unique across all settings types.
        /// </summary>
        public abstract string FileName { get; }

        /// <summary>
        /// Display name shown in UI. Override for a human-friendly label.
        /// </summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        /// Sort order for UI enumeration. Lower values appear first.
        /// Built-in categories use 0-9. Use 10+ for game-specific settings.
        /// </summary>
        public virtual int Order => 0;

        /// <summary>
        /// Version string for migration support.
        /// </summary>
        public string version = "1";

        /// <summary>
        /// Whether this settings data has unsaved changes.
        /// </summary>
        [NonSerialized] public bool isDirty;

        /// <summary>
        /// Called after loading from disk.
        /// </summary>
        public virtual void OnLoaded() {
            isDirty = false;
        }

        /// <summary>
        /// Called before saving to disk.
        /// </summary>
        public virtual void OnBeforeSave() {
            isDirty = false;
        }

        /// <summary>
        /// Pushes current values to the engine or runtime systems.
        /// Called automatically after load and on ApplyAsync.
        /// Override in categories that directly control Unity systems (e.g., GraphicsSettings).
        /// Leave empty for middleware-agnostic categories (e.g., AudioSettings).
        /// </summary>
        public virtual void Apply() { }

        /// <summary>
        /// Resets all values to their defaults.
        /// </summary>
        public abstract void ResetToDefaults();

        /// <summary>
        /// Marks this settings data as having unsaved changes.
        /// </summary>
        public void MarkDirty() {
            isDirty = true;
        }

        /// <summary>
        /// Called when migrating from an older version.
        /// Override to handle version-specific data transformations.
        /// </summary>
        public virtual void Migrate(string fromVersion) { }
    }

    /// <summary>
    /// Generic base with a static Current accessor.
    /// Usage: public class MySettings : SettingsData&lt;MySettings&gt; { ... }
    /// Then access via MySettings.Current.
    /// </summary>
    [Serializable]
    public abstract class SettingsData<T> : SettingsData where T : SettingsData {
        public static T Current => ServiceLocator.Resolve<ISettingsService>()?.Get<T>();
    }
}
