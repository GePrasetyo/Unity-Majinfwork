using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Majinfwork.Localization {
    public class LocalizationDropdown : Dropdown {
        private new IEnumerator Start() {
            base.Start();

            yield return LocalizationSettings.InitializationOperation;
            PopulateLanguageOptions();
        }

        private void PopulateLanguageOptions() {
            int selected = 0;
            for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; ++i) {
                Locale locale = LocalizationSettings.AvailableLocales.Locales[i];
                if (LocalizationSettings.SelectedLocale == locale) {
                    selected = i;
                }
            }
            value = selected;
            onValueChanged.AddListener((index) => {
                if (index >= LocalizationSettings.AvailableLocales.Locales.Count) {
                    return;
                }
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
            });
            RefreshShownValue();
        }
    }
}