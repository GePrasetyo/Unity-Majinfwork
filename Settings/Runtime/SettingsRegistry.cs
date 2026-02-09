using System;
using System.Collections.Generic;
using System.Reflection;

namespace Majinfwork.Settings {
    /// <summary>
    /// Discovers all concrete SettingsData subclasses via reflection.
    /// Results are cached after the first call.
    /// </summary>
    public static class SettingsRegistry {
        private static Type[] cachedTypes;

        /// <summary>
        /// Gets all concrete SettingsData subclasses across loaded assemblies.
        /// </summary>
        public static Type[] GetAllSettingsTypes() {
            if (cachedTypes != null) return cachedTypes;

            var result = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++) {
                var assemblyName = assemblies[i].GetName().Name;

                if (assemblyName.StartsWith("System") ||
                    assemblyName.StartsWith("Unity") ||
                    assemblyName.StartsWith("mscorlib") ||
                    assemblyName.StartsWith("netstandard") ||
                    assemblyName.StartsWith("Microsoft"))
                    continue;

                try {
                    var types = assemblies[i].GetTypes();
                    for (int j = 0; j < types.Length; j++) {
                        var type = types[j];
                        if (!type.IsAbstract && type.IsSubclassOf(typeof(SettingsData))) {
                            result.Add(type);
                        }
                    }
                }
                catch (ReflectionTypeLoadException) { }
            }

            cachedTypes = result.ToArray();
            return cachedTypes;
        }

        /// <summary>
        /// Clears the cached types. Call after domain reload or hot-reload.
        /// </summary>
        public static void ClearCache() {
            cachedTypes = null;
        }
    }
}
