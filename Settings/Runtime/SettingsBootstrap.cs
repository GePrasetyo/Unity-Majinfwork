using UnityEngine;

namespace Majinfwork.Settings {
    internal static class SettingsBootstrap {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize() {
            var settingsService = new SettingsService();
            ServiceLocator.Register<ISettingsService>(settingsService);
            _ = settingsService.InitializeAsync();

            Application.quitting += Shutdown;
        }

        private static void Shutdown() {
            Application.quitting -= Shutdown;

            var settingsService = ServiceLocator.Resolve<ISettingsService>();
            if (settingsService != null) {
                settingsService.Shutdown();
                ServiceLocator.Unregister<ISettingsService>(out _);
            }
        }
    }
}
