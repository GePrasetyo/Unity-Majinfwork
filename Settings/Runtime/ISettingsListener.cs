using System;

namespace Majinfwork.Settings {
    /// <summary>
    /// Receives notifications when any settings category is applied.
    /// Register via ISettingsService.AddListener / RemoveListener.
    /// </summary>
    public interface ISettingsListener {
        /// <summary>
        /// Called when a settings category is applied.
        /// </summary>
        /// <param name="settingsType">The concrete Type of the SettingsData that changed.</param>
        void OnSettingsApplied(Type settingsType);
    }

    /// <summary>
    /// Typed listener for a specific settings category.
    /// Receives the settings instance directly, avoiding manual casts.
    /// </summary>
    /// <typeparam name="T">The SettingsData type to listen for.</typeparam>
    public interface ISettingsListener<in T> : ISettingsListener where T : SettingsData {
        /// <summary>
        /// Called when settings of type T are applied.
        /// </summary>
        void OnSettingsApplied(T settings);
    }
}
